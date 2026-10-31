using SkyrimForge.Game;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Adds a function a menu's ActionScript can call. <c>_root.skyrimForgeGold()</c> returns the player's gold.</summary>
/// <remarks>
/// A plugin extends a menu it did not author by registering the function in an <see cref="IScaleformFunctionRegistrar.OnMovieCreated"/> handler,
/// which also runs each time a menu reopens.
/// </remarks>
public sealed class ScaleformPlugin(GameTarget gameTarget, IGameEvents events, IScaleformFunctionRegistrar scaleform, IUtility utility, ILog log) : IPlugin
{
    // Prefixed, because it becomes a member of the game movie's _root.
    private const string functionName = "skyrimForgeGold";

    private bool watching;

    // Written and read on different threads.
    private int installCount;

    private int calls;

    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF2, ModifierKeys.Shift).Subscribe(_ => InvokeAsync());
        log.Write($"Shift+F2 registers {functionName} on the HUD and on every later menu, then calls it.");
    }

    private async Task InvokeAsync()
    {
        await utility.WaitForGameThreadAsync();
        if (gameTarget.UI.GetMenu(HUDMenu.MenuName) is not { } hud || hud.UiMovie is not { } movie)
        {
            log.Write("The HUD is not loaded.");
            return;
        }

        if (!watching)
        {
            try
            {
                scaleform.OnMovieCreated.Subscribe(args => InstallOnRoot(args.Movie));
                watching = true;
                log.Write($"Every movie created from now on receives {functionName}.");
            }
            catch (InvalidOperationException ex)
            {
                log.Write($"Could not subscribe to OnMovieCreated: {ex.Message}");
            }
        }

        using (ScaleformValue root = ScaleformValue.Create())
        {
            if (!movie.GetVariable(root.Value, "_root"))
            {
                log.Write("The HUD's movie returned no _root.");
                return;
            }
            Install(movie, root.Value);

            using (ScaleformValue result = ScaleformValue.Create())
            {
                bool called = root.Value.Invoke(functionName, result.Value);
                string reportedText = result.Value.IsNumber() ? result.Value.GetNumber().ToString(CultureInfo.InvariantCulture) : "no number";

                root.Value.DeleteMember(functionName);

                log.Write($"{functionName} returned {reportedText} gold (invoke returned {called}). Calls: {Volatile.Read(ref calls)}. Movies: {Volatile.Read(ref installCount)}.");
            }
        }
    }

    private void InstallOnRoot(GFxMovieView movie)
    {
        using (ScaleformValue root = ScaleformValue.Create())
        {
            if (movie.GetVariable(root.Value, "_root"))
            {
                Install(movie, root.Value);
            }
        }
    }

    private void Install(GFxMovieView movie, GFxValue root)
    {
        try
        {
            scaleform.Register(movie, root, functionName, ReportGold);
            Interlocked.Increment(ref installCount);
        }
        catch (InvalidOperationException ex)
        {
            log.Write($"Could not register {functionName} on a movie: {ex.Message}");
        }
    }

    // The game is stopped for the call, so this reads the player directly.
    private void ReportGold(GFxFunctionHandler.Params call)
    {
        Interlocked.Increment(ref calls);
        call.RetVal?.SetNumber(gameTarget.Player.GetGoldAmount());
    }
}
