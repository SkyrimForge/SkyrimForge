using System;
using System.Collections.Generic;

namespace SkyrimForge.Infrastructure;

// The helpers are methods, because the serializer writes every readable property into the record.
internal sealed class CoSaveEntries
{
    public Dictionary<string, string>? Strings { get; set; }

    public Dictionary<string, uint>? Forms { get; set; }

    public Dictionary<string, CoSaveEntries>? Children { get; set; }

    public bool IsEmpty() => (Strings == null || Strings.Count == 0) && (Forms == null || Forms.Count == 0) && (Children == null || Children.Count == 0);

    public Dictionary<string, string> GetStringsForWrite() => Strings ??= new Dictionary<string, string>(StringComparer.Ordinal);

    public Dictionary<string, uint> GetFormsForWrite() => Forms ??= new Dictionary<string, uint>(StringComparer.Ordinal);

    public CoSaveEntries GetChildForWrite(string name)
    {
        Children ??= new Dictionary<string, CoSaveEntries>(StringComparer.Ordinal);
        if (!Children.TryGetValue(name, out CoSaveEntries? child))
        {
            Children[name] = child = new CoSaveEntries();
        }
        return child;
    }
}
