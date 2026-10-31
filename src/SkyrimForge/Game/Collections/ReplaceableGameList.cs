using System;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>A run of entries that a plugin reads and replaces whole.</summary>
public sealed class ReplaceableGameList<T> : ReadableGameList<T>
{
    private readonly Func<nint, IReadOnlyList<T>, bool> replace;

    internal ReplaceableGameList(nint address, Func<nint, IReadOnlyList<T>> read, Func<nint, int> count, Func<nint, IReadOnlyList<T>, bool> replace) : base(address, read, count)
    {
        this.replace = replace;
    }

    /// <summary>Replaces the run with <paramref name="entries"/>. The new run is allocated on the game's heap, and the run it replaces is not freed.</summary>
    /// <remarks>A run that stores its entries whole receives a copy of each, so the entries read from it afterwards are the copies.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="entries"/> has more entries than the run's count field can store, or the game's heap could not allocate the new run.
    /// </exception>
    public void Replace(IEnumerable<T> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (!replace(Address, [.. entries]))
        {
            throw new ArgumentException("The run's count field cannot store that many entries, or the game's heap could not allocate them.", nameof(entries));
        }
    }
}
