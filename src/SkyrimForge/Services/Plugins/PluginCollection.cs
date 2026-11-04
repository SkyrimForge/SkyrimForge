using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge;

internal sealed class PluginCollection : IPluginCollection, IEnumerable<IPlugin>
{
    private readonly List<IPlugin> plugins = [];

    internal void AddRange(IEnumerable<IPlugin> plugins)
    {
        this.plugins.AddRange(plugins);
    }

    public T Get<T>() where T : class
    {
        return GetOrNull<T>()
            ?? throw new InvalidOperationException(
                "No loaded plugin is of type " + typeof(T).FullName + ". " +
                "It is not installed, it failed to load, this was called before " + typeof(IPlugin).FullName + "." + nameof(IPlugin.Start) + "(), " +
                "or the assembly that declares the type is in a plugin's folder rather than in SharedLibraries, so each plugin that references the assembly loads a separate type.");
    }

    public T? GetOrNull<T>() where T : class
    {
        T[] matches = [.. plugins.OfType<T>()];
        if (matches.Length == 1) { return matches[0]; }
        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                "More than one " + typeof(IPlugin).FullName + " matches " + typeof(T).FullName + ": " +
                string.Join(", ", matches.Select(plugin => plugin.GetType().FullName)));
        }
        return default;
    }

    public IEnumerator<IPlugin> GetEnumerator() => plugins.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
