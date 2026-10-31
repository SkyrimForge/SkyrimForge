namespace SkyrimForge;

/// <summary>A unit of behavior loaded into a game.</summary>
/// <remarks>
/// An implementation declares exactly one public constructor, whose parameters are the <see href="https://docs.skyrimforge.net/reference/services.html">services</see> it uses.
/// Its saved state and log file are stored under the full name of its type or under the name its <see cref="PluginIdentityAttribute"/> states.
/// </remarks>
public interface IPlugin
{
    /// <summary>Runs the plugin's startup work, once every plugin has been constructed.</summary>
    /// <remarks>
    /// It runs on the game's thread before <see cref="IGameEvents.OnDataLoaded"/>, so the game has not loaded its data files or built the player.
    /// Subscribe to events here, and call the game from their handlers.
    /// Use <see cref="IGameEvents.OnDataLoaded"/> for work that needs the game's forms,
    /// and <see cref="IGameEvents.OnNewGameOrSaveLoaded"/> for work that belongs to a game in progress, including waits, which a load cancels.
    /// </remarks>
    void Start();
}
