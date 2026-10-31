using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace SkyrimForge.Infrastructure;

internal sealed class PerPluginSavedState(CoSave coSave, CoSaveStore store, string pluginTypeName) : IPerPluginSavedState
{
    private readonly CoSave coSave = coSave;
    private readonly CoSaveStore store = store;
    private readonly string pluginTypeName = pluginTypeName;
    private readonly ISavedStateEntries entries = new SavedStateEntries(store, new CoSaveScope(pluginTypeName, null, []));

    /// <inheritdoc/>
    public ISavedStateEntries GetFormEntries(FormId<Form> formId) => new SavedStateEntries(store, new CoSaveScope(pluginTypeName, formId.Value, []));

    /// <inheritdoc/>
    public IReadOnlyCollection<FormId<Form>> GetFormEntriesKeys() => [.. store.GetFormEntriesKeys(pluginTypeName).Select(formId => new FormId<Form>(formId))];

    /// <inheritdoc/>
    public void RemoveFormEntries(FormId<Form> formId) => store.RemoveFormEntries(pluginTypeName, formId.Value);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> ReadStringsFromSave(string saveFileName)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        IReadOnlyDictionary<string, string>? strings = coSave.ReadEntries(pluginTypeName, saveFileName)?.Strings;
        return strings ?? ReadOnlyDictionary<string, string>.Empty;
    }

    /// <inheritdoc/>
    public string? GetString(string key) => entries.GetString(key);

    /// <inheritdoc/>
    public void SetString(string key, string? value) => entries.SetString(key, value);

    /// <inheritdoc/>
    public FormId<Form>? GetFormId(string key) => entries.GetFormId(key);

    /// <inheritdoc/>
    public void SetFormId(string key, FormId<Form>? formId) => entries.SetFormId(key, formId);

    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetStringKeys() => entries.GetStringKeys();

    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetFormIdKeys() => entries.GetFormIdKeys();

    ISavedStateEntries ISavedStateEntries.GetEntries(string key) => entries.GetEntries(key);

    IReadOnlyCollection<string> ISavedStateEntries.GetEntriesKeys() => entries.GetEntriesKeys();

    bool ISavedStateEntries.HasEntries(string key) => entries.HasEntries(key);

    void ISavedStateEntries.RemoveEntries(string key) => entries.RemoveEntries(key);

    void ISavedStateEntries.MoveEntries(string sourceKey, string destinationKey) => entries.MoveEntries(sourceKey, destinationKey);

    Lock ISavedStateEntries.EntriesLock => entries.EntriesLock;
}
