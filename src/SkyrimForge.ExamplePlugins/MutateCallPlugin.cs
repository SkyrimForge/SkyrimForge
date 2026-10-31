using SkyrimForge.Game;
using System;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Calls game functions that change state, by turning and lifting the player once they are in the world and restoring both in the same frame.</summary>
public sealed class MutateCallPlugin(GameTarget gameTarget, IGameEvents events, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        MutateOnceInWorldAsync().LogFault(log);
        log.Write("Turns and lifts the player once they are in the world, then restores both.");
    }

    private async Task MutateOnceInWorldAsync()
    {
        const float liftUnits = 64f;

        // At the main menu the player is a default character with no 3D loaded.
        await events.OnFrame.WaitForNextAsync(_ => gameTarget.Player.Is3DLoaded());
        PlayerCharacter player = gameTarget.Player;

        float heading = player.GetAngle().Z;
        player.SetHeading(heading + (MathF.PI / 2f));
        float turnedHeading = player.GetAngle().Z;
        player.SetHeading(heading);

        NiPoint3 originalPosition = player.GetPosition();
        player.SetPosition(originalPosition with { Z = originalPosition.Z + liftUnits }, true);
        NiPoint3 liftedPosition = player.GetPosition();
        player.SetPosition(originalPosition, true);

        log.Write($"SetHeading: heading {heading:F3} rad, after +90 deg {turnedHeading:F3}. SetPosition: z {originalPosition.Z:F1}, after +{liftUnits:F0} {liftedPosition.Z:F1}. Both restored.");
    }
}
