using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SkyrimForge.Infrastructure;

internal sealed class PerPluginSavedState(CoSave coSave, CoSaveStore store, string pluginTypeName)
    : SavedStateEntries(store, new CoSaveScope(pluginTypeName, null, [])), IPerPluginSavedState
{
    /// <inheritdoc/>
    public ISavedStateEntries GetFormEntries(FormId<Form> formId) => new SavedStateEntries(Store, new CoSaveScope(pluginTypeName, formId.Value, []));

    /// <inheritdoc/>
    public IReadOnlyCollection<FormId<Form>> GetFormEntriesKeys() => [.. Store.GetFormEntriesKeys(pluginTypeName).Select(formId => new FormId<Form>(formId))];

    /// <inheritdoc/>
    public void RemoveFormEntries(FormId<Form> formId) => Store.RemoveFormEntries(pluginTypeName, formId.Value);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> ReadStringsFromSave(string saveFileName)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        IReadOnlyDictionary<string, string>? strings = coSave.ReadEntries(pluginTypeName, saveFileName)?.Strings;
        return strings ?? ReadOnlyDictionary<string, string>.Empty;
    }
}
