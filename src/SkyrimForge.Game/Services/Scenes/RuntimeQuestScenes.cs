using SkyrimForge.Papyrus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

// Each scene plays in a quest record of its own, because a scene started in a quest that has run a scene with one of its actors can have a later phase end as it begins.
// The game runs a quest's scenes only after Quest.Start has listed the quest with the story teller, so Scene.Start is called once the quest's start finishes.
// A scene's records are made as it first starts after each load. A save restores the runtime forms it recorded under their own IDs,
// and the Papyrus VM does not bind Scene to a form made before the load that shares one of those IDs.
internal sealed class RuntimeQuestScenes : IQuestScenes, IScenePlayback
{
    private static readonly TimeSpan actionReadInterval = TimeSpan.FromSeconds(0.25);

    private readonly GameTarget gameTarget;
    private readonly IGameSession session;
    private readonly VoiceSilencer voiceSilencer;
    private readonly PluginVoices voices;
    private readonly ILog log;
    private readonly LinkedRefChanges linkedRefs;
    private readonly SceneLineTracker lineTracker;
    private readonly SceneReferences sceneReferences;
    private readonly ScenePlayerControl playerControl;
    private readonly SceneConversations conversations;
    private readonly SceneStarter starter;
    private readonly string questName;
    private readonly IReadOnlyList<QuestScene> declaredScenes;
    private readonly ISceneOwner owner;
    private readonly GameEvent<QuestSceneBeginEventArgs> onBegin;
    private readonly GameEvent<QuestScenePhaseEventArgs> onPhaseBegin;
    private readonly GameEvent<QuestScenePhaseEventArgs> onPhaseEnd;
    private readonly GameEvent<QuestSceneActionEndEventArgs> onActionEnd;
    private readonly GameEvent<QuestSceneEndEventArgs> onEnd;

    private readonly Dictionary<QuestScene, SceneHostQuest> hostQuests = [];

    private readonly Dictionary<(QuestScene Scene, int FirstPhaseIndex), SceneRecord> records = [];

    private readonly Dictionary<QuestScene, ScenePlay> plays = [];

    private TimeSpan now;

    internal RuntimeQuestScenes(GameTarget gameTarget, IGameEvents events, IUtility utility, IGameSession session, VoiceSilencer voiceSilencer, PluginVoices voices, ILog log, LinkedRefChanges linkedRefs, ISceneOwner owner, IReadOnlyList<QuestScene> declaredScenes)
    {
        this.gameTarget = gameTarget;
        this.session = session;
        this.voiceSilencer = voiceSilencer;
        this.voices = voices;
        this.log = log;
        this.linkedRefs = linkedRefs;
        this.owner = owner;
        questName = owner.Name;
        this.declaredScenes = declaredScenes;
        sceneReferences = new SceneReferences(owner);
        playerControl = new ScenePlayerControl(gameTarget, owner, sceneReferences, linkedRefs, log);
        conversations = new SceneConversations(owner, gameTarget);
        starter = new SceneStarter(utility, owner, sceneReferences, playerControl, linkedRefs, log, questName, () => now);
        lineTracker = new SceneLineTracker(gameTarget, events, utility, log, questName, sceneReferences.IsPlayer);
        onBegin = new GameEvent<QuestSceneBeginEventArgs>(ex => log.Write($"A subscriber to the scenes of the quest \"{questName}\" threw: {ex}"));
        onPhaseBegin = new GameEvent<QuestScenePhaseEventArgs>(ex => log.Write($"A subscriber to the scene phases of the quest \"{questName}\" threw: {ex}"));
        onPhaseEnd = new GameEvent<QuestScenePhaseEventArgs>(ex => log.Write($"A subscriber to the scene phases of the quest \"{questName}\" threw: {ex}"));
        onActionEnd = new GameEvent<QuestSceneActionEndEventArgs>(ex => log.Write($"A subscriber to the scene actions of the quest \"{questName}\" threw: {ex}"));
        onEnd = new GameEvent<QuestSceneEndEventArgs>(ex => log.Write($"A subscriber to the scenes of the quest \"{questName}\" threw: {ex}"));
        foreach (SceneConversation conversation in declaredScenes.SelectMany(scene => scene.Phases).SelectMany(phase => phase.Actions).OfType<SceneConversation>().Distinct())
        {
            conversation.Branch.OnConversationEnd.Subscribe(_ =>
            {
                foreach ((QuestScene scene, ScenePlay play) in plays)
                {
                    if (play.ConversationPhase is { } phase && SceneConversations.Get(scene, phase).Contains(conversation))
                    {
                        play.EndedConversations.Add((phase, conversation));
                    }
                }
            });
        }
        events.OnFrame.Subscribe(args =>
        {
            now = args.UnpausedTime;
            Update();
        });
        events.OnNewGameOrSaveLoading.Subscribe(_ =>
        {
            records.Clear();
            lineTracker.Clear();
            starter.ClearReleaseTimes();
            EndAll(QuestSceneEndReason.NewGameOrSaveLoading);
        });
    }

    public IGameEvent<QuestSceneBeginEventArgs> OnBegin => onBegin;

    public IGameEvent<QuestSceneActionEndEventArgs> OnActionEnd => onActionEnd;

    public IGameEvent<QuestSceneEndEventArgs> OnEnd => onEnd;

    public IGameEvent<QuestScenePhaseEventArgs> OnPhaseBegin => onPhaseBegin;

    public IGameEvent<QuestScenePhaseEventArgs> OnPhaseEnd => onPhaseEnd;

    public IGameEvent<QuestSceneLineEventArgs> OnLineBegin => lineTracker.OnBegin;

    public IGameEvent<QuestSceneLineEventArgs> OnLineEnd => lineTracker.OnEnd;

    IRuntimeQuest IScenePlayback.Quest => owner.Quest;

    public IQuestSceneEvents GetEvents(QuestScene scene) => new QuestSceneEvents(this, scene);

    internal IEnumerable<Quest> QuestRecords => hostQuests.Values.Select(hostQuest => hostQuest.Quest);

    internal static void ResolveVoices(PluginVoices voices, IEnumerable<QuestScene> scenes)
    {
        foreach (TopicResponse response in scenes.SelectMany(scene => scene.Phases).SelectMany(phase => phase.Actions).OfType<SceneLine>().SelectMany(line => line.Variants).SelectMany(variant => variant))
        {
            _ = voices.ToReplyResponse(response);
        }
    }

    internal void Build()
    {
        foreach (QuestScene scene in declaredScenes)
        {
            Quest quest = owner.CreateSceneQuestRecord($"{questName}: {scene.Name}");
            QuestReferenceAlias[] namedAliases = [.. scene.Phases.SelectMany(phase => phase.Actions).SelectMany(action => action.Aliases).Distinct()];
            hostQuests.Add(scene, new SceneHostQuest(quest, new QuestRecordAliases(gameTarget, quest, namedAliases), namedAliases));
        }
    }

    internal void StopQuests()
    {
        foreach (Quest quest in QuestRecords)
        {
            if (quest.IsEnabled() && !quest.IsStopping())
            {
                quest.Stop();
            }
        }
    }

    public QuestSceneStartResult Start(QuestScene scene, int firstPhaseIndex)
    {
        ThrowIfNoPhase(scene, firstPhaseIndex);
        return Start(scene, firstPhaseIndex, forced: false);
    }

    public QuestSceneStartResult ForceStart(QuestScene scene, int firstPhaseIndex)
    {
        ThrowIfNoPhase(scene, firstPhaseIndex);
        return Start(scene, firstPhaseIndex, forced: true);
    }

    public Task<QuestSceneEndReason?> PlayAsync(QuestScene scene, int firstPhaseIndex)
    {
        ThrowIfNoPhase(scene, firstPhaseIndex);
        return Core();

        async Task<QuestSceneEndReason?> Core()
        {
            if (Start(scene, firstPhaseIndex, forced: false) != QuestSceneStartResult.Started)
            {
                return null;
            }
            // A start raises no end before the next frame.
            return (await onEnd.WaitForNextAsync(args => args.Scene == scene)).Reason;
        }
    }

    private QuestSceneStartResult Start(QuestScene scene, int firstPhaseIndex, bool forced)
    {
        if (plays.ContainsKey(scene))
        {
            return QuestSceneStartResult.AlreadyPlaying;
        }
        if (scene.Condition is { } condition
            && !ConditionGlobal.Evaluate(() => condition(scene), ex => log.Write($"The condition of the scene \"{scene.Name}\" of the quest \"{questName}\" threw, and counts as false: {ex}")))
        {
            return QuestSceneStartResult.ConditionNotMet;
        }
        if (GetRecord(scene, firstPhaseIndex) is { } record)
        {
            StartRecord(scene, record, firstPhaseIndex, forced);
            return QuestSceneStartResult.Started;
        }
        return QuestSceneStartResult.GameDataNotLoaded;
    }

    public void Stop(QuestScene scene)
    {
        if (plays.TryGetValue(scene, out ScenePlay? play) && play.EndReason == null)
        {
            play.EndReason = QuestSceneEndReason.Stopped;
            // Dispatching to a record before it is bound ends the process.
            if (play.IsBound)
            {
                play.Scene.Papyrus.Stop().LogFault(log);
            }
        }
    }

    public void StopAll()
    {
        foreach (QuestScene scene in plays.Keys.ToArray())
        {
            Stop(scene);
        }
    }

    public bool IsPlaying(QuestScene scene) => plays.ContainsKey(scene);

    public bool IsAnyPlaying => plays.Count > 0;

    public int? GetCurrentPhaseIndex(QuestScene scene) => plays.TryGetValue(scene, out ScenePlay? play) ? play.AnnouncedPhase : null;

    private static void ThrowIfNoPhase(QuestScene scene, int firstPhaseIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(firstPhaseIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(firstPhaseIndex, scene.Phases.Count);
    }

    internal void ClearSaidLines() => lineTracker.ClearSaid();

    internal IReadOnlyList<string> GetSaidLines() => [.. owner.SavedSaidLines.Union(lineTracker.GetSaidKeys())];

    internal IReadOnlyDictionary<string, int> GetResumePhases() =>
        plays.Where(pair => pair.Value.EndReason == null).ToDictionary(pair => pair.Key.Name, pair => pair.Value.ReachedPhase);

    // A save written while controls were disabled, or while the player was AI-driven, loads in that state.
    internal void Resume(IReadOnlyDictionary<string, int> resumePhases, ScenePlayerControls disabledControls, bool playerDriven)
    {
        foreach ((string name, int phase) in resumePhases)
        {
            if (declaredScenes.FirstOrDefault(scene => scene.Name == name) is not { } scene || plays.ContainsKey(scene))
            {
                continue;
            }
            // A package record absent from the load order throws, and the other scenes still resume.
            try
            {
                if (phase < scene.Phases.Count && GetRecord(scene, phase) is { } phaseRecord)
                {
                    StartRecord(scene, phaseRecord, phase);
                }
            }
            catch (InvalidOperationException ex)
            {
                log.Write($"The scene \"{name}\" of the quest \"{questName}\" did not resume: {ex.Message}");
            }
            if (plays.TryGetValue(scene, out ScenePlay? play))
            {
                play.ControlsDisabled = true;
            }
        }
        playerControl.EnableControls(disabledControls);
        if (playerDriven)
        {
            playerControl.ReleasePlayer();
        }
    }

    private SceneRecord? GetRecord(QuestScene scene, int firstPhaseIndex)
    {
        if (!hostQuests.TryGetValue(scene, out SceneHostQuest? hostQuest))
        {
            return null;
        }
        if (!records.TryGetValue((scene, firstPhaseIndex), out SceneRecord? sceneRecord))
        {
            sceneRecord = new SceneRecordBuilder(gameTarget, voiceSilencer, voices, hostQuest.Quest, hostQuest.Aliases, scene, firstPhaseIndex, () => plays.GetValueOrDefault(scene), () => now).Build();
            records.Add((scene, firstPhaseIndex), sceneRecord);
            lineTracker.Add(scene, sceneRecord);
        }
        return sceneRecord;
    }

    // Each record has infos of its own, so each variant of an IsSaidOnce line said in another record or recorded in the saved state is marked said first.
    private void StartRecord(QuestScene scene, SceneRecord sceneRecord, int firstPhaseIndex, bool forced = false)
    {
        lineTracker.MarkSaid(GetSaidLines());
        if (sceneReferences.GetUnavailabilityReason(scene, firstPhaseIndex, loadedRequired: false) is { } unavailabilityReason)
        {
            log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" did not start: {unavailabilityReason}.");
            plays.Add(scene, new ScenePlay(sceneRecord, firstPhaseIndex) { EndReason = QuestSceneEndReason.ActorUnavailable });
            return;
        }
        // Two scenes started in one frame with a shared actor take the actor from each other each frame.
        HashSet<QuestReferenceAlias> actors = [.. scene.GetActors(firstPhaseIndex)];
        HashSet<uint> referenceIds = [.. actors.Select(alias => owner.GetReference(alias)?.Value).WhereNotNull()];
        Scene[] stoppedScenes = [.. owner.PluginScenes.SelectMany(scenes => scenes.StopScenesWith(referenceIds))];
        ScenePlay play = new(sceneRecord, firstPhaseIndex) { IsForced = forced };
        plays.Add(scene, play);
        play.StartTask = starter.StartAsync(scene, play, hostQuests[scene], actors, stoppedScenes, session.NewGameOrSaveLoading);
    }

    internal IReadOnlyList<Scene> StopScenesWith(IReadOnlySet<uint> referenceIds)
    {
        KeyValuePair<QuestScene, ScenePlay>[] playsSharingReferences = [.. plays.Where(other => other.Value.EndReason == null
            && other.Key.GetActors(other.Value.ReachedPhase).Any(alias => owner.GetReference(alias)?.Value is { } referenceId && referenceIds.Contains(referenceId)))];
        foreach (KeyValuePair<QuestScene, ScenePlay> other in playsSharingReferences)
        {
            Stop(other.Key);
        }
        return [.. playsSharingReferences.Select(other => other.Value.Scene)];
    }

    internal bool HasActor(FormId<Form> referenceId) =>
        plays.Any(pair => pair.Value.EndReason == null && pair.Key.GetActors(pair.Value.ReachedPhase).Any(alias => owner.GetReference(alias)?.Value == referenceId.Value));

    private void Update()
    {
        foreach ((QuestScene scene, ScenePlay play) in plays.ToArray())
        {
            if (play.IsBound)
            {
                play.SceneRecord.WriteConditions(log, scene.Name, questName);
            }
            if (play.Scene.IsPlaying)
            {
                Advance(scene, play);
            }
            else if (play.StartTask is not { IsCompleted: false })
            {
                Finish(scene, play);
            }
        }
    }

    private void Advance(QuestScene scene, ScenePlay play)
    {
        play.StartTask = null;
        if (!play.HasPlayed)
        {
            play.HasPlayed = true;
            if (scene.DisabledPlayerControls != ScenePlayerControls.None)
            {
                play.ControlsDisabled = true;
                playerControl.DisableControls(scene.DisabledPlayerControls);
            }
            playerControl.SetPlayerAIDriven(scene, play);
            SilenceRemarks(scene, play);
            onBegin.Raise(new QuestSceneBeginEventArgs(scene, play.FirstPhase));
        }
        // The index reads the record's phase count for a frame once the last phase finishes, and can read it from an earlier phase of a scene the game ends early.
        // The count is therefore accepted only from the last phase, or from a phase after which the game skips every phase.
        int reportedPhase = play.FirstPhase + (int)play.Scene.CurrentPhaseIndex;
        if (reportedPhase < scene.Phases.Count || Enumerable.Range(0, scene.Phases.Count).Skip(play.ReachedPhase + 1).All(phase => IsSkipped(play, phase)))
        {
            play.ReachedPhase = Math.Max(play.ReachedPhase, reportedPhase);
        }
        // The game can pass a phase within a frame.
        for (int phase = (play.LastPassedPhase ?? play.FirstPhase - 1) + 1; phase <= Math.Min(play.ReachedPhase, scene.Phases.Count - 1); phase++)
        {
            play.LastPassedPhase = phase;
            if (!IsSkipped(play, phase))
            {
                AnnouncePhase(scene, play, phase);
            }
        }
        conversations.Show(scene, play, play.AnnouncedPhase);
        conversations.ReplaceOpenedPackages(scene, play);
        if (now >= play.NextActionReadTime && play.RunningActions.Count > 0)
        {
            play.NextActionReadTime = now + actionReadInterval;
            ReadActions(scene, play);
        }
        if (play.EndReason == null && sceneReferences.GetUnavailabilityReason(scene, play.ReachedPhase) is { } unavailabilityReason)
        {
            log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" stopped: {unavailabilityReason}.");
            play.EndReason = QuestSceneEndReason.ActorUnavailable;
            play.Scene.Papyrus.Stop().LogFault(log);
        }
    }

    private void AnnouncePhase(QuestScene scene, ScenePlay play, int phase)
    {
        if (play.AnnouncedPhase is { } endedPhase)
        {
            // An action can end in a phase the game skipped.
            EndActions(scene, play, action => action.EndPhase < phase);
            onPhaseEnd.Raise(new QuestScenePhaseEventArgs(scene, endedPhase));
        }
        play.AnnouncedPhase = phase;
        play.PhaseBeginTime = now;
        if (scene.Phases[phase].StageSetWhenBegun is { } stage)
        {
            SetStage(scene, stage);
        }
        onPhaseBegin.Raise(new QuestScenePhaseEventArgs(scene, phase));
        SceneRecordAction[] begunActions = [.. play.SceneRecord.Actions.Where(action => action.StartPhase == phase)];
        play.RunningActions.UnionWith(begunActions);
        WriteComputedLines(scene, play.SceneRecord, begunActions);
        foreach (ScenePackage travel in begunActions.Select(action => action.Action).OfType<ScenePackage>().Where(action => action.Travels && sceneReferences.IsPlayer(action.Actor)))
        {
            playerControl.SetPlayerLink(play, travel);
        }
    }

    private void Finish(QuestScene scene, ScenePlay play)
    {
        if (play.StartTask is { Exception: { } exception })
        {
            log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" did not start: {exception.InnerException?.Message ?? exception.Message}");
        }
        else if (!play.HasPlayed && play.EndReason == null)
        {
            log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" was started and did not play.");
        }
        plays.Remove(scene);
        Release(scene, play);
        QuestSceneEndReason reason = play.EndReason ?? GetGameEndReason(scene, play);
        if (reason == QuestSceneEndReason.Completed && play.AnnouncedPhase is { } lastPhase)
        {
            EndActions(scene, play, _ => true);
            onPhaseEnd.Raise(new QuestScenePhaseEventArgs(scene, lastPhase));
        }
        if (reason == QuestSceneEndReason.Completed && scene.StageSetWhenCompleted is { } stage)
        {
            SetStage(scene, stage);
        }
        // The game reports no reason for ending a scene.
        QuestReferenceAlias? actorInCombat = play.EndReason == null && reason == QuestSceneEndReason.Interrupted ? sceneReferences.GetActorInCombat(scene, play) : null;
        if (actorInCombat != null)
        {
            log.Write($"The actor of the alias \"{actorInCombat.Name}\" was in combat when the game ended the scene \"{scene.Name}\" of the quest \"{questName}\". "
                + "That combat may have ended the scene.");
        }
        onEnd.Raise(new QuestSceneEndEventArgs(scene, reason, actorInCombat));
        if (reason == QuestSceneEndReason.Completed && scene.Repeats)
        {
            // Start throws for a package record absent from the load order, and the frame event drops a handler that throws.
            try
            {
                Start(scene, 0, forced: false);
            }
            catch (InvalidOperationException ex)
            {
                log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" did not repeat: {ex.Message}");
            }
        }
    }

    private void Release(QuestScene scene, ScenePlay play)
    {
        lineTracker.EndScene(scene);
        if (play.ControlsDisabled)
        {
            playerControl.EnableControls(scene.DisabledPlayerControls);
        }
        if (play.PlayerDriven)
        {
            playerControl.ReleasePlayer();
        }
        foreach (FormId<Actor> actorId in play.SilencedRemarkers)
        {
            voiceSilencer.RestoreRemarks(actorId, play);
        }
        conversations.Show(scene, play, null);
        linkedRefs.Release(play);
        // Only a start that bound the record forced the actors into the scene's quest.
        if (play.IsBound)
        {
            starter.RecordRelease(scene.GetActors(play.FirstPhase));
        }
        Quest quest = hostQuests[scene].Quest;
        if (quest.IsEnabled() && !quest.IsStopping())
        {
            quest.Stop();
        }
    }

    internal ScenePlayerControls DisabledControls =>
        plays.Where(pair => pair.Value.ControlsDisabled).Aggregate(ScenePlayerControls.None, (controls, pair) => controls | pair.Key.DisabledPlayerControls);

    internal bool DrivesPlayer => plays.Values.Any(play => play.PlayerDriven);

    // The game builds a line from its info's responses in the frame after its phase is read as begun, so a line says the responses written here.
    private void WriteComputedLines(QuestScene scene, SceneRecord sceneRecord, IReadOnlyCollection<SceneRecordAction> begunActions)
    {
        foreach ((TopicInfo info, SceneLine line, _) in sceneRecord.Lines)
        {
            if (line.ComputeResponses != null && begunActions.Any(action => ReferenceEquals(action.Action, line))
                && owner.GetReference(line.Speaker)?.FormOrNull?.AsActor() is { } speaker)
            {
                TopicResponse[] responses = [];
                ReplyResponse[] replies = [];
                try
                {
                    TopicResponse[] computedResponses = [.. line.ComputeResponses!(speaker)];
                    replies = [.. computedResponses.Select(voices.ToReplyResponse)];
                    responses = computedResponses;
                }
                catch (Exception ex)
                {
                    log.Write($"The responses of a line the alias \"{line.Speaker.Name}\" says in the scene \"{scene.Name}\" of the quest \"{questName}\" were not computed, "
                        + $"and the line says nothing: {ex}");
                }
                voiceSilencer.UpdateResponses(info, replies, true);
                lineTracker.SetComputedResponses(info, responses);
            }
        }
    }

    private void ReadActions(QuestScene scene, ScenePlay play)
    {
        foreach (SceneRecordAction action in play.RunningActions.ToArray())
        {
            bool complete = action.Action is SceneConversation conversation ? play.EndedConversations.Contains((action.Phase, conversation)) : play.Scene.IsActionComplete((int)action.Id);
            if (complete)
            {
                EndActions(scene, play, readAction => readAction == action);
            }
        }
    }

    private void EndActions(QuestScene scene, ScenePlay play, Func<SceneRecordAction, bool> test)
    {
        foreach (SceneRecordAction action in play.RunningActions.Where(test).ToArray())
        {
            play.RunningActions.Remove(action);
            onActionEnd.Raise(new QuestSceneActionEndEventArgs(scene, action.Phase, action.Action));
        }
    }

    private static bool IsSkipped(ScenePlay play, int phase) => play.SceneRecord.PhaseStarts.TryGetValue(phase, out ConditionGlobal? startCondition) && !startCondition.Result;

    // The game ends a scene as another with one of its actors starts. A one-phase scene ended that way reads the phase count as its phase index,
    // so the other scene is tested before completion.
    private QuestSceneEndReason GetGameEndReason(QuestScene scene, ScenePlay play)
    {
        if (scene.GetActors(play.FirstPhase).Any(alias => owner.GetReference(alias)?.FormOrNull?.GetCurrentScene() is { IsPlaying: true } current
            && current.Address != play.Scene.Address && !play.ScenesAtStart.Contains(current.Address)))
        {
            return QuestSceneEndReason.ActorInAnotherScene;
        }
        if (play.ReachedPhase == scene.Phases.Count)
        {
            return QuestSceneEndReason.Completed;
        }
        return sceneReferences.GetUnavailabilityReason(scene, play.ReachedPhase) != null ? QuestSceneEndReason.ActorUnavailable : QuestSceneEndReason.Interrupted;
    }

    private void EndAll(QuestSceneEndReason reason)
    {
        QuestScene[] endedScenes = [.. plays.Keys];
        plays.Clear();
        foreach (QuestScene scene in endedScenes)
        {
            onEnd.Raise(new QuestSceneEndEventArgs(scene, reason));
        }
    }

    // A load clears every silence, and a scene playing at the save begins again after the load, silencing its actors again.
    private void SilenceRemarks(QuestScene scene, ScenePlay play)
    {
        foreach (SceneActorSettings settings in scene.ActorSettings.Where(settings => settings.SilencesRemarks))
        {
            if (owner.GetReference(settings.Actor)?.FormOrNull?.AsActor() is { } actor)
            {
                voiceSilencer.SilenceRemarks(actor.FormId, play);
                play.SilencedRemarkers.Add(actor.FormId);
            }
        }
    }

    private void SetStage(QuestScene scene, QuestStage stage) =>
        stage.SetAsync().OnFault(exception => log.Write($"Setting stage {stage.Index} from the scene \"{scene.Name}\" of the quest \"{questName}\" threw: {exception}"));
}
