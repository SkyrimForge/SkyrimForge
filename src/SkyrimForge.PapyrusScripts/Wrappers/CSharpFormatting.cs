using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.PapyrusScripts;

internal static class CSharpFormatting
{
    internal static string RenderDirectives(IEnumerable<string> namespaces) =>
        string.Concat(namespaces.Distinct(StringComparer.Ordinal).OrderBy(namespaceName => namespaceName, StringComparer.Ordinal).Select(namespaceName => $"using {namespaceName};\n"));

    internal static string RenderDocLines(string prose, string prefix = "    /// ")
    {
        int width = 180 - prefix.Length;
        List<string> lines = [];
        string remaining = prose.Trim();
        while (remaining.Length > width)
        {
            int breakIndex = FindBreakIndex(remaining, width);
            if (breakIndex == 0)
            {
                break;
            }
            lines.Add(remaining.Substring(0, breakIndex));
            remaining = remaining.Substring(breakIndex).TrimStart();
        }
        lines.Add(remaining);
        return string.Join("\n", lines.Select(line => prefix + line));
    }

    internal static string RenderDocElement(string tag, string inner, string prefix)
    {
        string singleLine = $"{prefix}<{tag}>{inner}</{tag}>";
        return singleLine.Length <= 180 ? singleLine : $"{prefix}<{tag}>\n{RenderDocLines(inner, prefix)}\n{prefix}</{tag}>";
    }

    internal static string RenderSummary(IReadOnlyList<string> comment, string indent)
    {
        IEnumerable<string> lines = comment.Select(EscapeXml);
        return comment.Count switch
        {
            0 => "",
            1 => RenderDocElement("summary", lines.First(), $"{indent}/// ") + "\n",
            _ => $"{indent}/// <summary>\n{string.Concat(lines.Select(line => RenderDocLines(line, $"{indent}/// ") + "\n"))}"
                + $"{indent}/// </summary>\n"
        };
    }

    internal static string EscapeXml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    /// <summary>Angle brackets are counted so a punctuation break never falls inside a cref.</summary>
    private static int FindBreakIndex(string prose, int width)
    {
        int depth = 0;
        int punctuationIndex = 0;
        int spaceIndex = 0;
        for (int i = 0; i < prose.Length && i <= width; i++)
        {
            char character = prose[i];
            if (character == '<')
            {
                depth++;
            }
            else if (character == '>' && depth > 0)
            {
                depth--;
            }
            else if (character == ' ')
            {
                spaceIndex = i;
                if (depth == 0 && i > 0 && ".,:;".IndexOf(prose[i - 1]) >= 0)
                {
                    punctuationIndex = i;
                }
            }
        }
        return punctuationIndex != 0 ? punctuationIndex : spaceIndex;
    }
}
