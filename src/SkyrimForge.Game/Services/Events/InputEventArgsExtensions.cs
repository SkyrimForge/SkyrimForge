using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="InputEventArgs"/>.</summary>
public static class InputEventArgsExtensions
{
    extension(InputEventArgs args)
    {
        /// <summary>Whether the event is the keyboard key <paramref name="key"/> going down.</summary>
        public bool IsKeyDown(BSKeyboardDevice.Keys.Key key) =>
            args.ButtonEventIfDown is { } buttonEvent && buttonEvent.GetDevice() == INPUT_DEVICES.INPUT_DEVICE.kKeyboard && buttonEvent.GetIDCode() == (uint)key;

        /// <summary>The <see cref="ButtonEvent"/> if this event is the first of a button press, otherwise null.</summary>
        public ButtonEvent? ButtonEventIfDown => args.InputEvent.AsButtonEvent() is { } buttonEvent && buttonEvent.IsDown() ? buttonEvent : null;
    }
}
