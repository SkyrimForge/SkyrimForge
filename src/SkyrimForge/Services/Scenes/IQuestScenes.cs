namespace SkyrimForge;

/// <summary>The scenes of an <see cref="IRuntimeQuest"/> as a group.</summary>
/// <remarks>Call members on the game's thread with a game loaded, except when subscribing to an event.</remarks>
public interface IQuestScenes : IQuestSceneEvents
{
    /// <summary>Stops each playing scene of the quest.</summary>
    void StopAll();

    /// <summary>Whether a scene of the quest is playing.</summary>
    bool IsAnyPlaying { get; }
}
