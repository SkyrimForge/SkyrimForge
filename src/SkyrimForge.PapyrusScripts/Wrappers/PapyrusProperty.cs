using System.Collections.Generic;

namespace SkyrimForge.PapyrusScripts;

internal sealed record PapyrusProperty(string Name, string Type, string? Value, bool IsAuto, bool IsReadable, bool IsWritable, IReadOnlyList<string> Comment, int Line);
