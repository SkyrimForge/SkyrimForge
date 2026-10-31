using System;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>A container from which entries are removed without freeing them.</summary>
public sealed class RemoveOnlyGameList<T> : ReadableGameList<T>
{
    private readonly Action<nint> clear;
    private readonly Func<nint, int, bool> removeAt;

    internal RemoveOnlyGameList(nint address, Func<nint, IReadOnlyList<T>> read, Func<nint, int> count, Action<nint> clear, Func<nint, int, bool> removeAt) : base(address, read, count)
    {
        this.clear = clear;
        this.removeAt = removeAt;
    }

    /// <summary>Empties the container without freeing its entries, which the game allocated and may free itself.</summary>
    public void Clear() => clear(Address);

    /// <summary>Removes the entry at <paramref name="index"/> without freeing memory the entry addresses.</summary>
    /// <remarks>
    /// In a container of handles the index counts the entries <see cref="ReadableGameList{T}.Get"/> returns, which skips a handle that resolves to no object.
    /// In a container of pointers it counts every entry, a null one included, as GetSlots returns them.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> does not identify an entry.</exception>
    public void RemoveAt(int index)
    {
        if (!removeAt(Address, index))
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The index is outside the container.");
        }
    }
}
