using System;
using System.Collections.Generic;
using System.Threading;

namespace SkyrimForge;

/// <summary>An <see cref="IGameEvent{TArgs}"/> with a <see cref="Raise"/> method for the code that raises the event.</summary>
public sealed class GameEvent<TArgs> : IGameEvent<TArgs> where TArgs : struct
{
    private readonly Action<Exception> reportFailure;
    private readonly Lock handlersLock;

    // Replaced, never mutated, and read by Raise without the lock.
    private volatile List<GameEventHandler<TArgs>> handlers;

    /// <summary>Creates an event with no subscribers.</summary>
    /// <param name="reportFailure">Receives the exception from a subscriber that threw. That subscriber is dropped first, so it is reported once.</param>
    /// <exception cref="ArgumentNullException"><paramref name="reportFailure"/> is <c>null</c>.</exception>
    public GameEvent(Action<Exception> reportFailure)
    {
        ArgumentNullException.ThrowIfNull(reportFailure);
        this.reportFailure = reportFailure;
        handlersLock = new Lock();
        handlers = [];
    }

    /// <inheritdoc/>
    public ISubscription Subscribe(GameEventHandler<TArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (handlersLock)
        {
            handlers = [.. handlers, handler];
        }
        return new Subscription(this, handler);
    }

    /// <inheritdoc/>
    public ISubscription Subscribe(AsyncGameEventHandler<TArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        GameEventHandler<TArgs> subscriber = null!;
        subscriber = args => handler(args).OnFault(exception =>
        {
            Remove(subscriber);
            reportFailure(exception);
        });
        return Subscribe(subscriber);
    }

    internal bool HasSubscribers => handlers.Count > 0;

    /// <summary>Invokes every current subscriber. A subscriber that throws is removed and reported, and the subscribers after it still run.</summary>
    public void Raise(TArgs args)
    {
        List<GameEventHandler<TArgs>> subscribers = handlers;
        foreach (GameEventHandler<TArgs> handler in subscribers)
        {
            try
            {
                handler(args);
            }
            catch (Exception ex)
            {
                Remove(handler);
                reportFailure(ex);
            }
        }
    }

    private void Remove(GameEventHandler<TArgs> handler)
    {
        lock (handlersLock)
        {
            List<GameEventHandler<TArgs>> remaining = [.. handlers];

            // One occurrence only, since the same handler can subscribe twice.
            if (!remaining.Remove(handler))
            {
                return;
            }

            handlers = remaining;
        }
    }

    private sealed class Subscription(GameEvent<TArgs> owner, GameEventHandler<TArgs> handler) : ISubscription
    {
        private readonly GameEvent<TArgs> owner = owner;
        private readonly GameEventHandler<TArgs> handler = handler;
        private int unsubscribed;

        public void Unsubscribe()
        {
            // A second call would otherwise remove another subscription of the same handler.
            if (Interlocked.Exchange(ref unsubscribed, 1) == 0)
            {
                owner.Remove(handler);
            }
        }
    }
}
