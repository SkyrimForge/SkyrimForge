using System;

namespace SkyrimForge.Infrastructure;

internal sealed class GameConsole : IConsole
{
    /// <inheritdoc/>
    public void Write(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        NativeGlobals.SF_ConsoleLog_Print(message);
    }
}
