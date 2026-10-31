using System;
using System.Collections;
using System.Collections.Generic;

namespace SkyrimForge;

internal sealed class SavedStateStringSet(ISavedStateEntries scope) : ISavedStateStringSet
{
    private readonly ISavedStateEntries scope = scope;

    public int Count => scope.GetStringKeys().Count;

    public bool Contains(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return scope.GetString(item) != null;
    }

    public bool Add(string item)
    {
        // Empty rather than null, since SetString drops the key for null.
        const string presentMarker = "";
        ArgumentNullException.ThrowIfNull(item);
        lock (scope.EntriesLock)
        {
            if (Contains(item))
            {
                return false;
            }
            scope.SetString(item, presentMarker);
            return true;
        }
    }

    public bool Remove(string item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (scope.EntriesLock)
        {
            if (!Contains(item))
            {
                return false;
            }
            scope.SetString(item, null);
            return true;
        }
    }

    public IEnumerator<string> GetEnumerator() => scope.GetStringKeys().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
