using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace SkyrimForge.PapyrusScripts;

/// <summary>
/// Writes the .psc declaring the native functions of each class marked [PapyrusScript], and reports a method Papyrus cannot declare as a compile error.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class PapyrusScriptGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<PapyrusScriptDeclaration> scripts = context.SyntaxProvider.ForAttributeWithMetadataName("SkyrimForge.Papyrus.PapyrusScriptAttribute",
            static (node, _) => node is TypeDeclarationSyntax,
            static (attributeContext, _) => PapyrusScriptDeclaration.Read((INamedTypeSymbol)attributeContext.TargetSymbol, attributeContext.Attributes[0]));
        IncrementalValueProvider<string?> outputDirectory = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => GetOutputDirectory(provider.GlobalOptions));
        context.RegisterSourceOutput(scripts.Collect().Combine(outputDirectory), static (sourceContext, pair) => Write(sourceContext, pair.Left, pair.Right));
    }

    private static string? GetOutputDirectory(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue("build_property.BuildingProject", out string? buildingProject) && string.Equals(buildingProject, "true", StringComparison.OrdinalIgnoreCase)
            && options.TryGetValue("build_property.ProjectDir", out string? projectDirectory) && projectDirectory.Length > 0)
        {
            return Path.Combine(projectDirectory, "Scripts", "Source");
        }
        return null;
    }

    private static void Write(SourceProductionContext context, ImmutableArray<PapyrusScriptDeclaration> scripts, string? outputDirectory)
    {
        // Applying the attribute to two partial declarations of one class is a separate compiler error, so a class counts once.
        List<IGrouping<string, PapyrusScriptDeclaration>> sharedNameGroups = [.. scripts.GroupBy(script => script.ScriptName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(script => script.ClassFullName).Distinct().Count() > 1)];
        foreach (Diagnostic diagnostic in scripts.SelectMany(script => script.Errors).Concat(sharedNameGroups.SelectMany(PapyrusScriptDeclaration.CreateSharedNameErrors)))
        {
            context.ReportDiagnostic(diagnostic);
        }
        if (outputDirectory != null)
        {
            // The .psc is an input to the Papyrus compiler, not to this compilation, so no generator API can produce it.
#pragma warning disable RS1035
            foreach (PapyrusScriptDeclaration script in scripts.Where(script => script.Errors.IsEmpty && !sharedNameGroups.Any(group => group.Contains(script))))
            {
                string path = Path.Combine(outputDirectory, script.ScriptName + ".psc");
                if (!File.Exists(path) || File.ReadAllText(path) != script.PscContents)
                {
                    Directory.CreateDirectory(outputDirectory);
                    File.WriteAllText(path, script.PscContents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                }
            }
            if (Directory.Exists(outputDirectory))
            {
                var declaredNames = new HashSet<string>(scripts.Select(script => script.ScriptName), StringComparer.OrdinalIgnoreCase);
                foreach (string path in Directory.EnumerateFiles(outputDirectory, "*.psc")
                    .Where(path => !declaredNames.Contains(Path.GetFileNameWithoutExtension(path)) && File.ReadAllText(path).Contains(PapyrusScriptDeclaration.GeneratedCommentPrefix)))
                {
                    File.Delete(path);
                }
            }
#pragma warning restore RS1035
        }
    }
}
