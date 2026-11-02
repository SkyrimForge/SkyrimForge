using SkyrimForge.Game;

namespace SkyrimForge.Events;

// Called on any thread.
internal interface IKeyboardReader
{
    BSKeyboardDevice.Keys.Key? GetKeyDown(InputEventArgs args);

    ModifierKeys GetPressedModifierKeys();
}
