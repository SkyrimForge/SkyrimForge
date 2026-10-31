using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="IGameEvents"/>.</summary>
public static class GameEventsExtensions
{
    extension(IGameEvents events)
    {
        /// <summary>An event raised when the player activates a reference.</summary>
        /// <remarks>Raised on a worker thread. <see cref="IGameEvents"/> states how a handler reaches the game's thread.</remarks>
        public IGameEvent<ActivateEventArgs> OnPlayerActivate => new FilteredGameEvent<ActivateEventArgs>(events.OnActivate, args => args.ActionReference?.IsPlayerRef() == true);

        /// <summary>Returns an event raised once as <paramref name="key"/> goes down, with any modifier keys or none.</summary>
        /// <remarks>Raised mostly on a worker thread. <see cref="IGameEvents"/> states how a handler reaches the game's thread.</remarks>
        public IGameEvent<InputEventArgs> OnKeyDown(BSKeyboardDevice.Keys.Key key) => new FilteredGameEvent<InputEventArgs>(events.OnInput, args => args.IsKeyDown(key));
    }
}
