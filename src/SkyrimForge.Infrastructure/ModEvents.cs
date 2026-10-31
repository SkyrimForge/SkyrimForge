using SkyrimForge.Game;
using System;

namespace SkyrimForge.Infrastructure;

internal sealed class ModEvents : IModEvents
{
    /// <inheritdoc/>
    public void Send(string eventName, string stringArgument = "", float numberArgument = 0f, IForm? sender = null)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        ArgumentNullException.ThrowIfNull(stringArgument);
        nint senderAddress = sender?.Address ?? 0;
        NativeGlobals.SF_Messaging_SendModEvent(eventName, stringArgument, numberArgument, senderAddress);
    }
}
