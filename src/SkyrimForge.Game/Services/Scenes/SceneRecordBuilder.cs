using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class SceneRecordBuilder(GameTarget gameTarget, VoiceSilencer voiceSilencer, PluginVoices voices, Quest questRecord, QuestRecordAliases recordAliases, QuestScene scene, int firstPhase, Func<ScenePlay?> getPlay, Func<TimeSpan> getNow)
{
    // The size CommonLib asserts for BGSSceneActionPackage.
    private const int packageActionSize = 0x40;

    // Skyrim.esm's "Take a look." vendor line. A line's info shares its responses, which VoiceSilencer replaces with the line's own.
    private static readonly FormId<TopicInfo> sharedResponsesInfoId = new(0x0007F6BC);

    private static readonly TimeSpan conversationTimer = TimeSpan.FromDays(1);

    private readonly GameTarget gameTarget = gameTarget;
    private readonly VoiceSilencer voiceSilencer = voiceSilencer;
    private readonly PluginVoices voices = voices;
    private readonly Quest questRecord = questRecord;
    private readonly QuestRecordAliases recordAliases = recordAliases;
    private readonly QuestScene scene = scene;
    private readonly int firstPhase = firstPhase;
    private readonly Func<ScenePlay?> getPlay = getPlay;
    private readonly Func<TimeSpan> getNow = getNow;

    private readonly List<SceneRecordTravel> travels = [];
    private readonly List<ConditionGlobal> conditions = [];
    private readonly Dictionary<int, ConditionGlobal> phaseStarts = [];
    private readonly List<SceneRecordLine> lines = [];
    private readonly List<SceneRecordSayOnceInfo> sayOnceInfos = [];

    internal SceneRecord Build()
    {
        Scene gameScene = RuntimeForms.Create(FormType.Scene).AsScene()!.Value;
        RuntimeForms.SetTemporary(gameScene, true);
        gameScene.ParentQuest = questRecord;
        gameScene.Flags = Scene.Flag.kStopOnQuestEnd | (scene.IsInterruptible ? Scene.Flag.kInterruptible : Scene.Flag.kNone);
        // A record of phases whose actions are all timers lists the scene's first actor, which the timers specify.
        QuestReferenceAlias[] actors = [.. scene.GetActors(firstPhase).DefaultIfEmpty(scene.GetActors(0).First()).Distinct()];
        foreach (QuestReferenceAlias actor in actors)
        {
            SceneActorSettings settings = scene.ActorSettings.FirstOrDefault(listedSettings => listedSettings.Actor == actor) ?? new SceneActorSettings(actor);
            gameScene.Actors.Add(recordAliases.GetId(actor));
            gameScene.ActorFlags.Add(GetActorFlags(settings));
            gameScene.ActorProgressionFlags.Add(GetBehaviourFlags(settings));
        }
        // A timer has no actor of its own, and the game's own timers each specify an actor of their scene.
        uint timerActorId = recordAliases.GetId(actors[0]);
        List<SceneRecordAction> actions = [];
        (QuestReferenceAlias Actor, int Start, int End)[] stayRuns = scene.ActorsStayInPlace ? [.. GetStayRuns(scene, firstPhase)] : [];
        foreach (int phase in Enumerable.Range(firstPhase, scene.Phases.Count - firstPhase))
        {
            gameScene.Phases.Add(CreatePhase(phase, stayRuns.Any(stayRun => stayRun.End == phase)));
        }
        uint index = 0;
        List<SceneRecordConversation> conversationPackages = [];
        foreach ((SceneAction action, int start, int end) in scene.GetActions(firstPhase))
        {
            PhaseRange phaseRange = new((ushort)(Math.Max(start, firstPhase) - firstPhase), (ushort)(end - firstPhase));
            // A conversation is the force-greet package and a timer that outlasts it, so its phase ends by the completion condition alone.
            SceneAction[] expandedActions = action is SceneConversation conversation
                ? [new ScenePackage(conversation.Actor, ForceGreet.PackageId), new SceneTimer(conversationTimer)] : [action];
            actions.Add(new SceneRecordAction(action, start, Math.Max(start, firstPhase), end, index + 1));
            foreach (SceneAction expandedAction in expandedActions)
            {
                index++;
                BGSSceneAction record = expandedAction switch
                {
                    SceneLine line => CreateDialogueAction(line, phaseRange, index),
                    SceneTimer timer => CreateTimerAction(timer, timerActorId, phaseRange, index),
                    ScenePackage package => CreatePackageAction(package, phaseRange, index),
                    _ => throw new InvalidOperationException($"The scene action {expandedAction.GetType().Name} has no record.")
                };
                gameScene.Actions.Add(record);
                if (expandedAction is ScenePackage && action is SceneConversation packageOwner)
                {
                    conversationPackages.Add(new SceneRecordConversation(packageOwner, record.AsBGSSceneActionPackage()!.Value.Packages));
                }
            }
        }
        foreach ((QuestReferenceAlias actor, int start, int end) in stayRuns)
        {
            index++;
            BGSSceneAction stayAction = CreateAction("BGSSceneActionPackage", packageActionSize, recordAliases.GetId(actor), new((ushort)(start - firstPhase), (ushort)(end - firstPhase)), index);
            stayAction.AsBGSSceneActionPackage()!.Value.Packages.Add(KnownForms.zzzDEFAULTzzzWaitForDialogue.Form);
            gameScene.Actions.Add(stayAction);
        }
        questRecord.Scenes.Add(gameScene);
        return new SceneRecord(gameScene, travels, conditions, phaseStarts, actions, lines, sayOnceInfos, conversationPackages);
    }

    private BGSScenePhase CreatePhase(int phase, bool endsStayRun)
    {
        ScenePhase declaredPhase = scene.Phases[phase];
        BGSScenePhase phaseRecord = BGSScenePhase.New();
        if (declaredPhase.Condition is { } condition)
        {
            ConditionGlobal start = CreateCondition(() => condition(scene));
            phaseStarts.Add(phase, start);
            TESCondition startConditions = phaseRecord.StartConditions;
            startConditions.Head = start.CreateConditionItem();
        }
        if (GetPhaseEnd(scene, phase, endsStayRun, getNow) is { } isComplete)
        {
            TESCondition completionConditions = phaseRecord.CompletionConditions;
            completionConditions.Head = CreateCondition(() => getPlay() is { } play && isComplete(play)).CreateConditionItem();
        }
        return phaseRecord;
    }

    internal static Func<ScenePlay, bool>? GetPhaseEnd(QuestScene scene, int phase, bool endsStayRun, Func<TimeSpan> getNow)
    {
        ScenePhase declaredPhase = scene.Phases[phase];
        List<Func<ScenePlay, bool>> endConditions = [];
        // The package and timer of a conversation never complete its phase, so the phase is complete once each of its conversations has ended.
        SceneConversation[] conversations = [.. declaredPhase.Actions.OfType<SceneConversation>()];
        if (conversations.Length > 0)
        {
            endConditions.Add(play => conversations.All(conversation => play.EndedConversations.Contains((phase, conversation))));
        }
        // The game ends a phase that has a completion condition only once the condition is true, whether or not its actions have ended.
        // A phase in which a stay run ends, whose package never completes, is therefore complete once each action of the scene that ends in it has ended,
        // and so is a phase with a time limit and an action that ends in it.
        if (declaredPhase.EndsWhen == null && (endsStayRun || (declaredPhase.TimeLimit != null && scene.GetActions(0).Any(action => action.End == phase))))
        {
            endConditions.Add(play => play.AnnouncedPhase == phase && !play.RunningActions.Any(action => action.EndPhase == phase));
        }
        if (declaredPhase.TimeLimit is { } limit)
        {
            endConditions.Add(play => play.AnnouncedPhase == phase && getNow() - play.PhaseBeginTime >= limit);
        }
        // The plugin's condition is last, so one that throws does not prevent the others from ending the phase.
        if (declaredPhase.EndsWhen is { } declaredEnd)
        {
            endConditions.Add(_ => declaredEnd(scene));
        }
        return endConditions.Count > 0 ? play => endConditions.Any(end => end(play)) : null;
    }

    internal static IEnumerable<(QuestReferenceAlias Actor, int Start, int End)> GetStayRuns(QuestScene scene, int firstPhase)
    {
        (SceneAction Action, int Start, int End)[] actions = [.. scene.GetActions(firstPhase)];
        foreach (QuestReferenceAlias actor in actions.Select(action => action.Action.Performer).WhereNotNull().Distinct())
        {
            int? start = null;
            foreach (int phase in Enumerable.Range(firstPhase, scene.Phases.Count - firstPhase))
            {
                bool free = !actions.Any(action => action.Action is ScenePackage or SceneConversation && action.Action.Performer == actor && action.Start <= phase && action.End >= phase);
                if (free)
                {
                    start ??= phase;
                }
                if (start is { } startPhase && (!free || phase == scene.Phases.Count - 1))
                {
                    yield return (actor, startPhase, free ? phase : phase - 1);
                    start = null;
                }
            }
        }
    }

    private static SCENE_ACTOR_FLAG GetActorFlags(SceneActorSettings settings) =>
        (settings.BlocksPlayerActivation ? SCENE_ACTOR_FLAG.kNoPlayerActivation : SCENE_ACTOR_FLAG.kNone)
        | (settings.IsOptional ? SCENE_ACTOR_FLAG.kOptional : SCENE_ACTOR_FLAG.kNone)
        | (settings.RunsOnlyScenePackages ? SCENE_ACTOR_FLAG.kRunOnlyScenePackages : SCENE_ACTOR_FLAG.kNone);

    private static Scene.BehaviourFlag GetBehaviourFlags(SceneActorSettings settings) =>
        (settings.EndsOnDeath ? Scene.BehaviourFlag.kDeathEnd : Scene.BehaviourFlag.kNone)
        | GetBehaviourFlag(settings.CombatInterruption, Scene.BehaviourFlag.kCombatPause, Scene.BehaviourFlag.kCombatEnd)
        | GetBehaviourFlag(settings.DialogueInterruption, Scene.BehaviourFlag.kDialoguePause, Scene.BehaviourFlag.kDialogueEnd)
        | GetBehaviourFlag(settings.ObservedCombatInterruption, Scene.BehaviourFlag.kOBS_COMPause, Scene.BehaviourFlag.kOBS_COMEnd);

    private static Scene.BehaviourFlag GetBehaviourFlag(SceneInterruption interruption, Scene.BehaviourFlag pause, Scene.BehaviourFlag end) => interruption switch
    {
        SceneInterruption.Pause => pause,
        SceneInterruption.End => end,
        _ => Scene.BehaviourFlag.kNone
    };

    private ConditionGlobal CreateCondition(Func<bool> condition)
    {
        ConditionGlobal conditionGlobal = new(condition);
        conditions.Add(conditionGlobal);
        return conditionGlobal;
    }

    private BGSSceneAction CreateAction(string type, int size, uint actorId, PhaseRange phaseRange, uint index)
    {
        BGSSceneAction action = BGSSceneAction.OrNull(GameHeap.AllocateObject(gameTarget, size, type))!.Value;
        action.ActorID = actorId;
        action.StartPhase = phaseRange.Start;
        action.EndPhase = phaseRange.End;
        action.Index = index;
        return action;
    }

    private BGSSceneAction CreateTimerAction(SceneTimer timer, uint actorId, PhaseRange phaseRange, uint index)
    {
        // The size CommonLib asserts for BGSSceneActionTimer.
        const int timerActionSize = 0x30;
        BGSSceneAction action = CreateAction("BGSSceneActionTimer", timerActionSize, actorId, phaseRange, index);
        BGSSceneActionTimer timerAction = action.AsBGSSceneActionTimer()!.Value;
        timerAction.TimerSeconds = (float)timer.Duration.TotalSeconds;
        return action;
    }

    // Each start of the scene fills a travel action's package list and sets the travel package's location.
    private BGSSceneAction CreatePackageAction(ScenePackage package, PhaseRange phaseRange, uint index)
    {
        BGSSceneAction action = CreateAction("BGSSceneActionPackage", packageActionSize, recordAliases.GetId(package.Actor), phaseRange, index);
        GameList<Package> packages = action.AsBGSSceneActionPackage()!.Value.Packages;
        if (package.Travels)
        {
            Package travel = TravelPackages.Create(package.Speed) ?? throw new InvalidOperationException("The game created no travel package with a location.");
            RuntimeForms.SetTemporary(travel, true);
            travels.Add(new SceneRecordTravel(travel, package, packages));
        }
        else
        {
            _ = package.LinkedPackage?.KeywordId?.Form;
            foreach (Package listedPackage in package.PackageIds.Select(packageId => packageId.Form))
            {
                packages.Add(listedPackage);
            }
        }
        return action;
    }

    private BGSSceneAction CreateDialogueAction(SceneLine line, PhaseRange phaseRange, uint index)
    {
        // The size CommonLib asserts for BGSSceneActionDialogue.
        const int dialogueActionSize = 0x40;
        ConditionGlobal? lineCondition = line.Condition is { } condition ? CreateCondition(() => condition(scene)) : null;
        Topic topic = RuntimeForms.Create(FormType.Dialogue).AsTopic()!.Value;
        RuntimeForms.SetTemporary(topic, true);
        DIALOGUE_DATA data = topic.Data;
        data.Type = DIALOGUE_TYPES.DIALOGUE_TYPE.kSceneDialogue;
        data.SetSubtype(DIALOGUE_DATA.Subtype.kScene);
        topic.OwnerQuest = questRecord;
        List<TopicInfo> infos = [];
        foreach (var (i, variant) in line.Variants.Index())
        {
            TopicInfo info = RuntimeForms.Create(FormType.Info).AsTopicInfo()!.Value;
            RuntimeForms.SetTemporary(info, true);
            info.ParentTopic = topic;
            info.DataInfo = sharedResponsesInfoId.Form;
            info.InfoIndex = (ushort)i;
            TOPIC_INFO_DATA.TOPIC_INFO_FLAGS infoFlags = TOPIC_INFO_DATA.TOPIC_INFO_FLAGS.kNone;
            if (line.Variants.Count > 1)
            {
                infoFlags |= TOPIC_INFO_DATA.TOPIC_INFO_FLAGS.kRandom | (i == line.Variants.Count - 1 ? TOPIC_INFO_DATA.TOPIC_INFO_FLAGS.kRandomEnd : TOPIC_INFO_DATA.TOPIC_INFO_FLAGS.kNone);
            }
            if (line.IsSaidOnce)
            {
                infoFlags |= TOPIC_INFO_DATA.TOPIC_INFO_FLAGS.kSayOnce;
                sayOnceInfos.Add(new SceneRecordSayOnceInfo(info, GetSayOnceKey(scene, variant)));
            }
            TOPIC_INFO_DATA infoData = info.Data;
            infoData.Flags = infoFlags;
            if (lineCondition != null)
            {
                TESCondition infoConditions = info.ObjConditions;
                infoConditions.Head = lineCondition.CreateConditionItem();
            }
            voiceSilencer.UpdateResponses(info, [.. variant.Select(voices.ToReplyResponse)], line.ComputeResponses != null);
            lines.Add(new SceneRecordLine(info, line, i));
            infos.Add(info);
        }
        topic.TopicInfos.Replace(infos);
        BGSSceneAction action = CreateAction("BGSSceneActionDialogue", dialogueActionSize, recordAliases.GetId(line.Speaker), phaseRange, index);
        BGSSceneActionDialogue dialogue = action.AsBGSSceneActionDialogue()!.Value;
        dialogue.Topic = topic;
        dialogue.HeadtrackActorID = line.LookTarget is { } target ? (int)recordAliases.GetId(target) : -1;
        BGSSceneAction.Flag flags = (line.LooksAtPlayer ? BGSSceneAction.Flag.kHeadTrackPlayer : BGSSceneAction.Flag.kNone)
            | (line.FacesTarget ? BGSSceneAction.Flag.kFaceTarget : BGSSceneAction.Flag.kNone);
        if (line.Loop != null)
        {
            flags |= BGSSceneAction.Flag.kLooping;
            dialogue.LoopingMin = (float)line.Loop.MinimumDelay.TotalSeconds;
            dialogue.LoopingMax = (float)line.Loop.MaximumDelay.TotalSeconds;
        }
        action.Flags = flags;
        return action;
    }

    private static string GetSayOnceKey(QuestScene scene, IReadOnlyList<TopicResponse> variant) =>
        $"{scene.Name}: {string.Join(' ', variant.Select(response =>
            response.SilenceDuration is { } duration ? $"silence {response.SpeakerIdleId?.Value:X8} {duration.Ticks}" : response.Text))}";

    private readonly record struct PhaseRange(ushort Start, ushort End);
}
