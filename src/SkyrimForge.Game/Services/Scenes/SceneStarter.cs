using SkyrimForge.Papyrus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class SceneStarter(IUtility utility, ISceneOwner owner, SceneReferences references, ScenePlayerControl playerControl, LinkedRefChanges linkedRefs, ILog log, string questName, Func<TimeSpan> getNow)
{
    private static readonly TimeSpan playWait = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan loadWait = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan stopInterval = TimeSpan.FromSeconds(0.25);

    // A scene started less than this long after the end of a scene that shares one of its actors can have a phase end as it begins.
    private static readonly TimeSpan releaseDelay = TimeSpan.FromSeconds(2);

    private readonly IUtility utility = utility;
    private readonly ISceneOwner owner = owner;
    private readonly SceneReferences references = references;
    private readonly ScenePlayerControl playerControl = playerControl;
    private readonly LinkedRefChanges linkedRefs = linkedRefs;
    private readonly ILog log = log;
    private readonly string questName = questName;
    private readonly Func<TimeSpan> getNow = getNow;

    private readonly Dictionary<QuestReferenceAlias, TimeSpan> releaseTimes = [];

    internal void RecordRelease(IEnumerable<QuestReferenceAlias> actors)
    {
        foreach (QuestReferenceAlias actor in actors)
        {
            releaseTimes[actor] = getNow();
        }
    }

    internal void ClearReleaseTimes() => releaseTimes.Clear();

    // Papyrus's Scene.Start does nothing for a quest the game is still starting.
    // The first wait is needed because a scene quest started inside an OnSaveLoaded handler is listed twice among the story teller's running quests,
    // and its scene updates twice each frame.
    internal async Task StartAsync(QuestScene scene, ScenePlay play, SceneHostQuest hostQuest, IReadOnlyCollection<QuestReferenceAlias> actors, IReadOnlyList<Scene> stoppedScenes, CancellationToken newGameOrSaveLoadingToken)
    {
        await utility.WaitAsync(TimeSpan.Zero, newGameOrSaveLoadingToken);
        await WaitForActorsAsync(actors, stoppedScenes, newGameOrSaveLoadingToken);
        TimeSpan loadDeadline = getNow() + loadWait;
        while (getNow() < loadDeadline && play.EndReason == null && references.GetUnavailabilityReason(scene, play.FirstPhase) != null)
        {
            await utility.WaitAsync(TimeSpan.Zero, newGameOrSaveLoadingToken);
        }
        if (play.EndReason != null)
        {
            return;
        }
        if (references.GetUnavailabilityReason(scene, play.FirstPhase) is { } unavailabilityReason)
        {
            log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" did not start: {unavailabilityReason}.");
            play.EndReason = QuestSceneEndReason.ActorUnavailable;
            return;
        }
        // A scene without the Interruptible flag does not release an actor to a later scene, which then plays without the actor.
        if (!play.IsForced && scene.GetActors(play.FirstPhase).FirstOrDefault(alias => owner.GetReference(alias)?.FormOrNull?.GetCurrentScene() is { IsPlaying: true } current
            && !current.Flags.HasFlag(Scene.Flag.kInterruptible)) is { } occupiedAlias)
        {
            log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" did not start: the actor of the alias \"{occupiedAlias.Name}\" plays in a scene that is not interruptible.");
            play.EndReason = QuestSceneEndReason.ActorInAnotherScene;
            return;
        }
        SetUpPackages(scene, play);
        // A form created at runtime has no Papyrus instance, and dispatching to a form without one ends the process.
        try
        {
            SceneScript.Dispatcher.Bind(play.Scene);
            play.IsBound = true;
        }
        catch (InvalidOperationException ex)
        {
            log.Write($"The scene \"{scene.Name}\" of the quest \"{questName}\" did not start: {ex.Message}");
            play.EndReason = QuestSceneEndReason.Interrupted;
            return;
        }
        await StartQuestAsync(scene, hostQuest, newGameOrSaveLoadingToken);
        if (play.EndReason != null)
        {
            return;
        }
        play.SceneRecord.WriteConditions(log, scene.Name, questName);
        play.ScenesAtStart = [.. scene.GetActors(play.FirstPhase).Select(alias => owner.GetReference(alias)?.FormOrNull?.GetCurrentScene())
            .WhereNotNull().Where(current => current.IsPlaying).Select(current => current.Address)];
        await (play.IsForced ? play.Scene.Papyrus.ForceStart() : play.Scene.Papyrus.Start());
        TimeSpan waitDeadline = getNow() + playWait;
        while (getNow() < waitDeadline && !play.Scene.IsPlaying && play.EndReason == null)
        {
            await utility.WaitAsync(TimeSpan.Zero, newGameOrSaveLoadingToken);
        }
    }

    // A scene does not play if started while a scene it stopped is still playing.
    private async Task WaitForActorsAsync(IReadOnlyCollection<QuestReferenceAlias> actors, IReadOnlyList<Scene> stoppedScenes, CancellationToken newGameOrSaveLoadingToken)
    {
        const int maxStopChecks = 20;
        for (int i = 0; i < maxStopChecks && stoppedScenes.Any(other => other.IsPlaying); i++)
        {
            await utility.WaitAsync(stopInterval, newGameOrSaveLoadingToken);
        }
        if (stoppedScenes.Any(other => other.IsPlaying))
        {
            log.Write($"A scene of the quest \"{questName}\" still played {maxStopChecks * stopInterval.TotalSeconds} seconds after it was stopped, "
                + "and the scene replacing it is started anyway.");
        }
        // A stopped scene can end in a later frame than the one its record stops playing in, so releaseDelay is counted from this frame.
        TimeSpan releaseWait = stoppedScenes.Count > 0 ? releaseDelay : actors.Select(actor => releaseTimes.TryGetValue(actor, out TimeSpan releaseTime) ? releaseTime + releaseDelay - getNow() : TimeSpan.Zero)
            .Append(TimeSpan.Zero).Max();
        if (releaseWait > TimeSpan.Zero)
        {
            await utility.WaitAsync(releaseWait, newGameOrSaveLoadingToken);
        }
    }

    // The player does not move with the generated travel package, so a player travel action runs a package whose location is the player's link.
    // The link points at the first action's destination here, and at each later destination as its phase begins.
    private void SetUpPackages(QuestScene scene, ScenePlay play)
    {
        bool playerPointed = false;
        foreach ((Package package, ScenePackage action, GameList<Package> packages) in play.SceneRecord.Travels)
        {
            if (references.GetDestination(action) is not { } destination)
            {
                continue;
            }
            packages.Clear();
            if (references.IsPlayer(action.Actor))
            {
                packages.Add(ScenePlayerControl.GetPlayerTravelPackage(action).PackageId.Form);
                if (!playerPointed)
                {
                    playerPointed = true;
                    playerControl.SetPlayerLink(play, action);
                }
            }
            else
            {
                packages.Add(package);
                TravelPackages.SetLocation(package, destination, action.Radius);
            }
        }
        foreach (ScenePackage action in scene.GetActions(play.FirstPhase).Select(action => action.Action).OfType<ScenePackage>())
        {
            if (action is { LinkedPackage: { } linkedPackage, LinkedRef: { } linkedRef } && owner.GetReference(action.Actor)?.FormOrNull?.AsActor() is { } actor
                && owner.GetReference(linkedRef)?.FormOrNull is { } target)
            {
                linkedRefs.SetLink(play, actor, linkedPackage.KeywordId?.FormOrNull, target);
            }
        }
    }

    private async Task StartQuestAsync(QuestScene scene, SceneHostQuest hostQuest, CancellationToken newGameOrSaveLoadingToken)
    {
        const int maxQuestFrames = 120;
        Quest quest = hostQuest.Quest;
        for (int i = 0; i < maxQuestFrames && (quest.IsEnabled() || quest.IsStopping()); i++)
        {
            if (quest.IsEnabled() && !quest.IsStopping())
            {
                quest.Stop();
            }
            await utility.WaitAsync(TimeSpan.Zero, newGameOrSaveLoadingToken);
        }
        foreach (QuestReferenceAlias alias in hostQuest.NamedAliases)
        {
            if (owner.GetReference(alias)?.FormOrNull is { } reference)
            {
                hostQuest.Aliases.Force(hostQuest.Aliases.GetId(alias), reference);
            }
        }
        if (!quest.Start())
        {
            log.Write($"The quest record of the scene \"{scene.Name}\" of the quest \"{questName}\" did not start.");
        }
        for (int i = 0; i < maxQuestFrames && quest.IsStarting(); i++)
        {
            await utility.WaitAsync(TimeSpan.Zero, newGameOrSaveLoadingToken);
        }
    }
}
