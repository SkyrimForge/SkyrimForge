using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkyrimForge.PapyrusScripts;

internal sealed class PapyrusScript(string name, string? parent, IReadOnlyList<string> comment, IReadOnlyList<PapyrusFunction> functions, IReadOnlyList<PapyrusProperty> properties)
{
    internal IReadOnlyList<string> Comment { get; } = comment;

    internal string Name { get; } = name;

    internal string? Parent { get; } = parent;

    internal IReadOnlyList<PapyrusFunction> Functions { get; } = functions;

    internal IReadOnlyList<PapyrusProperty> Properties { get; } = properties;

    internal bool DeclaresNative => Functions.Any(function => function.IsNative);

    private static readonly Regex declarationPattern = new(@"^\s*(?:([A-Za-z_]\w*(?:\s*\[\s*\])?)\s+)?Function\s+([A-Za-z_]\w*)\s*\(([^)]*)\)(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex scriptnamePattern = new(@"^\s*Scriptname\s+([A-Za-z_]\w*)(?:\s+extends\s+([A-Za-z_]\w*))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Anchored at the start because a body can pass the word Property to Debug.Trace.
    private static readonly Regex propertyPattern = new(@"^\s*([A-Za-z_]\w*(?:\s*\[\s*\])?)\s+Property\s+([A-Za-z_]\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex setAccessorPattern = new(@"^\s*Function\s+set\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex getAccessorPattern = new(@"^\s*[\w\[\]]+\s+Function\s+get\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex propertyValuePattern = new(@"=\s*(.+?)(?:\s+(?:Auto(?:ReadOnly)?|Hidden|Conditional|Mandatory))*\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex autoPropertyPattern = new(@"\bAuto(?:ReadOnly)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex readOnlyPropertyPattern = new(@"\bAutoReadOnly\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex endFunctionPattern = new(@"^\s*EndFunction\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static PapyrusScript Read(string path, IEnumerable<string> lines)
    {
        string? name = null;
        string? parent = null;
        bool commented = false;
        bool inProperty = false;
        bool afterDeclaration = false;
        bool inDocString = false;
        bool inBody = false;
        bool documentationAssigned = false;
        List<string> header = [];
        List<string> documentation = [];
        List<string> docString = [];
        List<string> body = [];
        List<PapyrusFunction> functions = [];
        List<PapyrusProperty> properties = [];
        foreach ((int lineNumber, string line) in JoinContinuedLines(lines))
        {
            if (commented)
            {
                commented = line.IndexOf("/;", StringComparison.Ordinal) < 0;
                continue;
            }
            if (line.IndexOf(";/", StringComparison.Ordinal) >= 0)
            {
                commented = line.IndexOf("/;", StringComparison.Ordinal) < 0;
                continue;
            }

            // A brace block directly after a declaration is Papyrus's function documentation and replaces any comment.
            // It is read before the line is split, since a semicolon inside it is text.
            if (inDocString || (afterDeclaration && line.TrimStart().StartsWith("{", StringComparison.Ordinal)))
            {
                afterDeclaration = false;
                string text = line.Trim();
                int closingBrace = text.IndexOf('}');
                docString.Add((closingBrace >= 0 ? text.Substring(0, closingBrace) : text).TrimStart('{').Trim());
                inDocString = closingBrace < 0;
                if (!inDocString)
                {
                    functions[functions.Count - 1] = functions[functions.Count - 1] with { Comment = [.. docString.Where(text => text.Length > 0)] };
                    docString.Clear();
                }
                continue;
            }
            afterDeclaration = false;

            (string code, string comment) = Split(line);

            // A blank line ends a comment's run of functions, inside a body as elsewhere.
            if (inBody)
            {
                if (endFunctionPattern.IsMatch(code))
                {
                    inBody = false;
                    functions[functions.Count - 1] = functions[functions.Count - 1] with { Body = [.. body] };
                    body.Clear();
                }
                else if (code.Trim().Length > 0)
                {
                    body.Add(code.Trim());
                }
                else if (comment.Length == 0)
                {
                    documentation.Clear();
                }
                continue;
            }

            // Accessors are not read as functions, since the VM resolves a property by its own name and a dispatch naming get or set resolves no function.
            if (inProperty)
            {
                bool declaresGet = getAccessorPattern.IsMatch(code);
                bool declaresSet = setAccessorPattern.IsMatch(code);
                if ((declaresGet || declaresSet) && properties.Count > 0)
                {
                    PapyrusProperty current = properties[properties.Count - 1];
                    properties[properties.Count - 1] = new PapyrusProperty(current.Name, current.Type, current.Value, current.IsAuto, current.IsReadable || declaresGet, current.IsWritable || declaresSet,
                        current.Comment, current.Line);
                }
                inProperty = code.IndexOf("EndProperty", StringComparison.OrdinalIgnoreCase) < 0;
                continue;
            }
            if (propertyPattern.Match(code) is { Success: true } propertyMatch)
            {
                bool auto = autoPropertyPattern.IsMatch(code);
                properties.Add(new PapyrusProperty(propertyMatch.Groups[2].Value, propertyMatch.Groups[1].Value.Replace(" ", ""), propertyValuePattern.Match(code) is { Success: true } valueMatch ? valueMatch.Groups[1].Value : null,
                    auto,
                    auto,
                    auto && !readOnlyPropertyPattern.IsMatch(code),
                    comment.Length > 0 ? [comment] : [.. documentation],
                    lineNumber));

                if (!auto)
                {
                    documentation.Clear();
                    inProperty = true;
                    continue;
                }
            }

            if (code.Trim().Length == 0)
            {
                if (comment.Length > 0)
                {
                    if (documentationAssigned)
                    {
                        documentation.Clear();
                        documentationAssigned = false;
                    }
                    documentation.Add(comment);
                    continue;
                }

                if (functions.Count == 0 && header.Count == 0)
                {
                    header = [.. documentation];
                }
                documentation.Clear();
                continue;
            }

            if (name == null && scriptnamePattern.Match(code) is { Success: true } scriptnameMatch)
            {
                name = scriptnameMatch.Groups[1].Value;
                parent = scriptnameMatch.Groups[2].Success ? scriptnameMatch.Groups[2].Value : null;
            }
            if (declarationPattern.Match(code) is { Success: true } declarationMatch)
            {
                string flags = declarationMatch.Groups[4].Value;
                bool isNative = Regex.IsMatch(flags, @"\bnative\b", RegexOptions.IgnoreCase);
                functions.Add(new PapyrusFunction(declarationMatch.Groups[2].Value, declarationMatch.Groups[1].Success ? declarationMatch.Groups[1].Value.Replace(" ", "") : null, Regex.IsMatch(flags, @"\bglobal\b", RegexOptions.IgnoreCase),
                    isNative,
                    ReadParameters(declarationMatch.Groups[3].Value),
                    comment.Length > 0 ? [comment] : [.. documentation],
                    lineNumber));
                afterDeclaration = true;
                inBody = !isNative;

                // The documentation is not cleared, so Utility.psc's "Set the given INI by type" documents SetINIFloat, SetINIInt, and SetINIBool.
                documentationAssigned = true;
                continue;
            }
            documentation.Clear();
        }

        return name != null ? new PapyrusScript(name, parent, header, functions, properties) : throw new InvalidDataException($"{path} declares no Scriptname.");
    }

    private static IEnumerable<(int Number, string Text)> JoinContinuedLines(IEnumerable<string> lines)
    {
        string continuedLine = "";
        int number = 0;
        int firstNumber = 1;
        foreach (string line in lines)
        {
            number++;
            string code = Split(line).Code.TrimEnd();
            if (code.EndsWith("\\", StringComparison.Ordinal))
            {
                if (continuedLine.Length == 0)
                {
                    firstNumber = number;
                }
                continuedLine += code.Substring(0, code.Length - 1);
                continue;
            }
            yield return (continuedLine.Length == 0 ? number : firstNumber, continuedLine + line);
            continuedLine = "";
        }
    }

    private static (string Code, string Comment) Split(string line)
    {
        bool inString = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"')
            {
                inString = !inString;
            }
            else if (line[i] == ';' && !inString)
            {
                return (line.Substring(0, i), line.Substring(i + 1).Trim());
            }
        }
        return (line, "");
    }

    private static List<PapyrusParameter> ReadParameters(string declaredParameters)
    {
        List<PapyrusParameter> parameters = [];
        foreach (string parameter in declaredParameters.Split(',').Select(parameter => parameter.Trim()).Where(parameter => parameter.Length > 0))
        {
            string[] declarationAndDefault = parameter.Split(['='], 2);
            string[] words = declarationAndDefault[0].Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                string type = string.Concat(words.Take(words.Length - 1));
                parameters.Add(new PapyrusParameter(type, words[words.Length - 1], declarationAndDefault.Length == 2 ? declarationAndDefault[1].Trim() : null));
            }
        }
        return parameters;
    }
}
