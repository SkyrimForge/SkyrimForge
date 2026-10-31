using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class RuntimeQuestRecord(GameTarget gameTarget, IUtility utility, IGameSession session, ILog log, SavedQuestStore store, QuestDeclaration declaration, string name, QUEST_DATA.Type type)
{
    // The game's own quests use instance 1.
    private const uint instance = 1;

    private readonly GameTarget gameTarget = gameTarget;
    private readonly IUtility utility = utility;
    private readonly IGameSession session = session;
    private readonly ILog log = log;
    private readonly SavedQuestStore store = store;
    private readonly QuestDeclaration declaration = declaration;
    private readonly string name = name;
    private readonly QUEST_DATA.Type type = type;

    // Each target is allocated the first time its marker is shown and retained until the process exits.
    private readonly Dictionary<(ushort Objective, uint AliasId), TESQuestTarget> questTargets = [];

    private QuestRecordAliases? recordAliases;

    private Task? startOrStopTask;

    internal Quest? Quest { get; private set; }

    internal void Build()
    {
        Quest record = CreateForm(name);
        Quest = record;
        recordAliases = new QuestRecordAliases(gameTarget, record, declaration);
    }

    internal Quest CreateForm(string formName)
    {
        Quest record = RuntimeForms.Create(FormType.Quest).AsQuest()!.Value;
        record.AsTESFullName().SetFullName(formName);
        QUEST_DATA data = record.Data;
        data.QuestType = type;
        // Neither enabled nor run once. Quest.Start enables a quest and lists it with the story teller only if the quest is not already enabled,
        // and a quest that runs once does not start again after it stops.
        data.Flags = QuestFlag.kDisplayedInHUD;
        data.Priority = 60;
        record.CurrentInstanceID = instance;
        gameTarget.DataHandler.AddFormToDataHandler(record);
        return record;
    }

    internal void SetPackage(QuestReferenceAlias alias, ObjectReference reference, Package? package) => recordAliases?.SetPackage(recordAliases.GetId(alias), reference, package);

    internal void RemoveAliasInstance(QuestReferenceAlias alias, ObjectReference reference) => recordAliases?.RemoveInstance(recordAliases.GetId(alias), reference);

    internal void ApplyAfterLoad()
    {
        // A save does not record the quest, so a quest started before the load is still enabled after it.
        if (Quest is { } record && record.IsEnabled())
        {
            record.Stop();
            startOrStopTask = ApplyOnceStartedOrStoppedAsync(record, true, true, session.NewGameOrSaveLoading);
        }
        else
        {
            Apply(true);
        }
    }

    // A save records the player's objectives by quest ID, which can resolve to this quest or to nothing, so the entries are replaced.
    // The active mark is restored only from a load, because the player may have set it in the journal since the last one.
    internal void Apply(bool restoreActive)
    {
        if (Quest is not { } quest || recordAliases == null)
        {
            return;
        }
        SavedQuest savedQuest = store.Read();
        (SavedObjective SavedObjective, BGSQuestObjective Objective)[] objectivePairs = ApplyObjectives(quest, recordAliases, savedQuest);
        ApplyJournalObjectives(quest, objectivePairs);
        ApplyFlags(quest, savedQuest, objectivePairs.Length > 0, restoreActive);
        StartOrStop(quest, recordAliases, objectivePairs.Length > 0);
        QuestDescriptions.Set(quest, savedQuest.Description, name, log);
    }

    private (SavedObjective SavedObjective, BGSQuestObjective Objective)[] ApplyObjectives(Quest quest, QuestRecordAliases aliases, SavedQuest savedQuest)
    {
        Dictionary<ushort, List<ObjectiveTarget>> targets =
            savedQuest.Objectives.ToDictionary(objective => objective.Index, objective => GetTargets(objective, aliases, savedQuest));
        bool IsMarked(SavedObjective objective) => objective.IsOpen && targets[objective.Index].Count > 0;
        HashSet<ushort> markedIndexes = [.. savedQuest.Objectives.Where(IsMarked).Select(objective => objective.Index)];
        foreach (BGSQuestObjective objective in quest.Objectives.Get())
        {
            BGSQuestObjective objectiveCopy = objective;
            objectiveCopy.State = QUEST_OBJECTIVE_STATE.kDormant;
            if (objectiveCopy.NumTargets > 0 && !markedIndexes.Contains(objectiveCopy.Index))
            {
                objectiveCopy.Targets.Replace([]);
            }
        }
        var objectivePairs = savedQuest.Objectives
            .Select(savedObjective => (SavedObjective: savedObjective, Objective: quest.SetObjective(savedObjective.Index, savedObjective.Text))).ToArray();
        foreach (var (savedObjective, objective) in objectivePairs)
        {
            BGSQuestObjective objectiveCopy = objective;
            objectiveCopy.State = savedObjective.State;
        }
        nint[] questTargetAddresses = [.. objectivePairs.Where(pair => IsMarked(pair.SavedObjective))
            .SelectMany(pair => SetMarkers(aliases, pair.Objective, pair.SavedObjective.Index, targets[pair.SavedObjective.Index])).Select(questTarget => questTarget.Address)];
        NativeSupplements.SF_Player_SetQuestTargets(quest.Address, questTargetAddresses, questTargetAddresses.Length);
        return objectivePairs;
    }

    private void ApplyJournalObjectives(Quest quest, (SavedObjective SavedObjective, BGSQuestObjective Objective)[] objectivePairs)
    {
        var objectives = gameTarget.Player.PlayerRuntimeData.Objectives;
        IReadOnlyList<BGSInstancedQuestObjective> current = objectives.Get();
        for (int i = current.Count - 1; i >= 0; i--)
        {
            if (current[i].Objective?.OwnerQuest?.Address == quest.Address)
            {
                objectives.RemoveAt(i);
            }
        }
        foreach (var (savedObjective, objective) in objectivePairs)
        {
            BGSInstancedQuestObjective entry = BGSInstancedQuestObjective.New();
            entry.Objective = objective;
            entry.InstanceID = instance;
            entry.InstanceState = savedObjective.State;
            objectives.Add(entry);
            entry.Delete();
        }
    }

    private static void ApplyFlags(Quest quest, SavedQuest savedQuest, bool showsObjective, bool restoreActive)
    {
        bool finished = savedQuest.Completed || savedQuest.Failed;
        QUEST_DATA data = quest.Data;
        // kActive is the journal's active-quest mark, which the game's own completed quests do not have and the completed list gives no way to clear.
        QuestFlag flags = data.Flags & ~(QuestFlag.kCompleted | QuestFlag.kFailed);
        if (finished)
        {
            flags = (flags | QuestFlag.kCompleted) & ~QuestFlag.kActive;
        }
        if (savedQuest.Failed)
        {
            flags |= QuestFlag.kFailed;
        }
        if (!showsObjective)
        {
            flags &= ~QuestFlag.kActive;
        }
        else if (restoreActive && !finished)
        {
            flags = savedQuest.Active ? flags | QuestFlag.kActive : flags & ~QuestFlag.kActive;
        }
        data.Flags = flags;
    }

    private void StartOrStop(Quest quest, QuestRecordAliases aliases, bool runs)
    {
        // CommonLib's IsRunning is true of a quest never started, testing only that it is neither stopping nor promoting.
        bool started = quest.IsEnabled();
        if (runs)
        {
            ForceAliases(aliases);
        }
        // A wait in progress applies the quest again once the start or stop finishes. A quest still stopping is not started, since the stop ends what the start begins.
        if (startOrStopTask is not { IsCompleted: false })
        {
            if (runs && !started && !quest.IsStopping())
            {
                if (!quest.Start())
                {
                    log.Write($"The quest \"{name}\" did not start.");
                }
            }
            else if (!runs && started)
            {
                quest.Stop();
            }
            // Starting a quest sets its objectives dormant and removes their markers, so the quest is applied again as its start or stop finishes.
            if (quest.IsStarting() || quest.IsStopping())
            {
                startOrStopTask = ApplyOnceStartedOrStoppedAsync(quest, false, false, session.NewGameOrSaveLoading);
            }
        }
    }

    // A stop made at a load leaves the quest enabled until a frame after the load, so with stopped set the wait also lasts until the quest is disabled.
    private async Task ApplyOnceStartedOrStoppedAsync(Quest quest, bool restoreActive, bool stopped, CancellationToken newGameOrSaveLoadingToken)
    {
        const int maxWaitFrames = 120;
        bool IsChanging() => quest.IsStarting() || quest.IsStopping() || (stopped && quest.IsEnabled());
        int waitedFrameCount = 0;
        do
        {
            await utility.WaitAsync(TimeSpan.Zero, newGameOrSaveLoadingToken);
            waitedFrameCount++;
        }
        while (waitedFrameCount < maxWaitFrames && IsChanging());
        if (IsChanging())
        {
            log.Write($"The quest \"{name}\" had not finished starting or stopping after {maxWaitFrames} frames and is applied anyway.");
        }
        // Cleared so that Apply starts or stops the quest. A start or stop it makes sets a new startOrStopTask, which this awaits.
        startOrStopTask = null;
        Apply(restoreActive);
        if (startOrStopTask is { IsCompleted: false } pendingTask)
        {
            await pendingTask;
        }
    }

    internal void ShowBanner(HUD_MESSAGE_TYPE type, string text)
    {
        if (Quest is not { } record)
        {
            return;
        }
        UIMessageQueue queue = gameTarget.UIMessageQueue;
        if (gameTarget.InterfaceStrings.HudData is not { } dataName || queue.CreateUIMessageData(dataName) is not { } messageData || HUDData.OrNull(messageData.Address) is not { } banner)
        {
            log.Write($"The quest \"{name}\" showed no {type} banner because the game created no HUD data.");
            return;
        }
        banner.Type = type;
        banner.Text = text;
        banner.Quest = record;
        queue.AddMessage(HUDMenu.MenuName, UI_MESSAGE_TYPE.kUpdate, messageData);
    }

    private List<TESQuestTarget> SetMarkers(QuestRecordAliases aliases, BGSQuestObjective objective, ushort index, IReadOnlyList<ObjectiveTarget> targets)
    {
        List<TESQuestTarget> createdTargets = [];
        foreach ((uint aliasId, FormId<ObjectReference> targetId) in targets)
        {
            if (targetId.FormOrNull is not { } targetReference)
            {
                log.Write($"Objective {index} of the quest \"{name}\" has no marker at {targetId} because no reference has that ID.");
                continue;
            }
            if (!questTargets.TryGetValue((index, aliasId), out TESQuestTarget target))
            {
                target = TESQuestTarget.New();
                target.Alias = aliasId;
                questTargets.Add((index, aliasId), target);
            }
            aliases.Force(aliasId, targetReference);
            createdTargets.Add(target);
        }
        // Rewritten only if it differs, since each write allocates the objective a new array of targets.
        if (!objective.Targets.Get().Select(target => target.Address).SequenceEqual(createdTargets.Select(target => target.Address)))
        {
            objective.Targets.Replace(createdTargets);
        }
        return createdTargets;
    }

    private void ForceAliases(QuestRecordAliases aliases)
    {
        foreach (QuestReferenceAlias alias in declaration.Aliases)
        {
            if (store.GetReference(alias)?.FormOrNull is { } reference)
            {
                aliases.Force(aliases.GetId(alias), reference);
            }
        }
    }

    private List<ObjectiveTarget> GetTargets(SavedObjective objective, QuestRecordAliases aliases, SavedQuest savedQuest)
    {
        QuestObjective? declaredObjective = declaration.Objectives.GetValueOrDefault(objective.Index);
        List<ObjectiveTarget> targets = [];
        foreach (QuestReferenceAlias alias in declaredObjective?.Targets ?? [])
        {
            if (store.GetReference(alias) is { } reference)
            {
                targets.Add(new(aliases.GetId(alias), reference));
            }
        }
        foreach (QuestLocationAlias alias in declaredObjective?.LocationTargets ?? [])
        {
            Action<string>? report = objective.IsOpen
                ? reason => log.Write($"Objective {objective.Index} of the quest \"{name}\" has no marker for the location alias \"{alias.Name}\": {reason}.")
                : null;
            if (store.GetLocationMarker(alias, report) is { } marker)
            {
                targets.Add(new(aliases.GetId(alias), marker));
            }
        }
        IReadOnlyList<FormId<ObjectReference>> references = savedQuest.ObjectiveTargets.GetValueOrDefault(objective.Index) ?? [];
        targets.AddRange(references.Select((reference, position) => new ObjectiveTarget(aliases.GetId(objective.Index, position), reference)));
        return targets;
    }

    private sealed record ObjectiveTarget(uint AliasId, FormId<ObjectReference> ReferenceId);
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_Player_SetQuestTargets(nint quest, [In] nint[] targets, int count);
}
