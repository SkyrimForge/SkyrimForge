using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A set of text in a saved-state store.</summary>
/// <remarks>Members compare ordinally and can be called on any thread.</remarks>
public interface ISavedStateStringSet : IReadOnlyCollection<string>
{
    /// <summary>Whether <paramref name="item"/> is a member.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
    bool Contains(string item);

    /// <summary>Adds <paramref name="item"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
    bool Add(string item);

    /// <summary>Removes <paramref name="item"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
    bool Remove(string item);
}
