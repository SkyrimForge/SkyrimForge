using SkyrimForge.Hosting;
using SkyrimForge.Papyrus;
using SkyrimForge.PluginHost.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

namespace SkyrimForge.PluginHost;

internal sealed class PluginLoader(
    string directory, ILog log, ServiceResolver serviceResolver, IReadOnlyList<IPluginServiceSource> serviceSources, IReadOnlyDictionary<string, string> sharedLibraryPaths)
{
    private readonly string directory = directory;
    private readonly IReadOnlyDictionary<string, string> sharedLibraryPaths = sharedLibraryPaths;
    private readonly ILog log = log;
    private readonly ServiceResolver serviceResolver = serviceResolver;
    private readonly IReadOnlyList<IPluginServiceSource> serviceSources = serviceSources;
    private readonly HashSet<string> identities = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> failures = [];
    private readonly Dictionary<IPlugin, PluginServiceResolver> resolversByPlugin = new(ReferenceEqualityComparer.Instance);

    private static readonly Version frameworkVersion = typeof(IPlugin).Assembly.GetName().Version!;

    private static readonly string noPluginClass = $"declares neither (1) a class that implements {typeof(IPlugin).FullName} and is not abstract nor (2) a class marked [PapyrusScript].";

    public IReadOnlyList<string> Failures => failures;

    public IReadOnlyList<IPlugin> Load()
    {
        if (!Directory.Exists(directory)) { return []; }

        var plugins = new List<IPlugin>();
        foreach (string path in Directory.EnumerateFiles(directory, "*.dll", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string pluginPath = Path.Combine(name, name + ".dll");
            string relativePath = Path.GetRelativePath(directory, path);
            if (string.Equals(relativePath, pluginPath, StringComparison.OrdinalIgnoreCase))
            {
                plugins.AddRange(LoadPlugins(path));
            }
            else if (ReferencesFramework(path))
            {
                log.Write($"{relativePath} is not loaded as a plugin, since a plugin's DLL is loaded only from a folder of the same name, {pluginPath}.");
            }
        }

        return plugins;
    }

    private List<IPlugin> LoadPlugins(string path)
    {
        string fileName = Path.GetFileName(path);
        Type[] types;
        try
        {
            Version? builtFor = GetFrameworkVersion(path);
            if (builtFor == null)
            {
                AddFailure($"{fileName}: {noPluginClass}");
                return [];
            }
            if (builtFor > frameworkVersion)
            {
                AddFailure($"{fileName}: built for {Product.Name} {builtFor.ToString(3)}, but {Product.Name} {frameworkVersion.ToString(3)} is installed.");
                return [];
            }
            types = new PluginLoadContext(path, sharedLibraryPaths).LoadFromAssemblyPath(path).GetTypes();
        }
        catch (Exception ex)
        {
            AddFailure($"{fileName}: {Describe(ex)}");
            return [];
        }

        var plugins = new List<IPlugin>();
        // Set before construction, so a DLL whose plugin class throws is not also reported as declaring none.
        bool declaresPluginClass = false;
        bool declaresServiceSource = false;
        foreach (Type type in types)
        {
            try
            {
                if (IsPluginClass(type))
                {
                    declaresPluginClass = true;
                    PluginServiceResolver pluginServiceResolver = CreateServiceResolver(type);
                    var plugin = (IPlugin)pluginServiceResolver.Construct();
                    resolversByPlugin.Add(plugin, pluginServiceResolver);
                    plugins.Add(plugin);
                }
                else if (type.IsDefined(typeof(PapyrusScriptAttribute)))
                {
                    declaresPluginClass = true;
                    AddPapyrusScript(type);
                }
                else if (ServiceSourceLoader.IsServiceSource(type))
                {
                    declaresServiceSource = true;
                    AddFailure($"{type.FullName}: its services are not registered, since the framework reads service sources only from {Bootstrap.SharedLibraryDirectoryName}. " +
                        "Setting SkyrimForgeSharedLibrary to true in its project builds it there.");
                }
            }
            catch (Exception ex)
            {
                AddFailure($"{type.FullName}: {Describe(ex)}");
            }
        }
        if (!declaresPluginClass && !declaresServiceSource)
        {
            AddFailure($"{fileName}: {noPluginClass}");
        }

        return plugins;
    }

    internal ILog GetLog(IPlugin plugin) => resolversByPlugin[plugin].GetRequired<ILog>();

    private void AddFailure(string failure)
    {
        failures.Add(failure);
        log.Write($"Plugin load failed: {failure}");
    }

    private static bool IsPluginClass(Type type) => type.IsAssignableTo(typeof(IPlugin)) && !type.IsAbstract;

    private static bool ReferencesFramework(string path)
    {
        try
        {
            return GetFrameworkVersion(path) != null;
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>SkyrimForge.Game counts, since a plugin of <see cref="PapyrusScriptAttribute"/> classes alone references only that assembly.</summary>
    private static Version? GetFrameworkVersion(string path)
    {
        using (var peReader = new PEReader(File.OpenRead(path)))
        {
            if (peReader.HasMetadata)
            {
                MetadataReader reader = peReader.GetMetadataReader();
                return reader.AssemblyReferences
                    .Select(reader.GetAssemblyReference)
                    .Where(reference => reader.StringComparer.Equals(reference.Name, Product.Name) || reader.StringComparer.Equals(reference.Name, Product.Name + ".Game"))
                    .Select(reference => reference.Version)
                    .Max();
            }
            return null;
        }
    }

    internal static string Describe(Exception ex)
    {
        Exception fault = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
        return $"{fault.GetType().Name}: {fault.Message}";
    }

    private PluginServiceResolver CreateServiceResolver(Type pluginType)
    {
        string identity = PluginServiceResolver.GetIdentity(pluginType);
        if (!identities.Add(identity))
        {
            throw new InvalidOperationException($"Another plugin already uses the identity \"{identity}\". Two plugins with one identity would share saved state and a log file.");
        }
        return new PluginServiceResolver(pluginType, serviceResolver, directory, serviceSources);
    }

    /// <summary>Registers native functions on data load, after the Papyrus VM exists.</summary>
    private void AddPapyrusScript(Type scriptType)
    {
        PluginServiceResolver scriptServiceResolver = CreateServiceResolver(scriptType);
        object script = scriptServiceResolver.Construct();
        IPapyrusFunctionRegistrar registrar = scriptServiceResolver.GetRequired<IPapyrusFunctionRegistrar>();
        scriptServiceResolver.GetRequired<IGameEvents>().OnDataLoaded.Subscribe(_ => PapyrusScriptRegistration.Register(registrar, script));
    }

    /// <summary>
    /// A second copy of a framework assembly would define a second <c>IPlugin</c> type, and services resolved by <c>typeof()</c> would not match the plugin's parameters.
    /// </summary>
    private sealed class PluginLoadContext(string pluginPath, IReadOnlyDictionary<string, string> sharedLibraryPaths) : AssemblyLoadContext
    {
        // Under the native host, the framework's context is a component load context, not Default.
        private static readonly AssemblyLoadContext hostContext = GetLoadContext(typeof(PluginLoadContext).Assembly) ?? Default;

        private readonly AssemblyDependencyResolver resolver = new(pluginPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            try
            {
                return hostContext.LoadFromAssemblyName(assemblyName);
            }
            catch (FileNotFoundException)
            {
                // The host context cannot resolve it, so it is a shared library or the plugin's own.
            }

            if (assemblyName.Name != null && sharedLibraryPaths.TryGetValue(assemblyName.Name, out string? sharedPath))
            {
                // Loaded into the host context, so every plugin receives the same instance.
                return hostContext.LoadFromAssemblyPath(sharedPath);
            }

            string? path = resolver.ResolveAssemblyToPath(assemblyName);
            return path != null ? LoadFromAssemblyPath(path) : null;
        }
    }
}
