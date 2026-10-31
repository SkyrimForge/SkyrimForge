using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SkyrimForge.PapyrusScripts;

/// <summary>Generates a C# class in the project's root namespace for each .psc file the project lists, except those <see cref="PapyrusScriptGenerator"/> wrote.</summary>
[Generator(LanguageNames.CSharp)]
public sealed class PapyrusWrapperGenerator : IIncrementalGenerator
{
    private const string category = "SkyrimForge.Papyrus";

    private const string gameNamespacePrefix = "SkyrimForge.Game.";

    private static readonly DiagnosticDescriptor missingScriptname = new("SF1004", "The .psc declares no Scriptname", "{0} declares no Scriptname, so no C# is generated from it",
        category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor omittedMember = new("SF1005", "No C# member is generated for a Papyrus function or property",
        "No C# member is generated for {0}.{1}, because {2}", category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly PapyrusTypes types = new(
        PapyrusTypeNames.Known.ToDictionary(entry => entry.Value, entry => entry.Key.Substring(gameNamespacePrefix.Length), StringComparer.OrdinalIgnoreCase),
        PapyrusTypeNames.Interfaces);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<PscFile?> files = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".psc", StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => Read(text, cancellationToken));
        IncrementalValueProvider<string> namespaceName = context.AnalyzerConfigOptionsProvider.Combine(context.CompilationProvider)
            .Select(static (pair, _) => GetNamespaceName(pair.Left.GlobalOptions, pair.Right.AssemblyName));
        context.RegisterSourceOutput(files.Collect().Combine(namespaceName), static (sourceContext, pair) => Generate(sourceContext, pair.Left, pair.Right));
    }

    private static PscFile? Read(AdditionalText file, CancellationToken cancellationToken)
    {
        if (file.GetText(cancellationToken) is not { } text)
        {
            return null;
        }
        string[] lines = [.. text.Lines.Select(line => line.ToString())];
        bool isGenerated = lines.Any(line => line.StartsWith(PapyrusScriptDeclaration.GeneratedCommentPrefix, StringComparison.Ordinal));
        try
        {
            return new PscFile(file.Path, text, PapyrusScript.Read(file.Path, lines), isGenerated);
        }
        catch (InvalidDataException)
        {
            return new PscFile(file.Path, text, null, isGenerated);
        }
    }

    private static string GetNamespaceName(AnalyzerConfigOptions options, string? assemblyName)
    {
        string name = (options.TryGetValue("build_property.RootNamespace", out string? rootNamespace) && rootNamespace.Trim().Length > 0 ? rootNamespace : assemblyName ?? "").Trim();
        IEnumerable<string> segments = name.Split('.').Where(segment => segment.Length > 0).Select(segment =>
        {
            var identifier = new StringBuilder();
            foreach (char character in segment)
            {
                identifier.Append(SyntaxFacts.IsIdentifierPartCharacter(character) ? character : '_');
            }
            return SyntaxFacts.IsIdentifierStartCharacter(segment[0]) ? identifier.ToString() : "_" + identifier;
        });
        string namespaceName = string.Join(".", segments);
        return namespaceName.Length > 0 ? namespaceName : "Papyrus";
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<PscFile?> files, string namespaceName)
    {
        List<PscFile> readFiles = [.. files.Where(file => file != null).Select(file => file!).OrderBy(file => file.Path, StringComparer.Ordinal)];
        foreach (PscFile file in readFiles.Where(file => file.Script == null))
        {
            context.ReportDiagnostic(Diagnostic.Create(missingScriptname, Location.Create(file.Path, default, default), Path.GetFileName(file.Path)));
        }

        // Every script is a parent a script can extend, including those the project implements.
        var parents = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var scripts = new Dictionary<string, PscFile>(StringComparer.OrdinalIgnoreCase);
        foreach (PscFile file in readFiles.Where(file => file.Script != null))
        {
            PapyrusScript script = file.Script!;
            if (!parents.ContainsKey(script.Name))
            {
                parents[script.Name] = script.Parent;
            }
            if (!file.IsGenerated && !scripts.ContainsKey(script.Name))
            {
                scripts[script.Name] = file;
            }
        }

        Dictionary<string, HashSet<string>> contestedNames = PapyrusContestedNames.Find(scripts.ToDictionary(entry => entry.Key, entry => entry.Value.Script!, StringComparer.OrdinalIgnoreCase), parents);
        var annotations = new PapyrusAnnotations();
        foreach (PscFile file in scripts.Values)
        {
            PapyrusScript script = file.Script!;
            List<OmittedMember> omittedMembers = [];
            string? source = PapyrusWrapperRenderer.Render(script, namespaceName, types, parents, contestedNames.TryGetValue(script.Name, out HashSet<string>? names) ? names : [],
                omittedMembers, [], annotations, "SkyrimForge");
            if (source != null)
            {
                // A function or property without a comment in the .psc becomes a member without a summary.
                // The renderer joins its parts with LF, and its templates take the line endings of the file that declares them.
                context.AddSource(PapyrusWrapperRenderer.GetClassName(script.Name) + ".g.cs", ("#pragma warning disable CS1591\n" + source).Replace("\r\n", "\n").Replace("\n", "\r\n"));
            }
            foreach (OmittedMember member in omittedMembers)
            {
                TextLine line = file.Text.Lines[member.Line - 1];
                Location location = Location.Create(file.Path, line.Span, file.Text.Lines.GetLinePositionSpan(line.Span));
                context.ReportDiagnostic(Diagnostic.Create(omittedMember, location, member.Script, member.Member, member.Reason));
            }
        }
    }
}
