using SkyrimForge.Game;
using SkyrimForge.Papyrus;
using System;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Binds a Papyrus script to the object under the crosshair when F7 is pressed, with no form record attaching it.</summary>
/// <remarks>
/// The Papyrus VM creates the same instance as for a script attached in the Creation Kit, and the game writes it into the save.
/// The build generates <see cref="SkyrimForgeCounterScript"/> from Scripts\Source\SkyrimForgeCounter.psc.
/// </remarks>
public sealed class PapyrusAttachPlugin(GameTarget gameTarget, IGameEvents events, IUtility utility, ILog log) : IPlugin
{
    private static string ScriptName => SkyrimForgeCounterScript.Dispatcher.ScriptName;

    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF7, ModifierKeys.None).Subscribe(_ => BindAsync());
        log.Write($"F7 binds {ScriptName} to the object under the crosshair and logs its activation count.");
    }

    private async Task BindAsync()
    {
        await utility.WaitForGameThreadAsync();
        if (gameTarget.CrosshairReference is not { } crosshairReference)
        {
            log.Write("Nothing is under the crosshair.");
            return;
        }

        try
        {
            SkyrimForgeCounterScript.Dispatcher.Bind(crosshairReference);
            // Counter is an Auto property, so reading it reads the instance's variable without a Papyrus call.
            int activationCount = crosshairReference.Papyrus.Counter;
            log.Write($"{ScriptName} is bound to {crosshairReference.GetDisplayFullName()}, 0x{crosshairReference.FormId}. Activations: {activationCount}.");
        }
        catch (InvalidOperationException ex)
        {
            log.Write($"{ScriptName} was not bound to 0x{crosshairReference.FormId}: {ex.Message}");
        }
        log.Write(await SkyrimForgeCounterScript.DescribePlayerGold());
    }
}
