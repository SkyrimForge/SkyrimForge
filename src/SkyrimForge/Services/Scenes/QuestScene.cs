using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>A scene of a runtime quest, made of phases played in order.</summary>
/// <remarks>
/// A scene belongs to the quest created with it or with a stage that starts it.
/// Members that use the quest throw <see cref="InvalidOperationException"/> until that quest is created.
/// Call its members on the game's thread with a game loaded, except when subscribing to its events.
/// </remarks>
public sealed class QuestScene
{
    /// <summary>Creates a scene.</summary>
    /// <param name="name">The scene's name, unique among the quest's scenes. Saves record a playing scene by it, so it must not change between plugin versions.</param>
    /// <param name="phases">The phases, played in order. A <see cref="SceneAction"/> converts to a phase of that action alone.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="phases"/> is empty, a phase has no action and no <see cref="ScenePhase.EndsWhen"/>, or no action is performed by an actor.
    /// Or an action's <see cref="SceneAction.EndPhaseIndex"/> is before its own phase or past the last phase, or a <see cref="SceneConversation"/>'s is a later phase.
    /// Or two <see cref="ScenePackage"/> actions point one alias's link with one keyword at two aliases.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="phases"/> is <c>null</c>.</exception>
    public QuestScene(string name, params IEnumerable<ScenePhase> phases)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(phases);
        Name = name;
        Phases = [.. phases];
        if (Phases.Count == 0)
        {
            throw new ArgumentException($"The scene \"{name}\" has no phase.", nameof(phases));
        }
        if (Phases.Index().FirstOrDefault(entry => entry.Item.Actions.Count == 0) is { Item: not null } emptyPhase)
        {
            throw new ArgumentException($"Phase {emptyPhase.Index} of the scene \"{name}\" has no action and no {nameof(ScenePhase.EndsWhen)}.", nameof(phases));
        }
        if (!Phases.SelectMany(phase => phase.Actions).Any(action => action.Performer != null))
        {
            throw new ArgumentException($"The scene \"{name}\" has no action an actor performs.", nameof(phases));
        }
        foreach (var (i, phase) in Phases.Index())
        {
            if (phase.Actions.FirstOrDefault(action => action.EndPhaseIndex is { } endPhaseIndex && (endPhaseIndex < i || endPhaseIndex >= Phases.Count))?.EndPhaseIndex is { } invalidPhaseIndex)
            {
                throw new ArgumentException($"An action of phase {i} of the scene \"{name}\" ends in phase {invalidPhaseIndex}, outside phases {i} to {Phases.Count - 1}.", nameof(phases));
            }
            if (phase.Actions.OfType<SceneConversation>().Any(conversation => conversation.EndPhaseIndex is { } endPhaseIndex && endPhaseIndex != i))
            {
                throw new ArgumentException($"A conversation of phase {i} of the scene \"{name}\" ends in a later phase. A conversation ends in the phase in which it begins.", nameof(phases));
            }
        }
        if (Phases.SelectMany(phase => phase.Actions).OfType<ScenePackage>().Where(action => action.LinkedRef != null)
            .GroupBy(action => (action.Actor, action.LinkedPackage!.KeywordId)).FirstOrDefault(link => link.Select(action => action.LinkedRef).Distinct().Count() > 1) is { } conflictingActions)
        {
            throw new ArgumentException($"The scene \"{name}\" points a link of the alias \"{conflictingActions.Key.Actor.Name}\" at two aliases.", nameof(phases));
        }
    }

    internal IEnumerable<(SceneAction Action, int Start, int End)> GetActions(int fromPhase) =>
        Phases.SelectMany((phase, i) => phase.Actions.Select(action => (Action: action, Start: i, End: action.EndPhaseIndex ?? i))).Where(entry => entry.End >= fromPhase);

    internal IEnumerable<QuestReferenceAlias> GetActors(int fromPhase) => GetActions(fromPhase).SelectMany(entry => entry.Action.Actors);

    /// <summary>The scene's name.</summary>
    public string Name { get; }

    /// <summary>The phases, in the order played.</summary>
    public IReadOnlyList<ScenePhase> Phases { get; }

    /// <summary>The quest the scene belongs to.</summary>
    /// <exception cref="InvalidOperationException">No quest has been created with the scene or with a stage that starts it.</exception>
    public IRuntimeQuest Quest => Owner.Quest;

    private IQuestSceneEvents? events;

    internal IScenePlayback? OwnerOrNull { get; set; }

    private IScenePlayback Owner => OwnerOrNull ?? throw new InvalidOperationException($"The scene \"{Name}\" belongs to no quest. "
        + "A scene belongs to the quest created with it or with a stage that starts it, from that quest's creation.");

    /// <summary>Returns whether the scene may start. A null condition always permits the start.</summary>
    /// <remarks>It runs on the thread that starts the scene. An exception from it is logged and counts as false.</remarks>
    public Func<QuestScene, bool>? Condition { get; init; }

    /// <summary>
    /// Whether the scene starts again each time it ends with <see cref="QuestSceneEndReason.Completed"/>,
    /// until <see cref="Condition"/> returns false or the scene is stopped.
    /// </summary>
    public bool Repeats { get; init; }

    /// <summary>Whether a scene that starts later with one of this scene's actors takes the actor and ends this one. True by default.</summary>
    /// <remarks><see cref="Start"/> of another scene of this plugin's quests that shares an actor stops this one regardless.</remarks>
    public bool IsInterruptible { get; init; } = true;

    /// <summary>
    /// Whether each actor that says a line or runs a package of the scene stands in place through the phases in which it runs no package of the scene.
    /// False by default.
    /// </summary>
    /// <remarks>The player does not stand in place.</remarks>
    public bool ActorsStayInPlace { get; init; }

    /// <summary>
    /// The player controls disabled from the frame the scene begins to play until it ends. <see cref="ScenePlayerControls.None"/> by default.
    /// </summary>
    /// <remarks>
    /// The controls are enabled between two repeats of a scene whose <see cref="Repeats"/> is true.
    /// The game does not count disables, so the scene's end also enables a control that another mod or the game disabled.
    /// </remarks>
    public ScenePlayerControls DisabledPlayerControls { get; init; }

    /// <summary>The stage the quest sets when the scene ends with <see cref="QuestSceneEndReason.Completed"/>, or null for none. The quest must declare the stage.</summary>
    public QuestStage? StageSetWhenCompleted { get; init; }

    /// <summary>The settings of the scene's actors, at most one for each alias.</summary>
    /// <exception cref="ArgumentException">Two settings specify one alias, or one specifies an alias that is no actor of the scene's actions.</exception>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyList<SceneActorSettings> ActorSettings
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            SceneActorSettings[] actors = [.. value];
            HashSet<QuestReferenceAlias> phaseActionActors = [.. Phases.SelectMany(phase => phase.Actions).SelectMany(action => action.Actors)];
            HashSet<QuestReferenceAlias> foundActors = [];
            foreach (SceneActorSettings actorSettings in actors)
            {
                if (!foundActors.Add(actorSettings.Actor))
                {
                    throw new ArgumentException($"The scene \"{Name}\" lists two settings for the alias \"{actorSettings.Actor.Name}\".", nameof(ActorSettings));
                }
                if (!phaseActionActors.Contains(actorSettings.Actor))
                {
                    throw new ArgumentException($"The scene \"{Name}\" lists settings for the alias \"{actorSettings.Actor.Name}\", which none of its actions uses.", nameof(ActorSettings));
                }
            }
            field = actors;
        }
    } = [];

    /// <summary>Starts the scene. A scene already playing plays on, and one whose <see cref="Condition"/> returns false does not start.</summary>
    /// <remarks>
    /// Starting the scene does not start the quest it belongs to.
    /// Each other scene of this plugin's quests playing with an actor of this one is stopped first, and this one starts two seconds after they have stopped playing.
    /// A scene that shares an actor alias with a scene of its quest that ended less than two seconds of unpaused time earlier starts two seconds after that end.
    /// The game ends a scene of its own that an actor of this one plays in.
    /// A start waits up to one second for an actor that is not loaded,
    /// and the scene ends with <see cref="QuestSceneEndReason.ActorUnavailable"/> if the actor has not loaded by then.
    /// Loading a save written while the scene plays restarts the phase it was in.
    /// </remarks>
    /// <param name="firstPhaseIndex">
    /// The index in <see cref="Phases"/> of the phase the scene plays from. Earlier phases raise no event and set no stage.
    /// An action begun earlier that runs into this phase starts with it. Each repeat plays from phase 0.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstPhaseIndex"/> is no index in the scene's phases.</exception>
    /// <exception cref="InvalidOperationException">
    /// The game has no form for the package record of a <see cref="ScenePackage"/> of the scene or for the keyword of its <see cref="LinkedRefPackage"/>,
    /// or the game created no travel package for one that travels.
    /// </exception>
    public QuestSceneStartResult Start(int firstPhaseIndex = 0) => Owner.Start(this, firstPhaseIndex);

    /// <summary>
    /// Starts the scene as <see cref="Start"/> does, except that an actor playing in a scene that is not interruptible is removed from that scene, which ends.
    /// </summary>
    /// <remarks>Each repeat of a scene whose <see cref="Repeats"/> is true starts as <see cref="Start"/> does.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstPhaseIndex"/> is no index in the scene's phases.</exception>
    /// <exception cref="InvalidOperationException">
    /// The game has no form for the package record of a <see cref="ScenePackage"/> of the scene or for the keyword of its <see cref="LinkedRefPackage"/>,
    /// or the game created no travel package for one that travels.
    /// </exception>
    public QuestSceneStartResult ForceStart(int firstPhaseIndex = 0) => Owner.ForceStart(this, firstPhaseIndex);

    /// <summary>Starts the scene as <see cref="Start"/> does, and returns a task that completes on the game's thread as the scene ends.</summary>
    /// <returns>The reason the scene ended, or null if <see cref="Start"/> would not return <see cref="QuestSceneStartResult.Started"/>.</returns>
    /// <remarks>
    /// The task completes at the first end of a scene whose <see cref="Repeats"/> is true, and the scene plays on.
    /// A load beginning completes it with <see cref="QuestSceneEndReason.NewGameOrSaveLoading"/>, and the code after the await then runs during the load.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstPhaseIndex"/> is no index in the scene's phases.</exception>
    /// <exception cref="InvalidOperationException">
    /// The scene belongs to no quest. The other failures of <see cref="Start"/> are stored in the returned task.
    /// </exception>
    public Task<QuestSceneEndReason?> PlayAsync(int firstPhaseIndex = 0) => Owner.PlayAsync(this, firstPhaseIndex);

    /// <summary>Stops the scene if it is playing.</summary>
    public void Stop() => Owner.Stop(this);

    /// <summary>Whether the scene is playing, from <see cref="Start"/> until <see cref="IQuestSceneEvents.OnEnd"/> is raised for it.</summary>
    public bool IsPlaying => Owner.IsPlaying(this);

    /// <summary>The events of this scene alone.</summary>
    public IQuestSceneEvents Events => events ??= Owner.GetEvents(this);

    /// <summary>The index in <see cref="Phases"/> of the phase the scene plays, or null if the scene is not playing or its first phase has not begun.</summary>
    public int? CurrentPhaseIndex => Owner.GetCurrentPhaseIndex(this);
}
