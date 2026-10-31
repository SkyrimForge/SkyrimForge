using SkyrimForge.Game;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A plugin's state between sessions, stored in the SKSE co-save beside the player's save file.</summary>
/// <remarks>
/// A value set is written with the next save, so a session that ends without saving loses it.
/// Members can be called on any thread. The store is emptied when a new game starts or a save starts loading,
/// and it has the loaded save's entries from <see cref="IGameEvents.OnSaveLoaded"/> onward.
/// Each member is atomic. A read followed by a write from more than one thread needs a lock of the plugin's own.
/// Keys that start with "SkyrimForge." belong to the framework,
/// and <see cref="ISavedStateEntries.GetStringKeys"/> and <see cref="ISavedStateEntries.GetFormIdKeys"/> return them too.
/// </remarks>
public interface IPerPluginSavedState : ISavedStateEntries
{
    /// <summary>Returns the part of this plugin's store belonging to one form, for state about a particular object.</summary>
    /// <remarks>Entries under a form whose .esm, .esp, or .esl file is no longer loaded are dropped as the save is read.</remarks>
    ISavedStateEntries GetFormEntries(FormId<Form> formId);

    /// <summary>Returns the IDs of the forms that have entries in this plugin's store, including forms that have only the framework's entries.</summary>
    IReadOnlyCollection<FormId<Form>> GetFormEntriesKeys();

    /// <summary>Removes every entry in this plugin's store under one form.</summary>
    /// <remarks>This includes the framework's entries under the form, such as a note's pages and the faction ranks an actor had before it filled a quest alias.</remarks>
    void RemoveFormEntries(FormId<Form> formId);

    /// <summary>Reads the text this plugin stored in a save, from that save's co-save file, without loading the save.</summary>
    /// <param name="saveFileName">The save's file name, as <see cref="SaveEventArgs.FileName"/> reports it, with or without the .ess extension.</param>
    /// <returns>
    /// Each key and its text as <see cref="ISavedStateEntries.SetString"/> stored them when the save was written,
    /// or empty if the save has no co-save or this plugin stored no text in it.
    /// </returns>
    /// <remarks>A form ID written into that text is the number the save recorded, unmapped.</remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="saveFileName"/> is <c>null</c>.</exception>
    /// <exception cref="System.IO.InvalidDataException">
    /// The co-save is not one SKSE wrote, or the framework's record in it does not decode at the version this build reads.
    /// </exception>
    /// <exception cref="System.IO.IOException">The co-save file exists and could not be read.</exception>
    IReadOnlyDictionary<string, string> ReadStringsFromSave(string saveFileName);
}
