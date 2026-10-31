using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SkyrimForge.Infrastructure;

internal sealed class CoSaveFormIdResolver(Func<uint, uint?> resolveFormId, ILog log)
{
    private readonly Func<uint, uint?> resolveFormId = resolveFormId;
    private readonly ILog log = log;

    internal CoSaveEntries Resolve(CoSaveEntries storedEntries)
    {
        Dictionary<string, uint>? resolvedForms = null;
        if (storedEntries.Forms != null)
        {
            resolvedForms = new Dictionary<string, uint>(StringComparer.Ordinal);
            foreach ((string key, uint storedFormId) in storedEntries.Forms)
            {
                if (resolveFormId(storedFormId) is { } currentId)
                {
                    resolvedForms[key] = currentId;
                }
                else
                {
                    log.Write($"The stored form 0x{storedFormId:X8} is no longer loaded. It was dropped.");
                }
            }
        }
        Dictionary<string, string>? resolvedStrings = storedEntries.Strings?.ToDictionary(pair => pair.Key, pair => ResolveText(pair.Value), StringComparer.Ordinal);
        // A damaged record can contain a null nested store.
        Dictionary<string, CoSaveEntries>? resolvedChildren = storedEntries.Children?
            .Where(pair => pair.Value != null)
            .Select(pair => KeyValuePair.Create(pair.Key, Resolve(pair.Value)))
            .Where(pair => !pair.Value.IsEmpty())
            .ToDictionary(StringComparer.Ordinal);
        return new CoSaveEntries { Strings = resolvedStrings, Forms = resolvedForms, Children = resolvedChildren };
    }

    internal Dictionary<uint, CoSaveEntries> ResolveFormEntries(Dictionary<uint, CoSaveEntries> entriesByForm)
    {
        var resolvedEntries = new Dictionary<uint, CoSaveEntries>();
        foreach ((uint storedFormId, CoSaveEntries entries) in entriesByForm)
        {
            if (resolveFormId(storedFormId) is not { } currentId)
            {
                log.Write($"The form 0x{storedFormId:X8} is no longer loaded. Its entries were dropped.");
                continue;
            }
            // A store left empty by dropped form IDs would list the form as having entries.
            if (entries != null && Resolve(entries) is { } currentEntries && !currentEntries.IsEmpty())
            {
                resolvedEntries[currentId] = currentEntries;
            }
        }
        return resolvedEntries;
    }

    // JSON that repeats a property name parses, then throws ArgumentException when its properties are read.
    private string ResolveText(string text)
    {
        if (!text.Contains("$form:", StringComparison.Ordinal))
        {
            return text;
        }
        try
        {
            return ResolveNode(JsonNode.Parse(text))?.ToJsonString() ?? "null";
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            log.Write($"Stored text containing a form ID could not be read as JSON. Its form IDs were not translated: {ex.Message}");
            return text;
        }
    }

    private JsonNode? ResolveNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonValue value when value.TryGetValue(out string? text) && FormIdJsonConverter.TryParse(text, out uint storedFormId):
                return ResolveFormIdText(storedFormId) is { } currentId ? JsonValue.Create(currentId) : null;
            case JsonArray array:
                for (int i = 0; i < array.Count; i++)
                {
                    JsonNode? element = array[i];
                    JsonNode? resolvedNode = ResolveNode(element);
                    if (!ReferenceEquals(resolvedNode, element))
                    {
                        array[i] = resolvedNode;
                    }
                }
                return array;
            case JsonObject jsonObject:
                // Cleared and rebuilt, because renaming a property requires removing it, and a node must be detached before it is added again.
                KeyValuePair<string, JsonNode?>[] properties = [.. jsonObject];
                jsonObject.Clear();
                foreach ((string name, JsonNode? propertyValue) in properties)
                {
                    string? resolvedName = FormIdJsonConverter.TryParse(name, out uint storedFormId) ? ResolveFormIdText(storedFormId) : name;
                    if (resolvedName != null)
                    {
                        jsonObject[resolvedName] = ResolveNode(propertyValue);
                    }
                }
                return jsonObject;
            default:
                return node;
        }
    }

    private string? ResolveFormIdText(uint storedFormId)
    {
        if (resolveFormId(storedFormId) is { } currentId)
        {
            return FormIdJsonConverter.Format(currentId);
        }
        log.Write($"The form 0x{storedFormId:X8} in stored text is no longer loaded. It was replaced with null, or the property it named was removed.");
        return null;
    }
}
