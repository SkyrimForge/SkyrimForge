namespace SkyrimForge;

/// <summary>Arguments for <see cref="IQuestSceneEvents.OnLineBegin"/> and <see cref="IQuestSceneEvents.OnLineEnd"/>.</summary>
/// <param name="Scene">The scene.</param>
/// <param name="Line">The line.</param>
/// <param name="VariantIndex">The index in <see cref="SceneLine.Variants"/> of the variant said.</param>
public readonly record struct QuestSceneLineEventArgs(QuestScene Scene, SceneLine Line, int VariantIndex);
