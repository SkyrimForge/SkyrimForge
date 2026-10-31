namespace SkyrimForge;

/// <summary>Arguments for <see cref="IQuestSceneEvents.OnPhaseBegin"/> and <see cref="IQuestSceneEvents.OnPhaseEnd"/>.</summary>
/// <param name="Scene">The scene.</param>
/// <param name="PhaseIndex">The index in <see cref="QuestScene.Phases"/> of the phase that began or ended.</param>
public readonly record struct QuestScenePhaseEventArgs(QuestScene Scene, int PhaseIndex);
