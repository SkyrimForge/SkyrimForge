using System.Collections.Generic;

namespace SkyrimForge.PapyrusScripts;

internal sealed record PapyrusFunction(string Name, string? ReturnType, bool IsGlobal, bool IsNative, IReadOnlyList<PapyrusParameter> Parameters, IReadOnlyList<string> Comment, int Line)
{
    public IReadOnlyList<string> Body { get; init; } = [];
}
