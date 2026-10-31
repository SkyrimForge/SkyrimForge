using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkyrimForge.Infrastructure;

internal sealed unsafe class SksePluginMessenger(ILog log) : ISksePluginMessenger
{
    // SKSE accepts one registration per sender.
    private static readonly Dictionary<string, List<HandlerRegistration<Action<SksePluginMessage>>>> registrationsBySender = new(StringComparer.OrdinalIgnoreCase);

    // Installed on the first subscription, so a test that constructs a plugin needs no native runtime.
    private static bool handlerInstalled;

    private readonly ILog log = log;

    /// <inheritdoc/>
    public bool Send(uint messageType, nint data = 0, int length = 0, string? receiver = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        return NativeGlobals.SF_Messaging_Dispatch(messageType, data, (uint)length, receiver);
    }

    /// <inheritdoc/>
    public ISubscription Subscribe(string sender, Action<SksePluginMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(handler);
        if (string.Equals(sender, "SKSE", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("SKSE delivers its own messages only to the listener SkyrimForge registers, so this subscription would never run. IGameEvents raises an event for each of them.", nameof(sender));
        }
        var registration = new HandlerRegistration<Action<SksePluginMessage>>(handler, log);
        lock (registrationsBySender)
        {
            if (!handlerInstalled)
            {
                NativeGlobals.SF_Messaging_SetHandler(&OnMessage);
                handlerInstalled = true;
            }
            if (!registrationsBySender.TryGetValue(sender, out List<HandlerRegistration<Action<SksePluginMessage>>>? registrations))
            {
                if (!NativeGlobals.SF_Messaging_Listen(sender))
                {
                    throw new InvalidOperationException($"SKSE rejected the registration for messages from {sender}.");
                }
                registrations = [];
                registrationsBySender.Add(sender, registrations);
            }
            registrations.Add(registration);
        }
        return new Subscription(sender, registration);
    }

    [UnmanagedCallersOnly]
    private static void OnMessage(uint messageType, nint data, uint length, nint sender)
    {
        string name = sender != 0 ? Marshal.PtrToStringUTF8(sender) ?? string.Empty : string.Empty;
        HandlerRegistration<Action<SksePluginMessage>>[] registrations;
        lock (registrationsBySender)
        {
            if (!registrationsBySender.TryGetValue(name, out List<HandlerRegistration<Action<SksePluginMessage>>>? listeners))
            {
                return;
            }
            // Copied, so a handler may subscribe or unsubscribe during delivery.
            registrations = [.. listeners];
        }

        var message = new SksePluginMessage(name, messageType, data, (int)length);
        foreach (HandlerRegistration<Action<SksePluginMessage>> registration in registrations)
        {
            try
            {
                registration.Handler(message);
            }
            catch (Exception ex)
            {
                Drop(name, registration);
                registration.Log.Write($"A handler for messages from {name} threw and was unsubscribed: {ex}");
            }
        }
    }

    private static void Drop(string sender, HandlerRegistration<Action<SksePluginMessage>> registration)
    {
        lock (registrationsBySender)
        {
            if (registrationsBySender.TryGetValue(sender, out List<HandlerRegistration<Action<SksePluginMessage>>>? registrations))
            {
                registrations.Remove(registration);
            }
        }
    }

    // SKSE cannot remove a registration, so unsubscribing removes only the handler.
    private sealed class Subscription(string sender, HandlerRegistration<Action<SksePluginMessage>> registration) : ISubscription
    {
        private readonly string sender = sender;
        private readonly HandlerRegistration<Action<SksePluginMessage>> registration = registration;

        /// <inheritdoc/>
        public void Unsubscribe() => Drop(sender, registration);
    }
}
