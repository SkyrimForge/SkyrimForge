namespace SkyrimForge;

/// <summary>The events of one scene or of every scene of a quest.</summary>
public interface IQuestSceneEvents
{
    /// <summary>Raised on the game's thread in the first frame a scene plays, including each repeat and a resume after a load.</summary>
    /// <remarks>A scene that ends before it plays raises <see cref="OnEnd"/> alone.</remarks>
    IGameEvent<QuestSceneBeginEventArgs> OnBegin { get; }

    /// <summary>Raised on the game's thread as each phase of a scene begins, including the phase at which a scene resumes after a load.</summary>
    IGameEvent<QuestScenePhaseEventArgs> OnPhaseBegin { get; }

    /// <summary>Raised on the game's thread as a phase ends.</summary>
    /// <remarks>A scene that ends other than with <see cref="QuestSceneEndReason.Completed"/> raises it for no unfinished phase.</remarks>
    IGameEvent<QuestScenePhaseEventArgs> OnPhaseEnd { get; }

    /// <summary>Raised once on the game's thread as an actor begins to say a <see cref="SceneLine"/> of a scene, however many responses the line has.</summary>
    IGameEvent<QuestSceneLineEventArgs> OnLineBegin { get; }

    /// <summary>Raised on the game's thread as an actor finishes saying a <see cref="SceneLine"/> of a scene.</summary>
    /// <remarks>A line still being said when its scene ends raises none.</remarks>
    IGameEvent<QuestSceneLineEventArgs> OnLineEnd { get; }

    /// <summary>Raised on the game's thread as an action of a scene ends, when the game reports it complete or as the phase it ends in ends, whichever is first.</summary>
    /// <remarks>
    /// A scene that ends with <see cref="QuestSceneEndReason.Completed"/> raises it for each action still running, before <see cref="OnPhaseEnd"/>.
    /// A scene that ends otherwise raises it for none of them.
    /// </remarks>
    IGameEvent<QuestSceneActionEndEventArgs> OnActionEnd { get; }

    /// <summary>Raised on the game's thread once a scene ends.</summary>
    IGameEvent<QuestSceneEndEventArgs> OnEnd { get; }
}
