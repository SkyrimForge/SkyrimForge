namespace SkyrimForge.Game;

internal sealed class KeyboardReader(GameTarget gameTarget) : IKeyboardReader
{
    public BSKeyboardDevice.Keys.Key? GetKeyDown(InputEventArgs args) =>
        args.ButtonEventIfDown is { } buttonEvent && buttonEvent.GetDevice() == INPUT_DEVICES.INPUT_DEVICE.kKeyboard ? (BSKeyboardDevice.Keys.Key)buttonEvent.GetIDCode() : null;

    public ModifierKeys GetPressedModifierKeys()
    {
        ModifierKeys modifiers = ModifierKeys.None;
        if (gameTarget.InputDeviceManager.GetKeyboard() is { } keyboard)
        {
            if (keyboard.IsPressed(BSKeyboardDevice.Keys.Key.kLeftShift) || keyboard.IsPressed(BSKeyboardDevice.Keys.Key.kRightShift))
            {
                modifiers |= ModifierKeys.Shift;
            }
            if (keyboard.IsPressed(BSKeyboardDevice.Keys.Key.kLeftControl) || keyboard.IsPressed(BSKeyboardDevice.Keys.Key.kRightControl))
            {
                modifiers |= ModifierKeys.Control;
            }
            if (keyboard.IsPressed(BSKeyboardDevice.Keys.Key.kLeftAlt) || keyboard.IsPressed(BSKeyboardDevice.Keys.Key.kRightAlt))
            {
                modifiers |= ModifierKeys.Alt;
            }
        }
        return modifiers;
    }
}
