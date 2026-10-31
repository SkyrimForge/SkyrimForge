using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A phase of a <see cref="QuestScene"/>, whose actions start together.</summary>
/// <remarks>The next phase starts once each action ending in this one has ended.</remarks>
public sealed class ScenePhase
{
    private readonly IReadOnlyList<SceneAction> declaredActions;

    // The game ends a phase that has an EndsWhen only once it returns true, so the timer's duration does not bound the phase.
    private readonly IReadOnlyList<SceneAction> waitActions = [new SceneTimer(TimeSpan.FromSeconds(1))];

    /// <summary>Creates a phase.</summary>
    /// <param name="actions">The actions. A phase with none waits until <see cref="EndsWhen"/> returns true.</param>
    /// <exception cref="ArgumentNullException"><paramref name="actions"/> is <c>null</c>.</exception>
    public ScenePhase(params IEnumerable<SceneAction> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        declaredActions = [.. actions];
    }

    /// <summary>The actions. A phase created with none that has an <see cref="EndsWhen"/> has one <see cref="SceneTimer"/>.</summary>
    public IReadOnlyList<SceneAction> Actions => declaredActions.Count == 0 && EndsWhen != null ? waitActions : declaredActions;

    /// <summary>Returns whether the phase plays. Null means the phase always plays.</summary>
    /// <remarks>
    /// It runs on the game's thread on each frame the scene plays, and the game reads its last result one frame later.
    /// An exception from it is logged and counts as false.
    /// </remarks>
    public Func<QuestScene, bool>? Condition { get; init; }

    /// <summary>Returns whether the phase is complete. Null means the phase's actions end it.</summary>
    /// <remarks>
    /// The phase ends once it returns true, even while its actions still run, and not before, even after they have ended. <see cref="TimeLimit"/> bounds the wait.
    /// It runs on the game's thread on each frame the scene plays, and the game reads its last result one frame later.
    /// An exception from it is logged and counts as false.
    /// </remarks>
    public Func<QuestScene, bool>? EndsWhen { get; init; }

    /// <summary>The longest unpaused time the phase lasts. Null means no limit.</summary>
    /// <remarks>The time counts from the frame <see cref="IQuestSceneEvents.OnPhaseBegin"/> is raised, and from zero again for a phase played again after a load.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public TimeSpan? TimeLimit
    {
        get;
        init
        {
            if (value is { } limit)
            {
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(limit, TimeSpan.Zero, nameof(TimeLimit));
            }
            field = value;
        }
    }

    /// <summary>The stage the quest sets when the phase begins, or null for none. The quest must declare the stage.</summary>
    public QuestStage? StageSetWhenBegun { get; init; }

    /// <summary>Converts <paramref name="action"/> to a phase of that action alone.</summary>
    public static implicit operator ScenePhase(SceneAction action) => new(action);
}
