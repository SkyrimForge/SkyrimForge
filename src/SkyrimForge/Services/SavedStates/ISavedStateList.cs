using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A list in a saved-state store, whose elements are stores of their own.</summary>
/// <remarks>
/// An element addresses an index and not the entries it was read from,
/// so after <see cref="RemoveAt"/> an element obtained earlier reads and writes the entries now at its index.
/// Members can be called on any thread.
/// </remarks>
public interface ISavedStateList : IReadOnlyList<ISavedStateEntries>
{
    /// <summary>Appends an empty element and returns its store.</summary>
    ISavedStateEntries Add();

    /// <summary>Removes the element at <paramref name="index"/> and shifts later elements down.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    void RemoveAt(int index);
}
