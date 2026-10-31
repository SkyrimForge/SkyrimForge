using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Generic;

namespace SkyrimForge.Papyrus;

/// <summary>The entries of a Papyrus array, each read as a <see cref="PapyrusValue"/>.</summary>
/// <remarks>Valid only as long as the <see cref="PapyrusValue"/> that produced it.</remarks>
public readonly ref struct PapyrusArray
{
    private readonly IReadOnlyList<Variable> entries;
    private readonly FormType objectType;

    internal PapyrusArray(IReadOnlyList<Variable> entries, FormType objectType)
    {
        this.entries = entries;
        this.objectType = objectType;
    }

    /// <summary>The number of entries. Zero for a None array.</summary>
    public int Count => Entries.Count;

    private IReadOnlyList<Variable> Entries => entries ?? [];

    /// <summary>The entry at <paramref name="index"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
    public PapyrusValue this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            return new(Entries[index], objectType);
        }
    }
}
