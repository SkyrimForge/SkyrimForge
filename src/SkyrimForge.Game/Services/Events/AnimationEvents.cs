using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Game;

internal sealed class AnimationEvents(ILog log) : IAnimationEvents
{
    private readonly ILog log = log;

    // Replaced, never mutated, because the animation thread reads it without the lock.
    private static volatile Dictionary<nint, HandlerRegistration<GameEventHandler<AnimationEventArgs>>[]> registrations = [];
    private static readonly Lock registrationsLock = new();
    private static bool registered;

    public ISubscription? Subscribe<TActor>(TActor self, GameEventHandler<AnimationEventArgs> handler) where TActor : IActor
    {
        ArgumentNullException.ThrowIfNull(handler);
        return AddRegistration(self.Address, new HandlerRegistration<GameEventHandler<AnimationEventArgs>>(handler, log));
    }

    public ISubscription? Subscribe<TActor>(TActor self, AsyncGameEventHandler<AnimationEventArgs> handler) where TActor : IActor
    {
        ArgumentNullException.ThrowIfNull(handler);
        nint actorAddress = self.Address;
        HandlerRegistration<GameEventHandler<AnimationEventArgs>> registration = null!;
        registration = new(args => handler(args).OnFault(exception =>
        {
            RemoveRegistration(actorAddress, registration);
            log.Write($"An animation event handler's task faulted, and the handler is dropped: {exception}");
        }), log);
        return AddRegistration(actorAddress, registration);
    }

    private static unsafe ISubscription? AddRegistration(nint actorAddress, HandlerRegistration<GameEventHandler<AnimationEventArgs>> registration)
    {
        lock (registrationsLock)
        {
            if (!registered)
            {
                NativeSupplements.SF_Events_RegisterAnimationGraph(&OnAnimationEvent);
                registered = true;
            }
            Dictionary<nint, HandlerRegistration<GameEventHandler<AnimationEventArgs>>[]> map = registrations;
            if (!map.TryGetValue(actorAddress, out var existingRegistrations))
            {
                if (!NativeSupplements.SF_Actor_AddAnimationGraphEventSink(actorAddress))
                {
                    return null;
                }
                existingRegistrations = [];
            }
            registrations = new Dictionary<nint, HandlerRegistration<GameEventHandler<AnimationEventArgs>>[]>(map) { [actorAddress] = [.. existingRegistrations, registration] };
        }
        return new Subscription(actorAddress, registration);
    }

    [UnmanagedCallersOnly]
    private static void OnAnimationEvent(nint tag, nint payload, nint holder)
    {
        try
        {
            if (!registrations.TryGetValue(holder, out var actorRegistrations))
            {
                return;
            }
            var animationEvent = new AnimationEventArgs(Marshal.PtrToStringUTF8(tag) ?? string.Empty, Marshal.PtrToStringUTF8(payload) ?? string.Empty, Actor.OrNull(holder)!.Value);
            foreach (HandlerRegistration<GameEventHandler<AnimationEventArgs>> registration in actorRegistrations)
            {
                try
                {
                    registration.Handler(animationEvent);
                }
                catch (Exception ex)
                {
                    RemoveRegistration(holder, registration);
                    registration.Log.Write($"An animation event handler threw and was removed: {ex}");
                }
            }
        }
        catch (Exception) { } // An exception escaping an UnmanagedCallersOnly method ends the process.
    }

    private static void RemoveRegistration(nint actorAddress, HandlerRegistration<GameEventHandler<AnimationEventArgs>> registration)
    {
        bool last;
        lock (registrationsLock)
        {
            Dictionary<nint, HandlerRegistration<GameEventHandler<AnimationEventArgs>>[]> map = registrations;
            if (!map.TryGetValue(actorAddress, out var actorRegistrations))
            {
                return;
            }
            List<HandlerRegistration<GameEventHandler<AnimationEventArgs>>> remaining = [.. actorRegistrations];
            if (!remaining.Remove(registration))
            {
                return;
            }
            var next = new Dictionary<nint, HandlerRegistration<GameEventHandler<AnimationEventArgs>>[]>(map);
            last = remaining.Count == 0;
            if (last)
            {
                next.Remove(actorAddress);
            }
            else
            {
                next[actorAddress] = [.. remaining];
            }
            registrations = next;
        }
        if (last)
        {
            NativeSupplements.SF_Actor_RemoveAnimationGraphEventSink(actorAddress);
        }
    }

    private sealed class Subscription(nint actorAddress, HandlerRegistration<GameEventHandler<AnimationEventArgs>> registration) : ISubscription
    {
        private readonly nint actorAddress = actorAddress;
        private readonly HandlerRegistration<GameEventHandler<AnimationEventArgs>> registration = registration;

        public void Unsubscribe() => RemoveRegistration(actorAddress, registration);
    }
}

internal static unsafe partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_Events_RegisterAnimationGraph(delegate* unmanaged<nint, nint, nint, void> handler);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Actor_AddAnimationGraphEventSink(nint self);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Actor_RemoveAnimationGraphEventSink(nint self);
}
