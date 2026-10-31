using SkyrimForge.Game;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Launches a spell and an arrow from the player on F11.</summary>
public sealed class ProjectilePlugin(GameTarget gameTarget, IGameEvents events, IDebug debug, IConsole console, IUtility utility, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF11, ModifierKeys.None).Subscribe(_ => SendAsync());
        log.Write("F11 launches a spell and an arrow from the player.");
    }

    private async Task SendAsync()
    {
        await utility.WaitForGameThreadAsync();
        Weapon bow = ExampleForms.HuntingBow.Form;
        Ammo arrow = ExampleForms.IronArrow.Form;
        Spell flames = ExampleForms.Flames.Form;

        PlayerCharacter shooter = gameTarget.Player;

        Projectile? spellProjectile = shooter.LaunchSpellFromAim(flames);
        Projectile? arrowProjectile = Projectile.LaunchArrow(shooter, arrow, bow);

        string message = $"The spell {Describe(spellProjectile)}. The arrow {Describe(arrowProjectile)}.";
        log.Write(message);
        debug.Notification($"SkyrimForge: spell {Describe(spellProjectile)}, arrow {Describe(arrowProjectile)}.");
        console.Write(message);
    }

    private static string Describe(Projectile? projectile) => projectile is { } madeProjectile ? $"made 0x{madeProjectile.FormId}" : "made none";
}
