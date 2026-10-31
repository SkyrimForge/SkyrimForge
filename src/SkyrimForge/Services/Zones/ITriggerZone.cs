namespace SkyrimForge;

/// <summary>A zone <see cref="ITriggerZoneFactory"/> creates.</summary>
/// <remarks>Members can be called on any thread, and the events are raised on the game's thread.</remarks>
public interface ITriggerZone
{
    /// <summary>Whether the player was inside at the latest check, as stored in the loaded game.</summary>
    bool IsPlayerInside { get; }

    /// <summary>Raised at the check that finds the player inside after outside.</summary>
    IGameEvent<TriggerZoneEventArgs> OnEnter { get; }

    /// <summary>Raised at the check that finds the player outside after inside.</summary>
    /// <remarks>A center function returning null, or a center reference the game no longer has, counts as outside.</remarks>
    IGameEvent<TriggerZoneEventArgs> OnLeave { get; }
}
