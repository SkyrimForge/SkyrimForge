using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SkyrimForge.PapyrusScripts;

/// <summary>
/// Reads the /// lines in the source, because <see cref="ISymbol.GetDocumentationCommentXml"/> returns nothing unless the project generates a documentation file.
/// </summary>
internal static class DocumentationSummary
{
    internal static string? Read(IMethodSymbol method)
    {
        if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not { } declaration)
        {
            return null;
        }
        IEnumerable<string> lines = declaration.GetLeadingTrivia().ToFullString().Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("///", StringComparison.Ordinal))
            .Select(line => line.Substring(3));
        try
        {
            XElement? summary = XElement.Parse($"<doc>{string.Join("\n", lines)}</doc>").Element("summary");
            string text = summary == null ? "" : Regex.Replace(string.Concat(summary.Nodes().Select(GetText)), @"\s+", " ").Trim();
            return text.Length > 0 ? text : null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static string GetText(XNode node) => node switch
    {
        XText text => text.Value,
        XElement { Name.LocalName: "see" or "seealso" } reference when reference.Value.Length > 0 => reference.Value,
        XElement { Name.LocalName: "see" or "seealso" } reference => GetReferencedName((string?)reference.Attribute("cref")) ?? (string?)reference.Attribute("langword") ?? "",
        XElement { Name.LocalName: "paramref" or "typeparamref" } reference => (string?)reference.Attribute("name") ?? "",
        XElement element => string.Concat(element.Nodes().Select(GetText)),
        _ => ""
    };

    private static string? GetReferencedName(string? cref)
    {
        if (cref == null)
        {
            return null;
        }
        string name = cref.Split('(', '{')[0];
        return name.Substring(Math.Max(name.LastIndexOf('.'), name.LastIndexOf(':')) + 1);
    }
}
