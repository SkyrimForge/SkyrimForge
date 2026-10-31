namespace SkyrimForge.PapyrusScripts;

internal sealed class PapyrusParameter(string type, string name, string? defaultValue)
{
    internal string Type { get; } = type;

    internal string Name { get; } = name;

    /// <summary>A dispatch passes the whole argument list, so C# applies the default at the call site.</summary>
    internal string? DefaultValue { get; } = defaultValue;
}
