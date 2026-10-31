using SkyrimForge.Game;
using System;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Places an Iron Sword in front of the player when F8 is pressed, waits for its 3D to load, and deletes it ten seconds later.</summary>
/// <remarks>
/// A new game or a save load during a wait cancels the task, and the sword is not deleted, because the reference belongs to the game before the load.
/// </remarks>
public sealed class PlaceObjectPlugin(GameTarget gameTarget, IGameEvents events, IDebug debug, IUtility utility, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF8, ModifierKeys.None).Subscribe(_ => PlaceAsync());
        log.Write("F8 places an Iron Sword in front of the player and deletes it ten seconds later.");
    }

    private async Task PlaceAsync()
    {
        // The height prevents the sword from starting inside a slope.
        const float ahead = 100f;
        const float above = 50f;
        await utility.WaitForGameThreadAsync();
        PlayerCharacter player = gameTarget.Player;
        if (!player.Is3DLoaded())
        {
            log.Write("The player's 3D is not loaded. Nothing was placed.");
            return;
        }

        // The heading is in radians clockwise from north, which is +Y.
        NiPoint3 position = player.GetPosition();
        float heading = player.GetAngleZ();
        NiPoint3 swordPosition = new(position.X + ahead * MathF.Sin(heading), position.Y + ahead * MathF.Cos(heading), position.Z + above);
        ObjectReference sword = player.Place(gameTarget.DataHandler, ExampleForms.IronSword, swordPosition);
        await sword.WaitFor3DLoadAsync(utility, TimeSpan.FromSeconds(5));
        log.Write($"Placed 0x{sword.FormId}. Its 3D is loaded.");
        debug.Notification("SkyrimForge: placed an Iron Sword for ten seconds.");

        await utility.WaitAsync(TimeSpan.FromSeconds(10));
        sword.DisableAndDelete();
        log.Write($"Deleted 0x{sword.FormId}.");
    }
}
