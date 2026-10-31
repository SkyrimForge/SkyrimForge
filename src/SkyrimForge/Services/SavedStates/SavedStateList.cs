using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace SkyrimForge;

internal sealed class SavedStateList(ISavedStateEntries list) : ISavedStateList
{
    private readonly ISavedStateEntries list = list;

    // The count is stored, because an element with no entries is not stored and the nested stores cannot count the elements.
    private ISavedStatePair<int> CountPair => list.GetPair<int>("count");

    public int Count => CountPair.Value ?? 0;

    public ISavedStateEntries this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            return GetElement(index);
        }
    }

    public ISavedStateEntries Add()
    {
        lock (list.EntriesLock)
        {
            int index = Count;
            // Removed, since a store returned before a removal can still write at this index.
            list.RemoveEntries(Format(index));
            CountPair.Value = index + 1;
            return GetElement(index);
        }
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        lock (list.EntriesLock)
        {
            int count = Count;
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
            list.RemoveEntries(Format(index));
            for (int i = index + 1; i < count; i++)
            {
                list.MoveEntries(Format(i), Format(i - 1));
            }
            // An empty list stores no count, so its store is empty and is removed.
            int remainingCount = count - 1;
            CountPair.Value = remainingCount > 0 ? remainingCount : null;
        }
    }

    public IEnumerator<ISavedStateEntries> GetEnumerator()
    {
        int count = Count;
        for (int i = 0; i < count; i++)
        {
            yield return GetElement(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private ISavedStateEntries GetElement(int index) => list.GetEntries(Format(index));

    private static string Format(int index) => index.ToString(CultureInfo.InvariantCulture);
}
