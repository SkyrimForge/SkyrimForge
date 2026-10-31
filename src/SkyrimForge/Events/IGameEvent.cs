namespace SkyrimForge;

/// <summary>A game event to which a plugin can subscribe.</summary>
/// <remarks>
/// Members can be called on any thread.
/// A game object in an event's arguments, such as an <c>Actor</c>, addresses the game's memory, so read it while the handler runs.
/// To use a form later, store its <c>FormId</c> and look it up again.
/// </remarks>
public interface IGameEvent<TArgs> where TArgs : struct
{
    /// <summary>Registers <paramref name="handler"/>.</summary>
    /// <remarks>A handler that throws is dropped and not called again, even after a later load. The exception is written to its plugin's log.</remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="handler"/> is <c>null</c>.</exception>
    ISubscription Subscribe(GameEventHandler<TArgs> handler);

    /// <summary>Registers <paramref name="handler"/>, whose task the event does not await.</summary>
    /// <remarks>
    /// A later raise can call the handler again while an earlier task is pending.
    /// A handler that throws, or whose task faults, is dropped and not called again, even after a later load. The exception is written to its plugin's log.
    /// A canceled task does not drop the handler.
    /// </remarks>
    /// <inheritdoc cref="Subscribe(GameEventHandler{TArgs})" path="/exception"/>
    ISubscription Subscribe(AsyncGameEventHandler<TArgs> handler);
}
