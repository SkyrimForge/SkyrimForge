using SkyrimForge.Game;
using System;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Sets and dismisses the player's follower and animal through the game's DialogueFollower quest (000750BA).</summary>
/// <remarks>
/// The player has at most one follower and one animal.
/// The game's follow, wait, and dismissal dialogue appears only for voice types that have those lines.
/// Call members on the game's thread.
/// </remarks>
public interface IFollowers
{
    /// <summary>The actor in the Follower alias, or null if the alias is empty or its actor is dead or deleted.</summary>
    /// <exception cref="InvalidOperationException">The quest has no Follower alias.</exception>
    Actor? Follower { get; }

    /// <summary>The actor in the Animal alias, or null if the alias is empty or its actor is dead or deleted.</summary>
    /// <exception cref="InvalidOperationException">The quest has no Animal alias.</exception>
    Actor? Animal { get; }

    /// <summary>Makes <paramref name="actor"/> the follower, a player teammate in the Follower alias.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Follower"/> is another actor. The exception is stored in the returned task.</exception>
    Task SetFollowerAsync<TActor>(TActor actor) where TActor : IActor;

    /// <summary>Dismisses the follower, shows the game's dismissal message, and has the follower say its dismissal line. Does nothing if <see cref="Follower"/> is null.</summary>
    Task DismissFollowerAsync();

    /// <summary>Makes <paramref name="actor"/> the animal, a player teammate in the Animal alias that does no favors and has Lockpicking 0.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Animal"/> is another actor. The exception is stored in the returned task.</exception>
    Task SetAnimalAsync<TActor>(TActor actor) where TActor : IActor;

    /// <summary>Dismisses the animal and shows the game's dismissal message. Does nothing if <see cref="Animal"/> is null.</summary>
    Task DismissAnimalAsync();
}
