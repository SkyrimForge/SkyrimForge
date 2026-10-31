using System;
using System.Collections.Concurrent;

namespace SkyrimForge.Papyrus;

internal sealed class PapyrusEvents : IPapyrusEvents
{
    // Keys are upper case because Papyrus compares names without regard to case.
    private static readonly ConcurrentDictionary<(string? ScriptName, string EventName), PapyrusEvent> declaredEvents = new();

    public PapyrusEvent Declare(string eventName)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        return declaredEvents.GetOrAdd((null, eventName.ToUpperInvariant()), _ => CreateEvent(null, eventName));
    }

    public PapyrusEvent Declare(string scriptName, string eventName)
    {
        ArgumentException.ThrowIfNullOrEmpty(scriptName);
        ArgumentException.ThrowIfNullOrEmpty(eventName);
        return declaredEvents.GetOrAdd((scriptName.ToUpperInvariant(), eventName.ToUpperInvariant()), _ => CreateEvent(scriptName, eventName));
    }

    private static PapyrusEvent CreateEvent(string? scriptName, string eventName)
    {
        nint registrationSet = NativePapyrus.SF_PapyrusEvents_Declare(eventName);
        if (registrationSet == 0)
        {
            throw new InvalidOperationException($"No registration set could be made for \"{eventName}\".");
        }
        return new PapyrusEvent(eventName, scriptName, registrationSet);
    }
}
