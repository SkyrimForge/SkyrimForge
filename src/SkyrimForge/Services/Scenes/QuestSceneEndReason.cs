namespace SkyrimForge;

/// <summary>The way a <see cref="QuestScene"/> ended.</summary>
public enum QuestSceneEndReason
{
    /// <summary>Its last phase was played.</summary>
    Completed,

    /// <summary><see cref="QuestScene.Stop"/> stopped it, or another scene of this plugin's quests that shares an actor with it started.</summary>
    Stopped,

    /// <summary>
    /// A non-optional actor of an action still to come had an empty alias or one filled with a reference that is not an actor, or was dead, disabled, deleted, or not loaded.
    /// The scene also ends this way if an action still to come is a package whose <see cref="ScenePackage.LinkedRef"/> alias is empty,
    /// or a travel package whose destination is an empty alias, or a location alias that is empty or whose location has no world location marker.
    /// </summary>
    ActorUnavailable,

    /// <summary>The game ended it before its last phase, as when an actor in the scene enters combat, or it did not start.</summary>
    Interrupted,

    /// <summary>A new game started or a save started loading. A scene the loaded save recorded as playing continues in the loaded game.</summary>
    NewGameOrSaveLoading,

    /// <summary>
    /// The game ended it as a scene of the game or of another plugin started with one of its actors,
    /// or it did not start because one of its actors was in a scene that is not interruptible.
    /// </summary>
    ActorInAnotherScene
}
