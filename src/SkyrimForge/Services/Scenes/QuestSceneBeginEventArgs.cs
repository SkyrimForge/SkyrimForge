namespace SkyrimForge;

/// <summary>Arguments for <see cref="IQuestSceneEvents.OnBegin"/>.</summary>
/// <param name="Scene">The scene that began to play.</param>
/// <param name="FirstPhaseIndex">The index in <see cref="QuestScene.Phases"/> of the phase it plays from, or at which it resumes after a load.</param>
public readonly record struct QuestSceneBeginEventArgs(QuestScene Scene, int FirstPhaseIndex);
