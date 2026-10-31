using System;
using System.Collections.Generic;

namespace SkyrimForge.PapyrusScripts;

internal sealed class PapyrusTypes(IReadOnlyDictionary<string, string> views, IReadOnlyDictionary<string, string> interfacesByView)
{
    private static readonly Dictionary<string, string> scalars = new(StringComparer.OrdinalIgnoreCase)
    {
        { "int", "int" },
        { "float", "float" },
        { "bool", "bool" },
        { "string", "string" }
    };

    internal static bool IsScalar(string papyrusType) => scalars.ContainsKey(papyrusType);

    internal static bool IsArray(string? papyrusType)
    {
        return papyrusType != null && papyrusType.EndsWith("[]", StringComparison.Ordinal);
    }

    internal static string GetElementTypeName(string arrayType) => arrayType.Substring(0, arrayType.Length - 2);

    /// <summary>A string entry is nullable to match the bound overload.</summary>
    internal static string GetElementType(string mappedType) => mappedType == "string" ? "string?" : mappedType;

    internal string? GetValueType(string papyrusType)
    {
        bool array = IsArray(papyrusType);
        string element = array ? GetElementTypeName(papyrusType) : papyrusType;
        string? mappedType = scalars.TryGetValue(element, out string? scalar) ? scalar : views.TryGetValue(element, out string? view) ? view : null;
        return mappedType == null ? null : array ? mappedType + "[]" : mappedType;
    }

    internal string? GetInterfaceType(string papyrusType)
    {
        return views.TryGetValue(papyrusType, out string? view) && interfacesByView.TryGetValue(view, out string? interfaceType) ? interfaceType : null;
    }

    internal string? GetReceiverType(string papyrusClass)
    {
        return GetInterfaceType(papyrusClass) ?? (views.TryGetValue(papyrusClass, out string? view) ? view : null);
    }

    internal bool IsForm(string papyrusType)
    {
        return !IsArray(papyrusType) && GetValueType(papyrusType) != null && !IsScalar(papyrusType);
    }

    internal bool CanBeNone(string papyrusType)
    {
        return GetValueType(papyrusType) != null && (IsArray(papyrusType) || !IsScalar(papyrusType));
    }

    /// <summary>A form is nullable, since Papyrus can return None for one. A scalar has no None, and an array that is None reads as empty.</summary>
    internal string GetDispatchResultType(string papyrusType)
    {
        string mappedType = GetValueType(papyrusType)!;
        return IsScalar(papyrusType) || IsArray(papyrusType) ? mappedType : mappedType + "?";
    }

    /// <summary>An unwritten string variable is empty, and PapyrusValue.Array returns no entries for None, so neither read returns null.</summary>
    internal string GetNullForgivingOperator(string? papyrusType) =>
        papyrusType != null && (IsArray(papyrusType) || GetValueType(papyrusType) == "string") ? "!" : "";
}
