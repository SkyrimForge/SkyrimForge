using System;
using System.Collections;
using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>The entries of a container a CommonLib function returned. Every view among the entries is valid until <see cref="Dispose"/>.</summary>
/// <remarks>An undisposed list remains allocated for the rest of the session.</remarks>
public sealed class DisposableGameList<T> : IReadOnlyList<T>, IDisposable
{
    private readonly IReadOnlyList<T> entries;
    private readonly Action<nint> release;
    private nint snapshot;

    internal DisposableGameList(nint snapshot, Func<nint, IReadOnlyList<T>> read, Action<nint> release)
    {
        this.snapshot = snapshot;
        this.release = release;
        try
        {
            entries = read(snapshot);
        }
        catch
        {
            release(snapshot);
            throw;
        }
    }

    /// <inheritdoc/>
    public int Count => Entries.Count;

    /// <inheritdoc/>
    public T this[int index] => Entries[index];

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Releases the container and the memory its entries address.</summary>
    public void Dispose()
    {
        if (snapshot != 0)
        {
            release(snapshot);
            snapshot = 0;
        }
    }

    private IReadOnlyList<T> Entries => snapshot != 0 ? entries : throw new ObjectDisposedException(nameof(DisposableGameList<T>));
}
