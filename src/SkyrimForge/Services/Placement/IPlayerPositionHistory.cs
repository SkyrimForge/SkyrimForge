using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>The positions the player has stood on in the loaded game, and the search for a position near the player to place an actor.</summary>
/// <remarks>
/// Positions are recorded while the player stands on the ground outdoors. A load or a new game empties the record. Call members on the game's thread.
/// </remarks>
public interface IPlayerPositionHistory
{
    /// <summary>The player's position on the ground, or the mount's while riding. It is null while the player is in the air or swimming.</summary>
    NiPoint3? GroundedPosition { get; }

    /// <summary>Waits up to <paramref name="timeout"/> of unpaused time for <see cref="GroundedPosition"/>.</summary>
    /// <returns>A task whose result is the position, or null if the player is not on the ground by then. A new game or a save load cancels the task.</returns>
    Task<NiPoint3?> WaitForGroundedPositionAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a position more than <paramref name="minimumDistance"/> and less than <paramref name="maximumDistance"/> units from the player,
    /// on ground an actor can stand on. <paramref name="candidates"/> are positions in the player's worldspace on which an actor can stand.
    /// </summary>
    /// <returns>
    /// The latest recorded position in range, then the first candidate in range, then the ground under a map marker in range,
    /// then the ground behind the player midway between the two distances, if that ground is in range, or null if none of them is found.
    /// </returns>
    SpawnPosition? FindSpawnPosition(float minimumDistance, float maximumDistance, IEnumerable<NiPoint3>? candidates = null);
}
