using System.Collections.Generic;

namespace SkyrimForge.PapyrusScripts;

/// <summary>The generator of the game's own scripts overrides these members. A Render member returns text ending in a newline, or an empty string.</summary>
internal class PapyrusAnnotations
{
    internal virtual string RenderFunctionObsolete(PapyrusScript script, PapyrusFunction function, PapyrusTypes types, string? receiver, bool onClass,
        IReadOnlyDictionary<int, string> typeParametersByIndex, string constraints, string indent) => "";

    internal virtual string RenderPropertyObsolete(string scriptName, PapyrusProperty property, PapyrusTypes types, string? receiver) => "";

    internal virtual string RenderNote(string scriptName, PapyrusFunction function, string indent) => "";

    internal virtual bool ReturnsNoneEntries(string scriptName, PapyrusFunction function) => false;
}
