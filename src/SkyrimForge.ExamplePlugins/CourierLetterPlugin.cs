using SkyrimForge.Game;
using System.Net;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Sends the player a letter by the game's courier when F1 is pressed.</summary>
public sealed class CourierLetterPlugin(GameTarget gameTarget, IGameEvents events, IReadableNotes readableNotes, IDebug debug, IUtility utility, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF1, ModifierKeys.None).Subscribe(_ => SendAsync());
        readableNotes.OnRead.Subscribe(args => log.Write($"The player read the letter {args.Note.FormId}."));
        log.Write("F1 sends the player a letter by courier.");
    }

    private async Task SendAsync()
    {
        await utility.WaitForGameThreadAsync();
        string text = $"This letter was written at hour {(int)gameTarget.Calendar.TimeOfDay.TotalHours}, for a traveler of level {gameTarget.Player.GetLevel()}.";
        ObjectReference letter = await readableNotes.SendByCourierAsync("A Letter by Courier", [WebUtility.HtmlEncode(text)]);
        log.Write($"Sent letter {letter.FormId} by courier.");
        debug.Notification("A letter is with the courier.");
    }
}
