using SkyrimForge.Game;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Logs each key the player presses with the control bound to it, and on each load logs the key bound to Activate.</summary>
/// <remarks>
/// <c>ButtonEventIfDown</c> is set only for the first event of a press. It is null for later events of the same press, for the release, and for input that is not a button.
/// </remarks>
public sealed class InputPlugin(GameTarget gameTarget, IGameEvents events, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnInput.Subscribe(Report);
        events.OnSaveLoaded.Subscribe(ReadMap);
        log.Write("Logging key presses.");
    }

    private void ReadMap(SaveLoadedEventArgs args)
    {
        const INPUT_DEVICES.INPUT_DEVICE keyboard = INPUT_DEVICES.INPUT_DEVICE.kKeyboard;
        const UserEvents.INPUT_CONTEXT_IDS.INPUT_CONTEXT_ID gameplayContext = UserEvents.INPUT_CONTEXT_IDS.INPUT_CONTEXT_ID.kGameplay;
        uint key = gameTarget.ControlMap.GetMappedKey("Activate", keyboard, gameplayContext);
        log.Write($"Activate is bound to key {key}.");
    }

    private void Report(InputEventArgs args)
    {
        if (args.ButtonEventIfDown is ButtonEvent buttonEvent)
        {
            string? control = buttonEvent.QUserEvent();
            log.Write($"Pressed {buttonEvent.GetDevice()} code {buttonEvent.GetIDCode()}, bound to " + (string.IsNullOrEmpty(control) ? "nothing." : $"\"{control}\"."));
        }
    }
}
