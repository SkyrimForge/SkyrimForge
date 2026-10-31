using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SkyrimForge.Infrastructure;

/// <summary>Resolves its scope on each call, so a view of a list element addresses its index rather than the entries it was read from.</summary>
internal sealed class SavedStateEntries(CoSaveStore store, CoSaveScope scope) : ISavedStateEntries
{
    private readonly CoSaveStore store = store;
    private readonly CoSaveScope scope = scope;

    /// <inheritdoc/>
    public string? GetString(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return store.GetString(scope, key);
    }

    /// <inheritdoc/>
    public void SetString(string key, string? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        store.SetString(scope, key, value);
    }

    /// <inheritdoc/>
    public FormId<Form>? GetFormId(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return store.GetFormId(scope, key) is { } storedFormId ? new FormId<Form>(storedFormId) : null;
    }

    /// <inheritdoc/>
    public void SetFormId(string key, FormId<Form>? storedFormId)
    {
        ArgumentNullException.ThrowIfNull(key);
        store.SetFormId(scope, key, storedFormId?.Value);
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetStringKeys() => store.GetStringKeys(scope);

    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetFormIdKeys() => store.GetFormIdKeys(scope);

    ISavedStateEntries ISavedStateEntries.GetEntries(string key) => new SavedStateEntries(store, scope with { Path = [.. scope.Path, key] });

    IReadOnlyCollection<string> ISavedStateEntries.GetEntriesKeys() => store.GetEntriesKeys(scope);

    bool ISavedStateEntries.HasEntries(string key) => store.HasEntries(scope, key);

    void ISavedStateEntries.RemoveEntries(string key) => store.RemoveEntries(scope, key);

    void ISavedStateEntries.MoveEntries(string sourceKey, string destinationKey) => store.MoveEntries(scope, sourceKey, destinationKey);

    Lock ISavedStateEntries.EntriesLock => store.EntriesLock;
}
