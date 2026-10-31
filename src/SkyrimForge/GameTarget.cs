namespace SkyrimForge;

/// <summary>
/// The game's singletons, such as the player, the data handler, the user interface, and the calendar.
/// Each property throws until the game has built its object.
/// </summary>
public sealed partial class GameTarget
{
    private readonly ISingletonSource singletonSource;

    internal GameTarget(ISingletonSource singletonSource)
    {
        this.singletonSource = singletonSource;
    }
}
