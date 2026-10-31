using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed partial class RuntimeQuest : IRuntimeQuest, IQuestObjectives, IQuestStages, ISceneOwner
{
    [GeneratedRegex("<Alias=([^>]+)>")]
    private static partial Regex AliasToken { get; }

    private readonly RuntimeQuestFactory factory;
    private readonly QuestDeclaration declaration;
    private readonly bool activeOnStart;
    private readonly SavedQuestStore store;
    private readonly RuntimeQuestRecord record;
    private readonly IGameSession session;
    private readonly ILog log;

    internal RuntimeQuest(RuntimeQuestFactory factory, string name, QuestDeclaration declaration, bool activeOnStart, SavedQuestStore store, RuntimeQuestRecord record, IGameSession session, ILog log, Func<RuntimeQuest, RuntimeQuestAliases> createAliases, Func<RuntimeQuest, RuntimeQuestScenes> createScenes)
    {
        this.factory = factory;
        this.declaration = declaration;
        this.activeOnStart = activeOnStart;
        this.store = store;
        this.record = record;
        this.session = session;
        this.log = log;
        Name = name;
        Aliases = createAliases(this);
        Scenes = createScenes(this);
    }

    public string Name { get; }

    IQuestScenes IRuntimeQuest.Scenes => Scenes;

    IRuntimeQuest IQuestObjectives.Quest => this;

    IRuntimeQuest IQuestStages.Quest => this;

    internal Quest? Quest => record.Quest;

    internal RuntimeQuestAliases Aliases { get; }

    internal RuntimeQuestScenes Scenes { get; }

    FormId<ObjectReference>? ISceneOwner.GetReference(QuestReferenceAlias alias) => store.GetReference(alias);

    FormId<ObjectReference>? ISceneOwner.GetLocationMarker(QuestLocationAlias alias) => store.GetLocationMarker(alias);

    Quest ISceneOwner.CreateSceneQuestRecord(string name) => record.CreateForm(name);

    IReadOnlyList<string> ISceneOwner.SavedSaidLines => store.Read().SaidLines;

    IRuntimeQuest ISceneOwner.Quest => this;

    IEnumerable<RuntimeQuestScenes> ISceneOwner.PluginScenes => factory.Scenes;

    internal void Build()
    {
        record.Build();
        Scenes.Build();
    }

    internal void ApplyToGame()
    {
        record.Apply(true);
        Aliases.ApplyActorStates();
    }

    internal void ApplyAfterLoad()
    {
        record.ApplyAfterLoad();
        Aliases.ApplyActorStates();
        Scenes.StopQuests();
        SavedQuest savedQuest = store.Read();
        Scenes.Resume(savedQuest.Scenes, savedQuest.DisabledControls, savedQuest.PlayerDriven);
    }

    internal void WriteSaveTimeState()
    {
        if (record.Quest is { } quest)
        {
            store.WriteSaveTimeState(quest.Data.Flags.HasFlag(QuestFlag.kActive), Scenes.GetResumePhases(), Scenes.DisabledControls, Scenes.DrivesPlayer, Scenes.GetSaidLines());
        }
    }

    public string Description
    {
        get => store.Read().Description;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Write(store.Read() with { Description = ReplaceAliasTokens(value) });
        }
    }

    public bool IsStarted => store.Read().Objectives.Count > 0;

    public bool IsCompleted => store.Read().Completed;

    public bool IsFailed => store.Read().Failed;

    public QuestStage? CurrentStage => store.Read().Stages.Select(index => declaration.Stages.GetValueOrDefault(index)).LastOrDefault(stage => stage != null);

    void IQuestObjectives.Display(QuestObjective objective)
    {
        ushort index = GetIndex(objective);
        Display(index, GetText(store.Read(), index, objective));
    }

    private void Display(ushort index, string text)
    {
        string displayedText = ReplaceAliasTokens(text);
        SavedQuest savedQuest = store.Read();
        SavedObjective? savedObjective = savedQuest.Objectives.FirstOrDefault(objective => objective.Index == index);
        SavedObjective displayedObjective = new(index, displayedText, savedObjective?.Completed == true, savedObjective?.Failed == true);
        SavedQuest displayedQuest = savedQuest with { Objectives = [.. savedQuest.Objectives.Where(objective => objective.Index != index), displayedObjective] };
        if (activeOnStart && savedQuest.Objectives.Count == 0)
        {
            store.Write(displayedQuest with { Active = true });
            record.Apply(true);
        }
        else
        {
            Write(displayedQuest);
        }
        if (savedObjective == null)
        {
            if (savedQuest.Objectives.Count == 0)
            {
                record.ShowBanner(HUD_MESSAGE_TYPE.kQuestStarted, Name);
            }
            record.ShowBanner(HUD_MESSAGE_TYPE.kObjectiveStarted, displayedText);
        }
    }

    void IQuestObjectives.Complete(QuestObjective objective) => EndObjective(GetIndex(objective), failed: false);

    void IQuestObjectives.Fail(QuestObjective objective) => EndObjective(GetIndex(objective), failed: true);

    public void Complete(bool completeObjectives = false) => Finish(completeObjectives, failed: false);

    public void Fail(bool failObjectives = false) => Finish(failObjectives, failed: true);

    public void RemoveFromJournalAndReset()
    {
        Scenes.StopAll();
        Aliases.ReleaseAllAliases();
        Scenes.ClearSaidLines();
        store.Write(null);
        record.Apply(false);
    }

    async Task<bool> IQuestStages.SetAsync(QuestStage stage)
    {
        SavedQuest savedQuest = store.Read();
        if (savedQuest.Stages.Contains(stage.Index))
        {
            return false;
        }
        // Read again, since a computed log entry can change the quest.
        string? logEntry = stage.GetLogEntry();
        savedQuest = store.Read();
        // Tested and read before anything is written, so a stage is applied whole or not at all.
        if (stage.QuestOutcome != null && savedQuest.Objectives.Count == 0 && stage.ObjectivesToDisplay.Count == 0)
        {
            throw new InvalidOperationException($"Stage {stage.Index} finishes the quest \"{Name}\" without displaying an objective, and the quest has displayed none.");
        }
        (ushort Index, string Text)[] displayedObjectives =
            [.. stage.ObjectivesToDisplay.Select(objective => (GetIndex(objective), GetText(savedQuest, GetIndex(objective), objective)))];
        Write(savedQuest with { Stages = [.. savedQuest.Stages, stage.Index] });
        EndObjectives(stage, savedQuest);
        foreach ((ushort objectiveIndex, string text) in displayedObjectives)
        {
            Display(objectiveIndex, text);
        }
        if (logEntry != null)
        {
            Description = logEntry;
        }
        if (stage.QuestOutcome is { } questOutcome)
        {
            Finish(endOpenObjectives: false, failed: questOutcome == QuestOutcome.Failed);
        }
        foreach (QuestScene scene in stage.ScenesToStart)
        {
            // A package record absent from the load order throws, and the stage is still set.
            try
            {
                QuestSceneStartResult result = scene.Start();
                if (result != QuestSceneStartResult.Started)
                {
                    log.Write($"Stage {stage.Index} of the quest \"{Name}\" did not start the scene \"{scene.Name}\": {result}.");
                }
            }
            catch (InvalidOperationException ex)
            {
                log.Write($"Stage {stage.Index} of the quest \"{Name}\" did not start the scene \"{scene.Name}\": {ex.Message}");
            }
        }
        if (stage.SetCallback != null)
        {
            await stage.SetCallback(new QuestStageContext(this, stage, session.NewGameOrSaveLoading));
        }
        return true;
    }

    // savedQuest is the quest's state before the stage, so an objective the stage displays for the first time is not among the open ones.
    private void EndObjectives(QuestStage stage, SavedQuest savedQuest)
    {
        HashSet<ushort> openIndexes = [.. savedQuest.Objectives.Where(objective => objective.IsOpen).Select(objective => objective.Index)];
        foreach (QuestObjective objective in stage.ObjectivesToComplete.Where(objective => openIndexes.Contains(GetIndex(objective))))
        {
            EndObjective(GetIndex(objective), failed: false);
        }
        foreach (QuestObjective objective in stage.ObjectivesToFail.Where(objective => openIndexes.Contains(GetIndex(objective))))
        {
            EndObjective(GetIndex(objective), failed: true);
        }
        if ((stage.OpenObjectivesOutcome ?? stage.QuestOutcome) is { } openOutcome)
        {
            HashSet<ushort> namedIndexes = [.. stage.ObjectivesToComplete.Concat(stage.ObjectivesToFail).Concat(stage.ObjectivesToDisplay).Select(GetIndex)];
            foreach (ushort openIndex in savedQuest.Objectives.Where(objective => objective.IsOpen && !namedIndexes.Contains(objective.Index)).Select(objective => objective.Index))
            {
                EndObjective(openIndex, failed: openOutcome == QuestOutcome.Failed);
            }
        }
    }

    public bool IsActive
    {
        get => store.Read() is { Objectives.Count: > 0, Completed: false, Failed: false } savedQuest && (Quest?.Data.Flags.HasFlag(QuestFlag.kActive) ?? savedQuest.Active);
        set
        {
            if (store.Read() is { Objectives.Count: > 0, Completed: false, Failed: false } savedQuest)
            {
                store.Write(savedQuest with { Active = value });
                record.Apply(true);
            }
        }
    }

    void IQuestObjectives.SetText(QuestObjective objective, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ushort index = GetIndex(objective);
        SavedQuest savedQuest = store.Read();
        Dictionary<ushort, string> texts = new(savedQuest.ObjectiveTexts)
        {
            [index] = text
        };
        string displayedText = ReplaceAliasTokens(text);
        Write(savedQuest with
        {
            ObjectiveTexts = texts,
            Objectives = [.. savedQuest.Objectives.Select(savedObjective => savedObjective.Index == index ? savedObjective with { Text = displayedText } : savedObjective)]
        });
    }

    void IQuestObjectives.SetTargetReferences<TReference>(QuestObjective objective, params IEnumerable<FormId<TReference>> referenceIds)
    {
        ArgumentNullException.ThrowIfNull(referenceIds);
        ushort index = GetIndex(objective);
        FormId<ObjectReference>[] references = [.. referenceIds.Select(referenceId => referenceId.Cast<ObjectReference>())];
        SavedQuest savedQuest = store.Read();
        Dictionary<ushort, IReadOnlyList<FormId<ObjectReference>>> targets = new(savedQuest.ObjectiveTargets);
        if (references.Length > 0)
        {
            targets[index] = references;
        }
        else
        {
            targets.Remove(index);
        }
        Write(savedQuest with { ObjectiveTargets = targets });
    }

    bool IQuestStages.IsDone(QuestStage stage) => store.Read().Stages.Contains(stage.Index);

    private ushort GetIndex(QuestObjective objective) => declaration.ObjectiveIndexes[objective];

    private string GetText(SavedQuest savedQuest, ushort index, QuestObjective objective) => savedQuest.ObjectiveTexts.GetValueOrDefault(index) ?? objective.GetText()
        ?? throw new InvalidOperationException($"Objective {index} of the quest \"{Name}\" has no text declared or set.");

    private void EndObjective(ushort index, bool failed)
    {
        SavedQuest savedQuest = store.Read();
        if (savedQuest.Objectives.FirstOrDefault(objective => objective.Index == index) is not { } displayedObjective)
        {
            throw new InvalidOperationException($"The quest \"{Name}\" has not displayed objective {index}.");
        }
        Write(savedQuest with
        {
            Objectives = [.. savedQuest.Objectives.Select(objective => objective.Index == index ? objective with { Completed = !failed, Failed = failed } : objective)]
        });
        if ((displayedObjective.Completed, displayedObjective.Failed) != (!failed, failed))
        {
            record.ShowBanner(failed ? HUD_MESSAGE_TYPE.kObjectiveFailed : HUD_MESSAGE_TYPE.kObjectiveComplete, displayedObjective.Text);
        }
    }

    private void Finish(bool endOpenObjectives, bool failed)
    {
        SavedQuest savedQuest = store.Read();
        if (savedQuest.Objectives.Count == 0)
        {
            throw new InvalidOperationException($"The quest \"{Name}\" has displayed no objective.");
        }
        IReadOnlyList<SavedObjective> objectivesToEnd = endOpenObjectives ? [.. savedQuest.Objectives.Where(objective => objective.IsOpen)] : [];
        Write(savedQuest with
        {
            Completed = !failed,
            Failed = failed,
            Objectives = [.. savedQuest.Objectives.Select(objective => endOpenObjectives && objective.IsOpen ? objective with { Completed = !failed, Failed = failed } : objective)]
        });
        foreach (SavedObjective objective in objectivesToEnd)
        {
            record.ShowBanner(failed ? HUD_MESSAGE_TYPE.kObjectiveFailed : HUD_MESSAGE_TYPE.kObjectiveComplete, objective.Text);
        }
        if ((savedQuest.Completed, savedQuest.Failed) != (!failed, failed))
        {
            record.ShowBanner(failed ? HUD_MESSAGE_TYPE.kQuestFailed : HUD_MESSAGE_TYPE.kQuestComplete, Name);
        }
    }

    private void Write(SavedQuest savedQuest)
    {
        store.Write(savedQuest);
        record.Apply(false);
    }

    private string ReplaceAliasTokens(string text) => AliasToken.Replace(text, token => GetAliasText(token.Groups[1].Value) is { Length: > 0 } name ? name : token.Value);

    private string? GetAliasText(string aliasName)
    {
        if (declaration.Aliases.FirstOrDefault(alias => alias.Name == aliasName) is { } alias)
        {
            return store.GetReference(alias)?.FormOrNull?.GetDisplayFullName();
        }
        return declaration.LocationAliases.FirstOrDefault(alias => alias.Name == aliasName) is { } locationAlias
            ? store.GetLocationId(locationAlias)?.FormOrNull?.AsTESFullName().GetFullName()
            : null;
    }
}
