using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>An actor's animation events, which are the annotations on its animations.</summary>
/// <remarks>
/// These correspond to Papyrus's OnAnimationEvent.
/// A handler runs on the worker thread that updates the actor's animation, not on the game's thread, and handlers for different actors run at the same time.
/// A subscription to the player's events continues through a move to another cell and through a load.
/// A handler that throws, or whose task faults, is dropped, and the exception is written to the log of the plugin that subscribed it.
/// </remarks>
public interface IAnimationEvents
{
    /// <summary>Subscribes <paramref name="handler"/> to the animation events of <paramref name="actor"/>.</summary>
    /// <returns>Null for an actor with no animation graph, such as one outside the loaded area.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="handler"/> is <c>null</c>.</exception>
    ISubscription? Subscribe<TActor>(TActor actor, GameEventHandler<AnimationEventArgs> handler) where TActor : IActor;

    /// <inheritdoc cref="Subscribe{TActor}(TActor, GameEventHandler{AnimationEventArgs})"/>
    ISubscription? Subscribe<TActor>(TActor actor, AsyncGameEventHandler<AnimationEventArgs> handler) where TActor : IActor;
}
