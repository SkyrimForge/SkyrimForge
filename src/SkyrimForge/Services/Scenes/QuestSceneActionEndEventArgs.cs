namespace SkyrimForge;

/// <summary>Arguments for <see cref="IQuestSceneEvents.OnActionEnd"/>.</summary>
/// <param name="Scene">The scene.</param>
/// <param name="PhaseIndex">The index in <see cref="QuestScene.Phases"/> of the phase whose actions include the action.</param>
/// <param name="Action">The action.</param>
public readonly record struct QuestSceneActionEndEventArgs(QuestScene Scene, int PhaseIndex, SceneAction Action);
