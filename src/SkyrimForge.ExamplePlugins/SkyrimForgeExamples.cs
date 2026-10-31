using SkyrimForge.Game;
using SkyrimForge.Papyrus;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Implements the native functions of the Papyrus script SkyrimForgeExamples, which the build declares in Scripts\Source\SkyrimForgeExamples.psc.</summary>
[PapyrusScript]
public sealed class SkyrimForgeExamples(GameTarget gameTarget)
{
    /// <summary>The number of gold coins in the player's inventory.</summary>
    public int GetPlayerGold() => gameTarget.Player.GetGoldAmount();
}
