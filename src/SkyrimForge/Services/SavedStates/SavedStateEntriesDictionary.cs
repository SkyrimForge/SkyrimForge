using System;
using System.Collections.Generic;

namespace SkyrimForge;

internal sealed class SavedStateEntriesDictionary<TKey>(ISavedStateEntries scope)
    : SavedStateDictionaryBase<TKey, ISavedStateEntries>(scope), ISavedStateEntriesDictionary<TKey>
    where TKey : struct, IConvertible
{
    public override bool TryGetValue(TKey key, out ISavedStateEntries value)
    {
        string name = SavedStateKey<TKey>.Format(key);
        value = Scope.GetEntries(name);
        return Scope.HasEntries(name);
    }

    public ISavedStateEntries GetEntries(TKey key) => Scope.GetEntries(SavedStateKey<TKey>.Format(key));

    public bool Remove(TKey key)
    {
        string name = SavedStateKey<TKey>.Format(key);
        lock (Scope.EntriesLock)
        {
            bool stored = Scope.HasEntries(name);
            Scope.RemoveEntries(name);
            return stored;
        }
    }

    public override IEnumerator<KeyValuePair<TKey, ISavedStateEntries>> GetEnumerator()
    {
        foreach (string name in Scope.GetEntriesKeys())
        {
            if (SavedStateKey<TKey>.TryParse(name, out TKey key))
            {
                yield return new KeyValuePair<TKey, ISavedStateEntries>(key, Scope.GetEntries(name));
            }
        }
    }
}
