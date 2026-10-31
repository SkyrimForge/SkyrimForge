using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyrimForge.PluginHost;

internal static class SharedLibraryPaths
{
    public static IReadOnlyDictionary<string, string> Find(string directory, ILog log)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(directory)) { return paths; }

        string[] folders = [.. Directory.GetDirectories(directory).Order(StringComparer.OrdinalIgnoreCase)];
        foreach (string folder in folders)
        {
            string name = Path.GetFileName(folder);
            string path = Path.Combine(folder, name + ".dll");
            if (File.Exists(path))
            {
                paths.Add(name, path);
            }
        }
        foreach (string path in folders.SelectMany(folder => Directory.GetFiles(folder, "*.dll").Order(StringComparer.OrdinalIgnoreCase)))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (!paths.TryAdd(name, path) && !string.Equals(paths[name], path, StringComparison.OrdinalIgnoreCase))
            {
                log.Write($"{Path.GetRelativePath(directory, path)} is not loaded, since {Path.GetRelativePath(directory, paths[name])} has the same name.");
            }
        }

        return paths;
    }
}
