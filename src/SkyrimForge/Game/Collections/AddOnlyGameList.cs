using System;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>A container to which entries are appended and from which none can be removed.</summary>
public sealed class AddOnlyGameList<T> : ReadableGameList<T>
{
    private readonly Action<nint, T> add;

    internal AddOnlyGameList(nint address, Func<nint, IReadOnlyList<T>> read, Func<nint, int> count, Action<nint, T> add) : base(address, read, count)
    {
        this.add = add;
    }

    /// <summary>Appends one entry. Nothing frees an entry once it is appended.</summary>
    public void Add(T value) => add(Address, value);

    /// <summary>Appends every one of <paramref name="entries"/> in order.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
    public void AddRange(IEnumerable<T> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        foreach (T entry in entries)
        {
            Add(entry);
        }
    }
}
