using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Adds the console command SkyrimForgeButtons, which shows a message box with a button per argument and writes the pressed button to the console.</summary>
public sealed class ConsoleCommandPlugin(IConsoleCommandRegistrar consoleCommandRegistrar, IMessageBoxPrompter messageBox, IGameSession session, IConsole console, ILog log) : IPlugin
{
    private const string
        commandName = "SkyrimForgeButtons",
        shortName = "sfbuttons";

    /// <inheritdoc/>
    public void Start()
    {
        consoleCommandRegistrar.Register(commandName, shortName, "Shows a message box with one button per argument, up to five.",
            ["Button1", "Button2", "Button3", "Button4", "Button5"], ShowButtonsAsync);
        log.Write($"Enter \"{commandName} First Second Third\" in the console to show a message box with three buttons.");
    }

    private async Task ShowButtonsAsync(ObjectReference? target, IReadOnlyList<string> arguments)
    {
        IReadOnlyList<string> buttons = arguments.Count > 0 ? arguments : ["OK"];
        string reference = target is { } targetReference ? $"{targetReference.FormId}" : "no reference";
        Report($"{commandName} ran on {reference} with {arguments.Count} argument(s){(arguments.Count > 0 ? $": {string.Join(", ", arguments)}" : "")}.");
        try
        {
            int pressedIndex = await messageBox.ShowAsync($"{commandName} received {arguments.Count} argument(s).", buttons, session.NewGameOrSaveLoading);
            Report($"{commandName}: button {pressedIndex} (\"{buttons.ElementAtOrDefault(pressedIndex)}\") was pressed.");
        }
        catch (OperationCanceledException)
        {
            Report($"{commandName}: no button was pressed before the game closed the message box or a new game or a save started loading.");
        }
        catch (InvalidOperationException ex)
        {
            Report($"{commandName}: the message box was not shown: {ex.Message}");
        }
    }

    private void Report(string message)
    {
        console.Write(message);
        log.Write(message);
    }
}
