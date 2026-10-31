namespace SkyrimForge;

/// <summary>Arguments for <see cref="IQuestSceneEvents.OnEnd"/>.</summary>
/// <param name="Scene">The scene that ended.</param>
/// <param name="Reason">The way it ended.</param>
/// <param name="ActorInCombat">
/// The alias of an actor whose <see cref="SceneActorSettings.CombatInterruption"/> is <see cref="SceneInterruption.End"/>,
/// in combat when the game ended the scene with <see cref="QuestSceneEndReason.Interrupted"/>. The value is null if none was.
/// The game reports no reason for a scene's end, so the combat is not confirmed as the cause.
/// </param>
public readonly record struct QuestSceneEndEventArgs(QuestScene Scene, QuestSceneEndReason Reason, QuestReferenceAlias? ActorInCombat = null);
