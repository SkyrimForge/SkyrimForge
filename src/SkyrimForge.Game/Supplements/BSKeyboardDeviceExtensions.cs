namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="IBSKeyboardDevice"/>.</summary>
public static class BSKeyboardDeviceExtensions
{
    extension<T>(T keyboard) where T : IBSKeyboardDevice
    {
        /// <summary>Returns whether <paramref name="key"/> is down.</summary>
        public bool IsPressed(BSKeyboardDevice.Keys.Key key) => keyboard.IsPressed((uint)key);
    }
}
