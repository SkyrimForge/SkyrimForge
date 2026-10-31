using System;

namespace SkyrimForge;

/// <summary>The flags and behaviors of one actor of a <see cref="QuestScene"/>. An actor the scene lists none for has the defaults.</summary>
public sealed class SceneActorSettings
{
    /// <summary>Creates the settings of the actor filling <paramref name="actor"/>.</summary>
    /// <param name="actor">An alias whose actor says a line of the scene, runs a package of it, takes part in a conversation of it, or is looked at in it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> is <c>null</c>.</exception>
    public SceneActorSettings(QuestReferenceAlias actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        Actor = actor;
    }

    /// <summary>The alias of the actor these settings govern.</summary>
    public QuestReferenceAlias Actor { get; }

    /// <summary>Whether the scene can play without the actor. False by default.</summary>
    /// <remarks>
    /// The game skips the lines of a disabled optional actor, and ends the scene with <see cref="QuestSceneEndReason.Interrupted"/> if the actor is in another cell.
    /// An optional actor whose alias is filled after the scene starts says no line.
    /// </remarks>
    public bool IsOptional { get; init; }

    /// <summary>Whether the player cannot activate the actor while the scene plays. False by default.</summary>
    public bool BlocksPlayerActivation { get; init; }

    /// <summary>Whether the actor's remarks, the lines it says unprompted outside a conversation, are silenced while the scene plays. False by default.</summary>
    public bool SilencesRemarks { get; init; }

    /// <summary>Whether the actor runs only the scene's packages while the scene plays. False by default.</summary>
    /// <remarks>The actor walks off during a phase in which it has no package action.</remarks>
    public bool RunsOnlyScenePackages { get; init; }

    /// <summary>Whether the game ends the scene if the actor dies. True by default.</summary>
    public bool EndsOnDeath { get; init; } = true;

    /// <summary>The effect of the actor entering combat. <see cref="SceneInterruption.End"/> by default.</summary>
    public SceneInterruption CombatInterruption { get; init; } = SceneInterruption.End;

    /// <summary>The effect of the actor entering a conversation. <see cref="SceneInterruption.Pause"/> by default.</summary>
    public SceneInterruption DialogueInterruption { get; init; } = SceneInterruption.Pause;

    /// <summary>The effect of the actor observing combat. <see cref="SceneInterruption.None"/> by default.</summary>
    public SceneInterruption ObservedCombatInterruption { get; init; }
}
