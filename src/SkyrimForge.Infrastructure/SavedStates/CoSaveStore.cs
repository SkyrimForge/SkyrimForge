using System;
using System.Collections.Generic;
using System.Threading;

namespace SkyrimForge.Infrastructure;

// Plugins call it from concurrent worker threads, so every member acquires entriesLock and returns a copy.
internal sealed class CoSaveStore
{
    private readonly Lock entriesLock = new();

    internal Lock EntriesLock => entriesLock;

    private readonly Dictionary<string, CoSaveEntries> entriesByPlugin = new(StringComparer.Ordinal);

    private readonly Dictionary<string, Dictionary<uint, CoSaveEntries>> entriesByPluginAndForm = new(StringComparer.Ordinal);

    internal byte[] Encode()
    {
        lock (entriesLock)
        {
            return new CoSaveRecord(entriesByPlugin, entriesByPluginAndForm).Encode();
        }
    }

    internal void Set(string pluginTypeName, CoSaveEntries entries)
    {
        lock (entriesLock)
        {
            entriesByPlugin[pluginTypeName] = entries;
        }
    }

    internal void SetFormEntries(string pluginTypeName, Dictionary<uint, CoSaveEntries> entriesByForm)
    {
        lock (entriesLock)
        {
            entriesByPluginAndForm[pluginTypeName] = entriesByForm;
        }
    }

    internal void Clear()
    {
        lock (entriesLock)
        {
            entriesByPlugin.Clear();
            entriesByPluginAndForm.Clear();
        }
    }

    internal string? GetString(CoSaveScope scope, string key)
    {
        lock (entriesLock)
        {
            return Get(scope)?.Strings is { } strings && strings.TryGetValue(key, out string? value) ? value : null;
        }
    }

    internal void SetString(CoSaveScope scope, string key, string? value)
    {
        lock (entriesLock)
        {
            if (value != null)
            {
                GetForWrite(scope).GetStringsForWrite()[key] = value;
                return;
            }
            Get(scope)?.Strings?.Remove(key);
            RemoveEmptyStores(scope);
        }
    }

    internal uint? GetFormId(CoSaveScope scope, string key)
    {
        lock (entriesLock)
        {
            return Get(scope)?.Forms is { } forms && forms.TryGetValue(key, out uint storedFormId) ? storedFormId : null;
        }
    }

    internal void SetFormId(CoSaveScope scope, string key, uint? storedFormId)
    {
        lock (entriesLock)
        {
            if (storedFormId != null)
            {
                GetForWrite(scope).GetFormsForWrite()[key] = storedFormId.Value;
                return;
            }
            Get(scope)?.Forms?.Remove(key);
            RemoveEmptyStores(scope);
        }
    }

    internal IReadOnlyCollection<string> GetStringKeys(CoSaveScope scope)
    {
        lock (entriesLock)
        {
            return Get(scope)?.Strings?.Keys is { } keys ? [.. keys] : [];
        }
    }

    internal IReadOnlyCollection<string> GetFormIdKeys(CoSaveScope scope)
    {
        lock (entriesLock)
        {
            return Get(scope)?.Forms?.Keys is { } keys ? [.. keys] : [];
        }
    }

    internal IReadOnlyCollection<string> GetEntriesKeys(CoSaveScope scope)
    {
        lock (entriesLock)
        {
            return Get(scope)?.Children?.Keys is { } keys ? [.. keys] : [];
        }
    }

    internal bool HasEntries(CoSaveScope scope, string key)
    {
        lock (entriesLock)
        {
            return Get(scope)?.Children?.ContainsKey(key) == true;
        }
    }

    internal void RemoveEntries(CoSaveScope scope, string key)
    {
        lock (entriesLock)
        {
            Get(scope)?.Children?.Remove(key);
            RemoveEmptyStores(scope);
        }
    }

    internal void MoveEntries(CoSaveScope scope, string sourceKey, string destinationKey)
    {
        lock (entriesLock)
        {
            if (Get(scope)?.Children is not { } children)
            {
                return;
            }
            if (children.Remove(sourceKey, out CoSaveEntries? movedEntries))
            {
                children[destinationKey] = movedEntries;
            }
            else
            {
                children.Remove(destinationKey);
                RemoveEmptyStores(scope);
            }
        }
    }

    internal IReadOnlyCollection<uint> GetFormEntriesKeys(string pluginTypeName)
    {
        lock (entriesLock)
        {
            return entriesByPluginAndForm.GetValueOrDefault(pluginTypeName)?.Keys is { } keys ? [.. keys] : [];
        }
    }

    internal void RemoveFormEntries(string pluginTypeName, uint formId)
    {
        lock (entriesLock)
        {
            entriesByPluginAndForm.GetValueOrDefault(pluginTypeName)?.Remove(formId);
        }
    }

    // Called with entriesLock held.
    private CoSaveEntries? GetRoot(CoSaveScope scope)
    {
        return scope.FormId == null
            ? entriesByPlugin.GetValueOrDefault(scope.PluginTypeName)
            : entriesByPluginAndForm.GetValueOrDefault(scope.PluginTypeName)?.GetValueOrDefault(scope.FormId.Value);
    }

    // Called with entriesLock held.
    private CoSaveEntries? Get(CoSaveScope scope)
    {
        CoSaveEntries? entries = GetRoot(scope);
        foreach (string name in scope.Path)
        {
            entries = entries?.Children?.GetValueOrDefault(name);
        }
        return entries;
    }

    // Called with entriesLock held.
    private CoSaveEntries GetForWrite(CoSaveScope scope)
    {
        CoSaveEntries entries = GetRootForWrite(scope);
        foreach (string name in scope.Path)
        {
            entries = entries.GetChildForWrite(name);
        }
        return entries;
    }

    // Called with entriesLock held.
    private CoSaveEntries GetRootForWrite(CoSaveScope scope)
    {
        if (scope.FormId == null)
        {
            if (!entriesByPlugin.TryGetValue(scope.PluginTypeName, out CoSaveEntries? pluginEntries))
            {
                entriesByPlugin[scope.PluginTypeName] = pluginEntries = new CoSaveEntries();
            }
            return pluginEntries;
        }
        if (!entriesByPluginAndForm.TryGetValue(scope.PluginTypeName, out Dictionary<uint, CoSaveEntries>? entriesByForm))
        {
            entriesByPluginAndForm[scope.PluginTypeName] = entriesByForm = [];
        }
        if (!entriesByForm.TryGetValue(scope.FormId.Value, out CoSaveEntries? entries))
        {
            entriesByForm[scope.FormId.Value] = entries = new CoSaveEntries();
        }
        return entries;
    }

    // Called with entriesLock held. An empty store left in place would read as a present key.
    private void RemoveEmptyStores(CoSaveScope scope)
    {
        var parents = new Stack<(CoSaveEntries Parent, string Name)>();
        CoSaveEntries? entries = GetRoot(scope);
        foreach (string name in scope.Path)
        {
            if (entries?.Children?.GetValueOrDefault(name) is not { } child)
            {
                break;
            }
            parents.Push((entries, name));
            entries = child;
        }
        while (parents.TryPop(out (CoSaveEntries Parent, string Name) link) && link.Parent.Children![link.Name].IsEmpty())
        {
            link.Parent.Children.Remove(link.Name);
        }
        if (scope.FormId != null && GetRoot(scope)?.IsEmpty() == true)
        {
            entriesByPluginAndForm[scope.PluginTypeName].Remove(scope.FormId.Value);
        }
    }
}
