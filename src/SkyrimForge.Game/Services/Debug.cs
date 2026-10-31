using System;

namespace SkyrimForge.Game;

internal sealed class Debug : IDebug
{
    public void MessageBox(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        GlobalFunctions.DebugMessageBox(message);
    }

    public void Notification(string message, string? sound, bool dropIfQueued)
    {
        ArgumentNullException.ThrowIfNull(message);
        GlobalFunctions.SendHUDMessage.ShowHUDMessage(message, sound, dropIfQueued);
    }
}
