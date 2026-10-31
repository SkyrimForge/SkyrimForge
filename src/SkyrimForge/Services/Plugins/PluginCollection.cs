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
        var plugin = GetOrNull<T>();
        if (plugin != null) { return plugin; }
        throw new InvalidOperationException(
            "No loaded plugin is of type " + typeof(T).FullName + ". " +
            "It is not installed, it failed to load, this was called before " + typeof(IPlugin).FullName + "." + nameof(IPlugin.Start) + "(), " +
            "or the assembly that declares the type is in a plugin's folder rather than in SharedLibraries, so each plugin that references the assembly loads a separate type.");
    }

    public T? GetOrNull<T>() where T : class
    {
        var plugins = GetAll<T>();
        if (plugins.Length == 1) { return plugins[0]; }
        if (plugins.Length > 1)
        {
            throw new InvalidOperationException(
                "More than one " + typeof(IPlugin).FullName + " matches " + typeof(T).FullName + ": " +
                string.Join(", ", plugins.Select(plugin => plugin.GetType().FullName)));
        }
        return default;
    }

    private T[] GetAll<T>()
    {
        return [.. plugins.OfType<T>()];
    }

    public IEnumerator<IPlugin> GetEnumerator() => plugins.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
