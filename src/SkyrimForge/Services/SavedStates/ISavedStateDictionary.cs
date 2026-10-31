using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A dictionary in a saved-state store whose values are stored as text.</summary>
/// <remarks>
/// Keys are integers or enums, stored as their integer, so renaming an enumerator does not affect stored values.
/// Reading an entry whose text does not parse as <typeparamref name="TValue"/> throws <see cref="System.FormatException"/>.
/// Members can be called on any thread.
/// </remarks>
public interface ISavedStateDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : struct, IConvertible
    where TValue : struct, IParsable<TValue>
{
    /// <summary>Gets or sets the value for <paramref name="key"/>.</summary>
    /// <exception cref="KeyNotFoundException">The getter is called for a key that has no value.</exception>
    new TValue this[TKey key] { get; set; }

    /// <summary>Removes the value for <paramref name="key"/>.</summary>
    bool Remove(TKey key);
}
