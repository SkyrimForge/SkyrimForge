using SkyrimForge;
using SkyrimForge.Game;

namespace SkyrimForgePlugin;

/// <summary>Shows a notification with the player's name and writes a line to its log each time a new game starts or a save finishes loading.</summary>
public sealed class SkyrimForgePlugin(GameTarget gameTarget, IGameEvents events, IDebug debug, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnNewGameOrSaveLoaded.Subscribe(_ =>
        {
            debug.Notification($"Hello, {gameTarget.Player.GetDisplayFullName()}.");
            log.Write("A new game started or a save finished loading.");
        });
        log.Write("SkyrimForgePlugin started.");
    }
}
