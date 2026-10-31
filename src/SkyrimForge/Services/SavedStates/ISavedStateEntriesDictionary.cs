using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A dictionary in a saved-state store whose values are stores of their own.</summary>
/// <remarks>
/// A key is present once its store has an entry.
/// Members can be called on any thread.
/// </remarks>
public interface ISavedStateEntriesDictionary<TKey> : IReadOnlyDictionary<TKey, ISavedStateEntries>
    where TKey : struct, IConvertible
{
    /// <summary>Gets the store for <paramref name="key"/>, whether or not it has entries.</summary>
    ISavedStateEntries GetEntries(TKey key);

    /// <summary>Removes all entries for <paramref name="key"/>.</summary>
    bool Remove(TKey key);
}
