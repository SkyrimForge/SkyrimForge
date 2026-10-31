using System;
using System.Collections;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>A container that is a field of a game object, read through the object rather than copied out of it.</summary>
/// <remarks>
/// Each enumeration reads the container and builds a new copy, as <see cref="Get"/> does.
/// A LINQ query reads the container when the query is enumerated, not when it is built.
/// </remarks>
public class ReadableGameList<T> : IEnumerable<T>
{
    private readonly Func<nint, IReadOnlyList<T>> read;
    private readonly Func<nint, int> count;

    internal ReadableGameList(nint address, Func<nint, IReadOnlyList<T>> read, Func<nint, int> count)
    {
        Address = address;
        this.read = read;
        this.count = count;
    }

    private protected nint Address { get; }

    /// <summary>The number of entries, a null one included, read without copying them. A container of handles counts the handles that resolve to an object.</summary>
    public int Count => count(Address);

    internal Delegate? SlotReader { get; init; }

    /// <summary>Returns a copy of the container's entries, reading the container and building a list on each call.</summary>
    /// <remarks>
    /// A container of handles omits a handle that resolves to no object.
    /// A container of pointers omits a null entry, which <see cref="Count"/> includes, so an entry after a null one is at a lower index. GetSlots returns every entry at its index.
    /// </remarks>
    public IReadOnlyList<T> Get() => read(Address);

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => Get().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal IReadOnlyList<TSlot?> ReadSlots<TSlot>() where TSlot : struct
    {
        return SlotReader is Func<nint, IReadOnlyList<TSlot?>> readSlots ? readSlots(Address)
            : throw new NotSupportedException("This list cannot be read by index because its container stores handles or is a map.");
    }
}
