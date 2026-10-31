using System;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="GameTarget"/>.</summary>
public static class GameTargetExtensions
{
    extension(GameTarget gameTarget)
    {
        /// <summary>Shakes the camera, as Papyrus's Game.ShakeCamera does.</summary>
        /// <param name="source">The reference at whose position the shake originates, or <c>null</c> for the player's position.</param>
        /// <param name="strength">The strength, from 0 to 1. A value outside that range is clamped to it.</param>
        /// <param name="duration">The duration in seconds.</param>
        /// <remarks>Call on the game's thread.</remarks>
        public void ShakeCamera(IObjectReference? source = null, float strength = 0.5f, float duration = 0f) =>
            GlobalFunctions.ShakeCamera(Math.Clamp(strength, 0f, 1f), source?.GetPosition() ?? gameTarget.Player.GetPosition(), duration);

        /// <summary>Moves the player to the cell whose editor ID is <paramref name="editorId"/>, as Papyrus's Debug.CenterOnCell does.</summary>
        /// <returns>Whether the game found the cell.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="editorId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="editorId"/> is empty. Papyrus logs an error and moves nothing.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public bool CenterOnCell(string editorId)
        {
            ArgumentException.ThrowIfNullOrEmpty(editorId);
            return gameTarget.Player.CenterOnCell(editorId);
        }

        /// <summary>The reference under the crosshair. Null if the crosshair is over none.</summary>
        /// <remarks>The game builds its CrosshairPickData after a plugin starts and before the main menu opens. Call on the game's thread.</remarks>
        /// <exception cref="InvalidOperationException">The game has not built its CrosshairPickData yet.</exception>
        public ObjectReference? CrosshairReference => gameTarget.CrosshairPickData.GetActiveTarget();

        /// <summary>Sets the flag the game's main loop tests to exit, as quitting to the desktop does.</summary>
        public void QuitGame()
        {
            Main.RUNTIME_DATA runtimeData = gameTarget.Main.RuntimeData;
            runtimeData.QuitGame = true;
        }
    }
}
