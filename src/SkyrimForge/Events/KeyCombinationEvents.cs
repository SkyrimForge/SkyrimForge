using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace SkyrimForge;

internal sealed class KeyCombinationEvents(IGameEvent<InputEventArgs> input, IKeyboardReader keyboardReader, Action<Exception> reportFailure)
{
    private const ModifierKeys definedModifiers = ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt;

    private readonly Lock eventsLock = new();
    private readonly Dictionary<Combination, GameEvent<InputEventArgs>> eventsByCombination = [];
    private bool inputSubscribed;

    internal IGameEvent<InputEventArgs> Get(BSKeyboardDevice.Keys.Key key, ModifierKeys modifiers)
    {
        if ((modifiers & ~definedModifiers) != ModifierKeys.None)
        {
            throw new ArgumentOutOfRangeException(nameof(modifiers), modifiers, "The value includes a flag ModifierKeys does not define.");
        }
        lock (eventsLock)
        {
            // Subscribed on first use, so no work is added to each input event until a combination is requested.
            if (!inputSubscribed)
            {
                input.Subscribe(RaiseCombinations);
                inputSubscribed = true;
            }
            var combination = new Combination(key, modifiers);
            if (!eventsByCombination.TryGetValue(combination, out GameEvent<InputEventArgs>? gameEvent))
            {
                gameEvent = new GameEvent<InputEventArgs>(reportFailure);
                eventsByCombination.Add(combination, gameEvent);
            }
            return gameEvent;
        }
    }

    private void RaiseCombinations(InputEventArgs args)
    {
        if (keyboardReader.GetKeyDown(args) is { } key)
        {
            KeyValuePair<Combination, GameEvent<InputEventArgs>>[] subscribed;
            lock (eventsLock)
            {
                subscribed = [.. eventsByCombination.Where(entry => entry.Key.Key == key && entry.Value.HasSubscribers)];
            }
            if (subscribed.Length > 0)
            {
                ModifierKeys pressedModifiers = keyboardReader.GetPressedModifierKeys();
                KeyValuePair<Combination, GameEvent<InputEventArgs>>[] matched = [.. subscribed.Where(entry => (entry.Key.Modifiers & ~pressedModifiers) == ModifierKeys.None)];
                int mostModifiers = matched.Select(entry => entry.Key.ModifierCount).DefaultIfEmpty().Max();
                foreach (KeyValuePair<Combination, GameEvent<InputEventArgs>> entry in matched.Where(entry => entry.Key.ModifierCount == mostModifiers))
                {
                    entry.Value.Raise(args);
                }
            }
        }
    }

    private readonly record struct Combination(BSKeyboardDevice.Keys.Key Key, ModifierKeys Modifiers)
    {
        internal int ModifierCount => BitOperations.PopCount((uint)Modifiers);
    }
}
