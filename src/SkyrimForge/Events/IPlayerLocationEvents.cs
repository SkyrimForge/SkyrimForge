namespace SkyrimForge;

/// <summary>Events for the location the player is in.</summary>
public interface IPlayerLocationEvents
{
    /// <summary>Raised on the game's thread on the first frame the player's 3D is loaded after the player enters a location, including by loading a save.</summary>
    /// <remarks>Of several changes before the player is loaded, only the last is raised.</remarks>
    IGameEvent<PlayerLocationEnterEventArgs> OnEnter { get; }
}
