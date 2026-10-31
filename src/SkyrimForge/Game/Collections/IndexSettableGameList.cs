using System;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>A run of fixed length that a plugin reads and writes one entry at a time.</summary>
public sealed class IndexSettableGameList<T> : ReadableGameList<T>
{
    private readonly Func<nint, int, T, bool> setEntry;

    internal IndexSettableGameList(nint address, Func<nint, IReadOnlyList<T>> read, Func<nint, int> count, Func<nint, int, T, bool> setEntry) : base(address, read, count)
    {
        this.setEntry = setEntry;
    }

    /// <summary>Writes <paramref name="value"/> to the entry at <paramref name="index"/>. A null value empties a slot of a run of pointers.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or not less than the length of the run.</exception>
    public void Set(int index, T value)
    {
        if (!setEntry(Address, index, value))
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The index is outside the run.");
        }
    }
}
