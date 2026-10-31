using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SkyrimForge.PapyrusScripts;

/// <summary>
/// An instance function is an extension member of PapyrusScript{T}, not of the view, where Papyrus's GetName and the bound GetName would both apply to actor.GetName().
/// If the two differed only by an optional parameter, overload resolution would silently choose the dispatch.
/// </summary>
internal static class PapyrusWrapperRenderer
{
    private static readonly HashSet<string> keywords = [.. ("abstract as base bool break byte case catch char "
        + "checked class const continue decimal default delegate do double else enum event explicit extern "
        + "false finally fixed float for foreach goto if implicit in int interface internal is lock long "
        + "namespace new null object operator out override params private protected public readonly ref return "
        + "sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while").Split(' ')];

    private static string? GetReceiverClass(PapyrusScript script, PapyrusTypes types, IReadOnlyDictionary<string, string?> parents)
    {
        var visitedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (string? current = script.Name; current != null && visitedNames.Add(current);
            current = parents.TryGetValue(current, out string? parent) ? parent : null)
        {
            if (types.GetReceiverType(current) != null)
            {
                return current;
            }
        }
        return null;
    }

    internal static string GetClassName(string scriptName) => scriptName + "Script";

    internal static string GetParameterName(PapyrusParameter parameter)
    {
        return keywords.Contains(parameter.Name) ? "@" + parameter.Name : parameter.Name;
    }

    internal static string GetMethodName(PapyrusFunction function)
    {
        return char.ToUpperInvariant(function.Name[0]) + function.Name.Substring(1);
    }

    internal static string? Render(PapyrusScript script, string namespaceName, PapyrusTypes types, IReadOnlyDictionary<string, string?> parents, ICollection<string> contestedNames,
        ICollection<OmittedMember> omittedMembers, ICollection<string> generatedNames, PapyrusAnnotations annotations, string generatorName)
    {
        string? receiverClass = GetReceiverClass(script, types, parents);
        string? receiver = receiverClass == null ? null : types.GetReceiverType(receiverClass);
        HashSet<string> headings = GetHeadings(script);
        List<string> methods = [];
        List<string> staticMethods = [];

        // A run split between the wrapper and the class needs its heading above both.
        string lastOnWrapper = "";
        string lastOnClass = "";
        foreach (PapyrusFunction function in script.Functions)
        {
            if (!function.IsGlobal && receiver == null)
            {
                omittedMembers.Add(new OmittedMember(script.Name, function.Name, $"neither {script.Name} nor a script it extends is a form type with a C# type, so the function has no object to be called on", function.Line));
                continue;
            }
            if (FindUnmappedType(function, types) is { } unmappedType)
            {
                omittedMembers.Add(new OmittedMember(script.Name, function.Name, $"no C# type corresponds to the Papyrus type {unmappedType}", function.Line));
                continue;
            }
            bool onClass = function.IsGlobal || contestedNames.Contains(function.Name);
            string comment = GetCommentText(function);
            ref string last = ref (onClass ? ref lastOnClass : ref lastOnWrapper);
            (onClass ? staticMethods : methods).Add(RenderMember(script, function, types, receiver, headings, onClass, comment == last, annotations));
            last = comment;
            generatedNames.Add(GetMethodName(function));
        }
        // Papyrus writes enums as constant properties. Most of their comments head a run and describe the family, so each is written as a plain comment, not a summary.
        List<string> constants = [.. script.Properties
            .Where(property => IsConstant(property, types))
            .Select(property => RenderHeading(property.Comment, "    ") + $"    public const {types.GetValueType(property.Type)} {property.Name} = {RenderLiteral(property.Type, property.Value, types)};")];
        var propertyRenderer = new PapyrusPropertyRenderer(script.Name, types, receiver, annotations, methods, omittedMembers, generatedNames);
        foreach (PapyrusProperty property in script.Properties.Where(property => !IsConstant(property, types)))
        {
            propertyRenderer.Add(property);
        }
        if (methods.Count == 0 && staticMethods.Count == 0 && constants.Count == 0)
        {
            return null;
        }

        List<string> usings = ["SkyrimForge.Game", "System.Threading.Tasks"];

        if (namespaceName != "SkyrimForge.Papyrus")
        {
            usings.Add("SkyrimForge.Papyrus");
        }
        if (methods.Concat(staticMethods).Any(member => member.IndexOf("IReadOnlyCollection<", StringComparison.Ordinal) >= 0))
        {
            usings.Add("System.Collections.Generic");
        }
        if (methods.Concat(staticMethods).Any(member => member.IndexOf("[Obsolete(", StringComparison.Ordinal) >= 0
            || member.IndexOf("ArgumentOutOfRangeException", StringComparison.Ordinal) >= 0))
        {
            usings.Add("System");
        }
        string directives = CSharpFormatting.RenderDirectives(usings);
        string body = RenderBlock(script.Name, methods, staticMethods, constants, receiverClass == null ? null : types.GetInterfaceType(receiverClass), receiver);
        return $$"""
            // Generated by {{generatorName}} from {{script.Name}}.psc. Do not edit.
            #nullable enable
            {{directives}}
            namespace {{namespaceName}};

            /// <summary>
            /// Functions and comments from {{script.Name}}.psc.{{RenderPreamble(script)}}
            /// </summary>
            public static class {{GetClassName(script.Name)}}
            {
            {{body}}
            }

            """;
    }

    private static string RenderPreamble(PapyrusScript script) => script.Comment.Count == 0 ? ""
            : $"\n/// <para>\n{string.Concat(script.Comment.Select(line => CSharpFormatting.RenderDocLines(CSharpFormatting.EscapeXml(line), "/// ") + "\n"))}/// </para>";

    private static string RenderBlock(string scriptName, List<string> methods, IEnumerable<string> staticMethods, IEnumerable<string> constants, string? constraint, string? receiver)
    {
        string dispatcher = "    /// <summary>The dispatcher that binds this script to a form and calls the functions and properties that have no generated member.</summary>\n"
            + $"    public static PapyrusScriptDispatcher Dispatcher {{ get; }} = new PapyrusScriptDispatcher(\"{scriptName}\");";
        List<string> parts = [dispatcher, .. constants];
        if (methods.Count > 0)
        {
            string header = constraint != null ? $"    extension<T>(PapyrusScript<T> self) where T : {constraint}" : $"    extension(PapyrusScript<{receiver}> self)";
            parts.Add($"{header}\n    {{\n{string.Join("\n\n", methods)}\n    }}");
        }
        parts.AddRange(staticMethods);
        return string.Join("\n\n", parts);
    }

    private static string? FindUnmappedType(PapyrusFunction function, PapyrusTypes types)
    {
        if (function.ReturnType != null && types.GetValueType(function.ReturnType) == null)
        {
            return function.ReturnType;
        }
        return function.Parameters.FirstOrDefault(parameter => types.GetValueType(parameter.Type) == null)?.Type;
    }

    private static string RenderMember(PapyrusScript script, PapyrusFunction function, PapyrusTypes types, string? receiver, ICollection<string> headings, bool onClass, bool repeated, PapyrusAnnotations annotations)
    {
        (Dictionary<int, string> typeParametersByIndex, string constraints) = GetTypeParameters(function, types);
        int requiredCount = function.Parameters.Count - CountDefaulted(function, types);
        IEnumerable<string> parameterList = function.Parameters.Select((parameter, i) =>
            $"{RenderParameterType(parameter, types, typeParametersByIndex.TryGetValue(i, out string? typeParameter) ? typeParameter : null)} {GetParameterName(parameter)}"
            + (i < requiredCount ? "" : $" = {GetParameterDefault(parameter, types)}"));
        string parameters = string.Join(", ", onClass && !function.IsGlobal ? parameterList.Prepend($"{receiver} self") : parameterList);
        string typeParameters = typeParametersByIndex.Count == 0 ? "" : $"<{string.Join(", ", typeParametersByIndex.Values)}>";
        string? resultType = function.ReturnType == null ? null : GetResultType(script, function, types, annotations);
        string typeArgument = resultType == null ? "" : $"<{resultType}>";

        List<string> arguments = [];
        if (!function.IsGlobal)
        {
            arguments.Add(onClass ? "self" : "self.Receiver");
        }
        arguments.Add($"\"{function.Name}\"");
        if (function.Parameters.Count > 0)
        {
            arguments.Add("new PapyrusArguments()" + string.Concat(function.Parameters.Select(parameter => $".Add({GetParameterName(parameter)})")));
        }

        string indent = onClass ? "    " : "        ";
        // PapyrusArguments records the arguments and the call allocates them, so building the arguments throws only usage errors, and the task is returned directly.
        List<string> arrayNames = [.. function.Parameters.Where(parameter => PapyrusTypes.IsArray(parameter.Type)).Select(GetParameterName)];
        return (repeated ? "" : RenderSummary(function, headings, indent))
            + annotations.RenderNote(script.Name, function, indent)
            + RenderArrayLengthException(arrayNames, indent)
            + (IsDeprecated(function) ? RenderPapyrusDeprecation(script, function, types, indent)
                : annotations.RenderFunctionObsolete(script, function, types, receiver, onClass, typeParametersByIndex, constraints, indent))
            + $"{indent}public {(onClass ? "static " : "")}{(resultType == null ? "Task" : $"Task<{resultType}>")} "
            + $"{GetMethodName(function)}{typeParameters}({parameters}){constraints} => Dispatcher.CallAsync{typeArgument}({string.Join(", ", arguments)}){types.GetNullForgivingOperator(function.ReturnType)};";
    }

    private static string RenderArrayLengthException(IReadOnlyList<string> arrayNames, string indent)
    {
        if (arrayNames.Count == 0)
        {
            return "";
        }
        string[] references = [.. arrayNames.Select(name => $"<paramref name=\"{name.TrimStart('@')}\"/>")];
        string last = references[references.Length - 1];
        string subject = references.Length switch
        {
            1 => last,
            2 => $"{references[0]} or {last}",
            _ => $"{string.Join(", ", references.Take(references.Length - 1))}, or {last}"
        };
        string prose = $"{subject} has more than 128 entries.";
        string singleLine = $"{indent}/// <exception cref=\"ArgumentOutOfRangeException\">{prose}</exception>";
        return singleLine.Length <= 180 ? singleLine + "\n"
            : $"{indent}/// <exception cref=\"ArgumentOutOfRangeException\">\n{CSharpFormatting.RenderDocLines(prose, $"{indent}/// ")}\n{indent}/// </exception>\n";
    }

    // "int Property Foo = 5 Auto" is a variable with a starting value, so only a property that cannot be written is constant.
    private static bool IsConstant(PapyrusProperty property, PapyrusTypes types)
    {
        return !property.IsWritable && RenderLiteral(property.Type, property.Value, types) != null;
    }

    private const string deprecatedDiagnosticId = "SF0003";

    // The pattern matches capitals only, since Spell.psc writes "will likely be deprecated" of a function it has not deprecated. SKSE spells it DEPRECIATED.
    private static readonly Regex deprecationPattern = new(@"\bDEPRECI?ATED\b", RegexOptions.Compiled);

    private static readonly Regex forwardingCallPattern = new(@"^(?:return\s+)?(?:(\w+)\s*\.\s*)?(\w+)\s*\(([^()]*)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsDeprecated(PapyrusFunction function) => function.Comment.Any(deprecationPattern.IsMatch);

    private static string RenderPapyrusDeprecation(PapyrusScript script, PapyrusFunction function, PapyrusTypes types, string indent)
    {
        string message = GetForwardTarget(script, function, types) is { } target
            ? $"{script.Name}.psc deprecates {GetMethodName(function)}, which calls {GetMethodName(target)} with the same arguments. Use {GetMethodName(target)} instead."
            : $"{script.Name}.psc deprecates {GetMethodName(function)}.";
        return $"{indent}[Obsolete(\"{message}\", DiagnosticId = \"{deprecatedDiagnosticId}\")]\n";
    }

    private static PapyrusFunction? GetForwardTarget(PapyrusScript script, PapyrusFunction function, PapyrusTypes types)
    {
        if (function.Body.Count == 1 && forwardingCallPattern.Match(function.Body[0]) is { Success: true } call
            && (!call.Groups[1].Success || call.Groups[1].Value.Equals(script.Name, StringComparison.OrdinalIgnoreCase))
            && call.Groups[3].Value.Split(',').Select(argument => argument.Trim()).Where(argument => argument.Length > 0)
                .SequenceEqual(function.Parameters.Select(parameter => parameter.Name), StringComparer.OrdinalIgnoreCase))
        {
            return script.Functions.FirstOrDefault(candidate => candidate.Name.Equals(call.Groups[2].Value, StringComparison.OrdinalIgnoreCase)
                && candidate.IsGlobal == function.IsGlobal && candidate.Parameters.Count == function.Parameters.Count && FindUnmappedType(candidate, types) == null);
        }
        return null;
    }

    /// <summary>A view is a struct, so a collection of one does not convert to a collection of its interface by variance.</summary>
    private static (Dictionary<int, string> Named, string Constraints) GetTypeParameters(PapyrusFunction function, PapyrusTypes types)
    {
        Dictionary<int, string> typeParametersByIndex = [];
        string constraints = "";
        for (int i = 0; i < function.Parameters.Count; i++)
        {
            string papyrusType = function.Parameters[i].Type;

            // A type parameter appearing only in an omitted parameter cannot be inferred, so GetAllArmor("Skyrim.esm") would be CS0411.
            if (PapyrusTypes.IsArray(papyrusType) && !IsDeclaredOptional(function.Parameters[i], types) && types.GetInterfaceType(PapyrusTypes.GetElementTypeName(papyrusType)) is { } constraint)
            {
                string name = typeParametersByIndex.Count == 0 ? "TForm" : $"TForm{typeParametersByIndex.Count + 1}";
                typeParametersByIndex[i] = name;
                constraints += $" where {name} : {constraint}";
            }
        }
        return (typeParametersByIndex, constraints);
    }

    private static string RenderSummary(PapyrusFunction function, ICollection<string> headings, string indent)
    {
        if (function.Comment.Count == 0)
        {
            return "";
        }

        if (headings.Contains(GetCommentText(function)))
        {
            return RenderHeading(function.Comment, indent);
        }
        return CSharpFormatting.RenderSummary(function.Comment, indent);
    }

    /// <summary>Plain comment lines, since a summary would attach the heading to the first member alone.</summary>
    private static string RenderHeading(IEnumerable<string> comment, string indent)
    {
        return string.Concat(comment.Select(line => $"{indent}// {line}\n"));
    }

    private static HashSet<string> GetHeadings(PapyrusScript script)
    {
        return [.. script.Functions.Zip(script.Functions.Skip(1), (first, second) => (First: first, Second: second))
            .Where(pair => pair.First.Comment.Count > 0 && GetCommentText(pair.First) == GetCommentText(pair.Second))
            .Select(pair => GetCommentText(pair.First))];
    }

    private static string GetCommentText(PapyrusFunction function) => string.Join("\n", function.Comment);

    private static int CountDefaulted(PapyrusFunction function, PapyrusTypes types)
    {
        int defaultedCount = 0;
        foreach (PapyrusParameter parameter in function.Parameters.Reverse())
        {
            if (GetParameterDefault(parameter, types) == null)
            {
                break;
            }
            defaultedCount++;
        }
        return defaultedCount;
    }

    private static string? RenderLiteral(string papyrusType, string? declaredDefault, PapyrusTypes types)
    {
        if (declaredDefault == null)
        {
            return null;
        }
        return types.GetValueType(papyrusType) switch
        {
            "int" => RenderIntegerLiteral(declaredDefault),
            "float" => float.TryParse(declaredDefault, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? declaredDefault + "f" : null,
            "bool" => declaredDefault.Equals("true", StringComparison.OrdinalIgnoreCase) ? "true" : declaredDefault.Equals("false", StringComparison.OrdinalIgnoreCase) ? "false" : null,
            "string" => declaredDefault.Length >= 2 && declaredDefault.StartsWith("\"", StringComparison.Ordinal) && declaredDefault.EndsWith("\"", StringComparison.Ordinal) ? declaredDefault : null,
            _ => null
        };
    }

    private static string? RenderIntegerLiteral(string value)
    {
        if (int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _))
        {
            return value;
        }
        // HexNumber would also allow whitespace, which the returned C# literal cannot contain.
        if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || !uint.TryParse(value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint bits))
        {
            return null;
        }
        return bits <= int.MaxValue ? value : $"unchecked((int){value})";
    }

    private static string GetResultType(PapyrusScript script, PapyrusFunction function, PapyrusTypes types, PapyrusAnnotations annotations)
    {
        string resultType = types.GetDispatchResultType(function.ReturnType!);
        return PapyrusTypes.IsArray(function.ReturnType) && annotations.ReturnsNoneEntries(script.Name, function) ? resultType.Substring(0, resultType.Length - 2) + "?[]" : resultType;
    }

    internal static string RenderParameterType(PapyrusParameter parameter, PapyrusTypes types, string? typeParameter)
    {
        string declaration;
        if (PapyrusTypes.IsArray(parameter.Type))
        {
            string element = PapyrusTypes.GetElementTypeName(parameter.Type);
            declaration = $"IReadOnlyCollection<{typeParameter ?? types.GetInterfaceType(element) ?? PapyrusTypes.GetElementType(types.GetValueType(element)!)}>";
        }
        else
        {
            declaration = types.GetReceiverType(parameter.Type) ?? types.GetValueType(parameter.Type)!;
        }
        return IsDeclaredOptional(parameter, types) ? declaration + "?" : declaration;
    }

    /// <summary>
    /// Papyrus accepts None for any object or array, but an unmarked null is likelier a caller's mistake, so only a parameter the script defaults to None is nullable.
    /// </summary>
    private static bool IsDeclaredOptional(PapyrusParameter parameter, PapyrusTypes types)
    {
        return string.Equals(parameter.DefaultValue, "None", StringComparison.OrdinalIgnoreCase) && types.CanBeNone(parameter.Type);
    }

    /// <summary>
    /// Separate from <see cref="RenderLiteral"/>, which <see cref="IsConstant"/> also calls, so an AutoReadOnly object property declared None does not become a const.
    /// </summary>
    private static string? GetParameterDefault(PapyrusParameter parameter, PapyrusTypes types)
    {
        return IsDeclaredOptional(parameter, types) ? "null" : RenderLiteral(parameter.Type, parameter.DefaultValue, types);
    }
}
