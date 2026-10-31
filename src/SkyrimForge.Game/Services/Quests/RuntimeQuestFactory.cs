using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class RuntimeQuestFactory : IRuntimeQuestFactory
{
    private readonly GameTarget gameTarget;
    private readonly IGameEvents events;
    private readonly IGameSession session;
    private readonly IUtility utility;
    private readonly IPerPluginSavedState savedState;
    private readonly VoiceSilencer voiceSilencer;
    private readonly PluginVoices voices;
    private readonly ILog log;
    private readonly LinkedRefChanges linkedRefs;
    private readonly Dictionary<string, RuntimeQuest> quests = new(StringComparer.Ordinal);
    private bool dataLoaded;
    private bool gameLoaded;

    private ISubscription? packageRunSubscription;

    public RuntimeQuestFactory(GameTarget gameTarget, IGameEvents events, IGameSession session, IUtility utility, IFrameworkFunctionHooks functionHooks, IPerPluginSavedState savedState, VoiceSilencer voiceSilencer, PluginVoices voices, ILog log)
    {
        this.gameTarget = gameTarget;
        this.events = events;
        this.session = session;
        this.utility = utility;
        this.savedState = savedState;
        this.voiceSilencer = voiceSilencer;
        this.voices = voices;
        this.log = log;
        // Constructed first, so its load handler restores the links the save recorded before ApplyAll resumes a scene that points one again.
        linkedRefs = new LinkedRefChanges(events, savedState);
        events.OnNewGameOrSaveLoading.Subscribe(_ =>
        {
            foreach (RuntimeQuest quest in quests.Values)
            {
                quest.Aliases.Packages.ClearAppliedPackages();
            }
        });
        events.OnDataLoaded.Subscribe(_ =>
        {
            dataLoaded = true;
            QuestDescriptions.Hook(functionHooks, log);
            foreach (RuntimeQuest quest in quests.Values)
            {
                quest.Build();
            }
        });
        events.OnSaveLoaded.Subscribe(_ => ApplyAll());
        events.OnSaveWriting.Subscribe(_ =>
        {
            foreach (RuntimeQuest quest in quests.Values)
            {
                quest.WriteSaveTimeState();
            }
            ExcludeFromSave();
        });
        events.OnNewGame.Subscribe(_ => ApplyAll());
    }

    internal IEnumerable<RuntimeQuestScenes> Scenes => quests.Values.Select(quest => quest.Scenes);

    public IRuntimeQuest Create(string key, string name, IEnumerable<QuestStage>? stages = null, QUEST_DATA.Type type = QUEST_DATA.Type.kSideQuest, IEnumerable<QuestScene>? scenes = null, IEnumerable<QuestAlias>? aliases = null,
        bool activeOnStart = false)
    {
        const string keyPrefix = "SkyrimForge.RuntimeQuest.";
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(name);
        if (quests.ContainsKey(key))
        {
            throw new ArgumentException($"This plugin already created a quest with the key \"{key}\".", nameof(key));
        }
        var declaration = new QuestDeclaration(stages ?? [], scenes, aliases);
        QuestAlias[] declaredAliases = [.. declaration.Aliases.Where(alias => alias != QuestReferenceAlias.Player), .. declaration.LocationAliases];
        if (declaredAliases.FirstOrDefault(alias => alias.QuestAliasesOrNull != null) is { } takenAlias)
        {
            throw new ArgumentException($"The alias \"{takenAlias.Name}\" already belongs to the quest \"{takenAlias.Quest.Name}\". An alias belongs to one quest only.", nameof(aliases));
        }
        if (declaration.Stages.Values.FirstOrDefault(stage => stage.OwnerOrNull != null) is { } takenStage)
        {
            throw new ArgumentException($"Stage {takenStage.Index} already belongs to the quest \"{takenStage.Quest.Name}\". A stage belongs to one quest only.", nameof(stages));
        }
        if (declaration.ObjectiveIndexes.Keys.FirstOrDefault(objective => objective.OwnerOrNull != null) is { } takenObjective)
        {
            string description = takenObjective.Index is { } objectiveIndex ? $"Objective {objectiveIndex}" : "An objective created with no index";
            throw new ArgumentException($"{description} already belongs to the quest \"{takenObjective.Quest.Name}\". An objective belongs to one quest only.", nameof(stages));
        }
        if (declaration.Scenes.FirstOrDefault(scene => scene.OwnerOrNull != null) is { } takenScene)
        {
            throw new ArgumentException($"The scene \"{takenScene.Name}\" already belongs to the quest \"{takenScene.Quest.Name}\". A scene belongs to one quest only.", nameof(scenes));
        }
        // Checked before the quest is built, because its scenes subscribe to game events, and nothing ends those subscriptions for a quest whose creation throws.
        RuntimeQuestScenes.ResolveVoices(voices, declaration.Scenes);
        var store = new SavedQuestStore(savedState, savedState.GetRecordPair<SavedQuest>(keyPrefix + key));
        var record = new RuntimeQuestRecord(gameTarget, utility, session, log, store, declaration, name, type);
        var packages = new AliasPackages(name, store.GetReference, actor => IsSpeaking(actor) || IsInScene(actor), record.SetPackage, linkedRefs, session, log);
        var runtimeQuest = new RuntimeQuest(this, name, declaration, activeOnStart, store, record, session, log,
            quest => new RuntimeQuestAliases(quest, this, gameTarget, events, utility, session, store, record, packages, new AliasActorState(savedState, store.Key), declaration),
            quest => new RuntimeQuestScenes(gameTarget, events, utility, session, voiceSilencer, voices, log, linkedRefs, quest, declaration.Scenes));
        quests.Add(key, runtimeQuest);
        foreach (QuestAlias alias in declaredAliases)
        {
            alias.QuestAliasesOrNull = runtimeQuest.Aliases;
        }
        foreach (QuestScene scene in declaration.Scenes)
        {
            scene.OwnerOrNull = runtimeQuest.Scenes;
        }
        foreach (QuestStage stage in declaration.Stages.Values)
        {
            stage.OwnerOrNull = runtimeQuest;
        }
        foreach (QuestObjective objective in declaration.ObjectiveIndexes.Keys)
        {
            objective.OwnerOrNull = runtimeQuest;
        }
        if (dataLoaded)
        {
            runtimeQuest.Build();
            if (gameLoaded)
            {
                runtimeQuest.ApplyToGame();
            }
        }
        return runtimeQuest;
    }

    internal void StartAliasPackageRuns()
    {
        packageRunSubscription ??= events.OnFrame.SubscribeEvery(TimeSpan.FromSeconds(0.25), _ =>
        {
            foreach (RuntimeQuest quest in quests.Values)
            {
                quest.Aliases.Packages.Run();
            }
        });
    }

    // The game's current scene of an actor does not decide this, since a data file's scene can stay in phase 0 while the actor runs its own packages.
    private bool IsInScene(Actor actor) => quests.Values.Any(quest => quest.Scenes.HasActor(actor.FormId));

    // UnvoicedSpeech gives a speaker outside a conversation the wait-for-dialogue package.
    private bool IsSpeaking(Actor actor) =>
        gameTarget.MenuTopicManager.IsConversationOpenWith(actor.FormId) || actor.GetCurrentPackage()?.FormId == KnownForms.zzzDEFAULTzzzWaitForDialogue;

    private void ApplyAll()
    {
        gameLoaded = true;
        foreach (RuntimeQuest quest in quests.Values)
        {
            quest.ApplyAfterLoad();
        }
    }

    // A created form is written into every save unless it states kTemporary.
    // The flag is cleared on the next frame, since the journal shows no objectives of a quest that states it.
    private void ExcludeFromSave()
    {
        List<Quest> records = [.. quests.Values.Select(quest => quest.Quest).WhereNotNull(), .. quests.Values.SelectMany(quest => quest.Scenes.QuestRecords)];
        foreach (Quest record in records)
        {
            RuntimeForms.SetTemporary(record, true);
        }
        // Run as the wait completes, inside the next frame.
        utility.WaitAsync(TimeSpan.Zero).ContinueWith(_ =>
        {
            foreach (Quest record in records)
            {
                RuntimeForms.SetTemporary(record, false);
            }
        }, TaskContinuationOptions.ExecuteSynchronously).LogFault(log);
    }
}
