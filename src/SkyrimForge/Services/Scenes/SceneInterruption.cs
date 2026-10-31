namespace SkyrimForge;

/// <summary>The effect an event involving a scene's actor has on the scene.</summary>
public enum SceneInterruption
{
    /// <summary>The scene plays on.</summary>
    None,

    /// <summary>The scene pauses until the event is over.</summary>
    Pause,

    /// <summary>The game ends the scene.</summary>
    End
}
