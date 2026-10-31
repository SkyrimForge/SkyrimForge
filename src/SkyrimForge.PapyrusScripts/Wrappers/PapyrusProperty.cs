using System.Collections.Generic;

namespace SkyrimForge.PapyrusScripts;

internal sealed class PapyrusProperty(string name, string type, string? value, bool isAuto, bool isReadable, bool isWritable, IReadOnlyList<string> comment, int line)
{
    internal string Name { get; } = name;

    internal string Type { get; } = type;

    internal string? Value { get; } = value;

    /// <summary>A property that is not auto has functions as its accessors, so reading one runs the VM and returns a Task.</summary>
    internal bool IsAuto { get; } = isAuto;

    /// <summary>Papyrus allows a set alone, and a read of one throws.</summary>
    internal bool IsReadable { get; } = isReadable;

    internal bool IsWritable { get; } = isWritable;

    internal IReadOnlyList<string> Comment { get; } = comment;

    internal int Line { get; } = line;
}
