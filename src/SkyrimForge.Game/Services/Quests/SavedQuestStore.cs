using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SkyrimForge.Game;

internal sealed class SavedQuestStore(IPerPluginSavedState savedState, ISavedStateRecordPair<SavedQuest> questPair)
{
    private static readonly SavedQuest notStartedQuest = new(false, false, [], "", false, [], new Dictionary<ushort, string>());

    private readonly IPerPluginSavedState savedState = savedState;
    private readonly ISavedStateRecordPair<SavedQuest> questPair = questPair;

    // Alias events read the quest for every object the game loads, so the text is parsed again only when it has changed.
    // One reference, so a reader on another worker thread copies a text and the quest parsed from it together.
    private ParsedQuest? lastRead;

    internal string Key => questPair.Key;

    internal SavedQuest Read()
    {
        string? text = savedState.GetString(questPair.Key);
        ParsedQuest? parsed = lastRead;
        if (parsed == null || text != parsed.Text)
        {
            // Parsed from the text just read, as the pair parses it. Reading the pair again could return a text written in between.
            parsed = new ParsedQuest(text, (text != null ? JsonSerializer.Deserialize<SavedQuest>(text) : null) ?? notStartedQuest);
            lastRead = parsed;
        }
        return parsed.SavedQuest;
    }

    internal void Write(SavedQuest? savedQuest) => questPair.Value = savedQuest;

    // Written without being applied, since the game already shows the mark and plays the scenes.
    internal void WriteSaveTimeState(bool active, IReadOnlyDictionary<string, int> scenePhases, ScenePlayerControls disabledControls, bool playerDriven, IReadOnlyList<string> saidLines)
    {
        SavedQuest savedQuest = Read();
        bool sameScenes = savedQuest.Scenes.Count == scenePhases.Count && scenePhases.All(entry => savedQuest.Scenes.TryGetValue(entry.Key, out int phase) && phase == entry.Value);
        bool sameSaidLines = savedQuest.SaidLines.Count == saidLines.Count && saidLines.All(savedQuest.SaidLines.Contains);
        if (savedQuest.Active != active || !sameScenes || savedQuest.DisabledControls != disabledControls || savedQuest.PlayerDriven != playerDriven || !sameSaidLines)
        {
            questPair.Value = savedQuest with { Active = active, Scenes = scenePhases, DisabledControls = disabledControls, PlayerDriven = playerDriven, SaidLines = saidLines };
        }
    }

    internal FormId<ObjectReference>? GetReference(QuestReferenceAlias alias)
    {
        if (alias == QuestReferenceAlias.Player)
        {
            return KnownForms.PlayerRef.Cast<ObjectReference>();
        }
        // A reference whose .esm, .esp, or .esl file is absent from the load order reads as zero.
        return Read().Aliases.GetValueOrDefault(alias.Name)?.ReferenceId is { Value: not 0 } referenceId ? referenceId : null;
    }

    internal FormId<Location>? GetLocationId(QuestLocationAlias alias) =>
        // A location whose .esm, .esp, or .esl file is absent from the load order reads as zero.
        Read().LocationAliases.TryGetValue(alias.Name, out FormId<Location> locationId) && locationId.Value != 0 ? locationId : null;

    internal FormId<ObjectReference>? GetLocationMarker(QuestLocationAlias alias, Action<string>? report = null)
    {
        if (GetLocationId(alias) is not { } locationId)
        {
            return null;
        }
        if (locationId.FormOrNull is not { } location)
        {
            report?.Invoke($"no location has the ID {locationId}");
            return null;
        }
        if (location.WorldLocMarker is { } marker)
        {
            return marker.FormId;
        }
        report?.Invoke($"the location {locationId} has no world location marker");
        return null;
    }

    private sealed record ParsedQuest(string? Text, SavedQuest SavedQuest);
}
