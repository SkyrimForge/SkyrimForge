using Microsoft.CodeAnalysis.Text;

namespace SkyrimForge.PapyrusScripts;

internal sealed record PscFile(string Path, SourceText Text, PapyrusScript? Script, bool IsGenerated);
