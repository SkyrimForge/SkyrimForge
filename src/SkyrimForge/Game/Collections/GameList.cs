using System;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>A container to which entries are added and from which they are removed.</summary>
public sealed class GameList<T> : ReadableGameList<T>
{
    private readonly Action<nint, T> add;
    private readonly Action<nint> clear;
    private readonly Func<nint, int, bool> removeAt;

    internal GameList(nint address, Func<nint, IReadOnlyList<T>> read, Func<nint, int> count, Action<nint, T> add, Action<nint> clear, Func<nint, int, bool> removeAt)
        : base(address, read, count)
    {
        this.add = add;
        this.clear = clear;
        this.removeAt = removeAt;
    }

    /// <inheritdoc cref="AddOnlyGameList{T}.Add"/>
    public void Add(T value) => add(Address, value);

    /// <inheritdoc cref="AddOnlyGameList{T}.AddRange"/>
    public void AddRange(IEnumerable<T> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        foreach (T entry in entries)
        {
            Add(entry);
        }
    }

    /// <inheritdoc cref="RemoveOnlyGameList{T}.Clear"/>
    public void Clear() => clear(Address);

    /// <inheritdoc cref="RemoveOnlyGameList{T}.RemoveAt"/>
    public void RemoveAt(int index)
    {
        if (!removeAt(Address, index))
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The index is outside the container.");
        }
    }

    /// <summary>Empties the container and fills it with <paramref name="entries"/>, which may be a sequence that reads this container. The entries removed are not freed.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
    public void Replace(IEnumerable<T> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        List<T> replacements = [.. entries];
        Clear();
        AddRange(replacements);
    }
}
