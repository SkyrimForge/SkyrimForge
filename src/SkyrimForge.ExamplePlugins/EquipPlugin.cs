using SkyrimForge.Game;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Equips the most valuable weapon in the player's inventory when F4 is pressed, and logs each hand before and after.</summary>
/// <remarks>Equipping an object not in the inventory changes the equipped slot and the animation but shows no model.</remarks>
public sealed class EquipPlugin(GameTarget gameTarget, IGameEvents events, IConsole console, IUtility utility, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF4, ModifierKeys.None).Subscribe(_ => EquipAsync());
        log.Write("F4 equips the most valuable weapon in the player's inventory.");
    }

    private async Task EquipAsync()
    {
        await utility.WaitForGameThreadAsync();
        PlayerCharacter player = gameTarget.Player;

        // Every inventory contains an unnamed weapon that the game equips to sheathe.
        List<Weapon> weapons = [.. player.GetInventoryCounts()
            .Where(entry => entry.Value > 0)
            .Select(entry => entry.Key.AsWeapon())
            .OfType<Weapon>().Where(weapon => !string.IsNullOrEmpty(weapon.GetName())).OrderByDescending(weapon => weapon.GetGoldValue()).ThenBy(weapon => weapon.FormId)];
        if (weapons.Count == 0)
        {
            log.Write("The player has no weapon to equip.");
            return;
        }

        Weapon bestWeapon = weapons[0];

        string handsBefore = DescribeHands(player);

        gameTarget.ActorEquipManager.EquipObject(player, bestWeapon);

        IReadOnlyList<ExtraDataList> stacks = player.GetEquippedEntryData(false) is { } entry ? entry.ExtraLists.Get() : [];
        string stackText = stacks.Count == 0 ? "no separate stacks" : string.Join("; ", stacks.Select(stack => $"count {stack.GetCount()}, soul {stack.GetSoulLevel()}"));

        string message = $"Equipped \"{bestWeapon.GetName()}\" 0x{bestWeapon.FormId}, value {bestWeapon.GetGoldValue()}, the most valuable of {weapons.Count} named weapons. "
            + $"Before: {handsBefore}. After: {DescribeHands(player)}. Stacks: {stackText}.";
        log.Write(message);
        console.Write($"SkyrimForge: {message}");
    }

    private static string DescribeHands(PlayerCharacter player)
    {
        return $"right {Identify(player.GetEquippedObject(false))}, left {Identify(player.GetEquippedObject(true))}";
    }

    private static string Identify(Form? form)
    {
        return form is { } equippedForm ? $"0x{equippedForm.FormId}" : "nothing";
    }
}
