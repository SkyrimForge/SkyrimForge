using SkyrimForge.Game;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Makes an actor walk to the player and start a conversation, using the game's force-greet package.</summary>
/// <remarks>
/// The actor is given the package again if it ends away from the player.
/// An actor the player cannot talk to plays a greeting instead of opening a conversation.
/// Call members on the game's thread.
/// </remarks>
public interface IForceGreet
{
    /// <summary>Makes <paramref name="actor"/> walk to the player and start a conversation.</summary>
    /// <returns>
    /// A task that completes when a conversation with the actor opens.
    /// It is canceled by <paramref name="cancellationToken"/>, or when a save starts loading or a new game begins.
    /// </returns>
    Task StartAsync<TActor>(TActor actor, CancellationToken cancellationToken = default) where TActor : IActor;
}
