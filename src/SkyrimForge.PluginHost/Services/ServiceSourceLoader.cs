using SkyrimForge.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace SkyrimForge.PluginHost.Services;

internal static class ServiceSourceLoader
{
    internal static bool IsServiceSource(Type type) => type.IsAssignableTo(typeof(IPluginServiceSource)) && !type.IsAbstract;

    public static (IReadOnlyList<IPluginServiceSource> Sources, IReadOnlyList<string> Failures) Load(IEnumerable<string> paths, ILog log)
    {
        var sources = new List<IPluginServiceSource>();
        var failures = new List<string>();
        foreach (Type type in paths.SelectMany(path => GetSourceTypes(path, log)))
        {
            try
            {
                sources.Add((IPluginServiceSource)Activator.CreateInstance(type)!);
                log.Write($"{type.FullName} will add its services to every plugin.");
            }
            catch (Exception ex)
            {
                string failure = $"{type.FullName}: {PluginLoader.Describe(ex)}";
                failures.Add(failure);
                log.Write($"Service source load failed: {failure}");
            }
        }

        return (sources, failures);
    }

    // Services are looked up by Type, and one class loaded into two load contexts becomes two distinct Types.
    // Sources are loaded into the host's context, where a plugin's constructor parameters are resolved.
    private static readonly AssemblyLoadContext hostContext = AssemblyLoadContext.GetLoadContext(typeof(ServiceSourceLoader).Assembly) ?? AssemblyLoadContext.Default;

    private static IEnumerable<Type> GetSourceTypes(string path, ILog log)
    {
        Type[] types;
        try
        {
            types = hostContext.LoadFromAssemblyPath(path).GetTypes();
        }
        catch (BadImageFormatException)
        {
            // The file is a native DLL, such as a library's shim.
            return [];
        }
        catch (Exception ex)
        {
            log.Write($"{Path.GetFileName(path)} could not be read for service sources: {ex.GetType().Name}: {ex.Message}");
            return [];
        }

        return types.Where(IsServiceSource);
    }
}
