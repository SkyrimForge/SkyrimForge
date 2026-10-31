using Microsoft.CodeAnalysis.Text;

namespace SkyrimForge.PapyrusScripts;

internal sealed class PscFile(string path, SourceText text, PapyrusScript? script, bool isGenerated)
{
    internal string Path { get; } = path;

    internal SourceText Text { get; } = text;

    internal PapyrusScript? Script { get; } = script;

    internal bool IsGenerated { get; } = isGenerated;
}
