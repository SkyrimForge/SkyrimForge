using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SkyrimForge;

internal abstract class SavedStateDictionaryBase<TKey, TValue>(ISavedStateEntries scope) : IReadOnlyDictionary<TKey, TValue> where TKey : struct, IConvertible
{
    protected ISavedStateEntries Scope { get; } = scope;

    public TValue this[TKey key] =>
        TryGetValue(key, out TValue? value) ? value : throw new KeyNotFoundException($"Nothing is stored under {SavedStateKey<TKey>.Format(key)}.");

    public IEnumerable<TKey> Keys => this.Select(pair => pair.Key);

    public IEnumerable<TValue> Values => this.Select(pair => pair.Value);

    // CA1829 would have this read Count, which is this property.
#pragma warning disable CA1829 // Use Length/Count property instead of Count() when available
    public int Count => Enumerable.Count(this);
#pragma warning restore CA1829 // Use Length/Count property instead of Count() when available

    public bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public abstract bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

    public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
