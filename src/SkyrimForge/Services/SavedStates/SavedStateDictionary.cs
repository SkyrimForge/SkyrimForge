using System;
using System.Collections.Generic;

namespace SkyrimForge;

internal sealed class SavedStateDictionary<TKey, TValue>(ISavedStateEntries scope)
    : SavedStateDictionaryBase<TKey, TValue>(scope), ISavedStateDictionary<TKey, TValue>
    where TKey : struct, IConvertible
    where TValue : struct, IParsable<TValue>
{
    public new TValue this[TKey key]
    {
        get => base[key];
        set => GetPair(key).Value = value;
    }

    public override bool TryGetValue(TKey key, out TValue value)
    {
        TValue? storedValue = GetPair(key).Value;
        value = storedValue.GetValueOrDefault();
        return storedValue != null;
    }

    public bool Remove(TKey key)
    {
        ISavedStatePair<TValue> entry = GetPair(key);
        lock (Scope.EntriesLock)
        {
            bool stored = entry.Value != null;
            entry.Value = null;
            return stored;
        }
    }

    public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (string text in Scope.GetStringKeys())
        {
            if (SavedStateKey<TKey>.TryParse(text, out TKey key) && Scope.GetPair<TValue>(text).Value is { } value)
            {
                yield return new KeyValuePair<TKey, TValue>(key, value);
            }
        }
    }

    private ISavedStatePair<TValue> GetPair(TKey key) => Scope.GetPair<TValue>(SavedStateKey<TKey>.Format(key));
}
