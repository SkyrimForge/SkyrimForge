using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge;

/// <summary>Arguments for <see cref="IStoryEvents.OnStoryEvent"/>.</summary>
/// <param name="Signature">
/// The four characters the game registers the kind under, such as "CRFT" for crafting and "KILL" for a killing.
/// </param>
/// <param name="Name">The name the game gives the kind, such as "Craft Item".</param>
/// <param name="Values">One entry per value the kind declares, in the order it declares them.</param>
public readonly record struct StoryEventArgs(string Signature, string Name, IReadOnlyList<StoryEventValue> Values)
{
    /// <summary>Gets the first value named <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException">The kind declares no value named <paramref name="name"/>.</exception>
    public StoryEventValue GetValue(string name) =>
        Values.FirstOrNull(value => value.Name == name)
            ?? throw new ArgumentException($"{Signature} declares no value named \"{name}\". It declares {DeclaredNames}.", nameof(name));

    // Quoted, so a trailing space is visible, as in the "Actor " that AFAV declares.
    private string DeclaredNames => Values.Count == 0 ? "none" : string.Join(", ", Values.Select(value => $"\"{value.Name}\""));
}
