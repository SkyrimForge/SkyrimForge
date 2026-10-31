namespace SkyrimForge;

/// <summary>A cooldown measured in <see cref="FrameEventArgs.UnpausedTime"/> from each start. A new game or a save load ends it.</summary>
/// <remarks>Members can be called on any thread.</remarks>
public interface ICooldown
{
    /// <summary>Whether the cooldown has started in the current game and its duration has not passed since.</summary>
    bool IsActive { get; }

    /// <summary>Starts the cooldown if it is not active, and returns whether it started. An active cooldown's end does not change.</summary>
    bool TryStart();
}
