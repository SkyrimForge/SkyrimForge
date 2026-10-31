using System;

namespace SkyrimForge;

/// <summary>One entry of a saved-state store, read and written as <typeparamref name="T"/> under one key.</summary>
/// <remarks>
/// Converts with the invariant culture.
/// Store a form ID through <see cref="ISavedStateFormIdPair{TForm}"/> instead,
/// since a form ID stored as a number skips the mapping <see cref="ISavedStateEntries.SetFormId"/> describes.
/// Members can be called on any thread.
/// </remarks>
public interface ISavedStatePair<T> where T : struct, IParsable<T>
{
    /// <summary>The key this reads and writes within its store.</summary>
    string Key { get; }

    /// <summary>
    /// The stored value. It is null if nothing is stored under <see cref="Key"/>.
    /// Assigning null drops the key.
    /// </summary>
    /// <exception cref="System.FormatException">The stored text does not parse as <typeparamref name="T"/>.</exception>
    T? Value { get; set; }
}
