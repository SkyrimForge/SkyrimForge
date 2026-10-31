namespace SkyrimForge;

/// <summary>The result of <see cref="QuestScene.Start"/> and <see cref="QuestScene.ForceStart"/>.</summary>
public enum QuestSceneStartResult
{
    /// <summary>
    /// The game begins playing the scene on a later frame. <see cref="IQuestSceneEvents.OnEnd"/> is raised for this start even if the game does not play it.
    /// </summary>
    Started,

    /// <summary>
    /// The scene was already playing and continues. A scene that <see cref="QuestScene.Stop"/> stopped counts as playing until its end is raised on a later frame.
    /// </summary>
    AlreadyPlaying,

    /// <summary>The scene's <see cref="QuestScene.Condition"/> returned false, or threw.</summary>
    ConditionNotMet,

    /// <summary>The game has not loaded its data.</summary>
    GameDataNotLoaded
}
