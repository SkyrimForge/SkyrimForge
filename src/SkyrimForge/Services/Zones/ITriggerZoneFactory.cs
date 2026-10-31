using SkyrimForge.Game;
using System;

namespace SkyrimForge;

/// <summary>Creates zones around references that raise events as the player enters and leaves them.</summary>
/// <remarks>
/// The zones are checked on the game's thread each quarter second of unpaused time while a game and the player are loaded.
/// The player is inside a zone while within its radius of the center, measured in three dimensions, and in the center's worldspace or interior cell.
/// Call members on the game's thread.
/// </remarks>
public interface ITriggerZoneFactory
{
    /// <summary>Creates a zone of <paramref name="radius"/> units around the reference <paramref name="getCenterId"/> returns.</summary>
    /// <param name="key">The saved-state name of the zone, unique among this plugin's zones and the same in every version of the plugin.</param>
    /// <param name="getCenterId">Called at each check. A null result leaves the player outside, and an exception ends the zone's checks.</param>
    /// <param name="radius">The radius, in game units.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is zero or negative.</exception>
    /// <exception cref="ArgumentException">This plugin created a zone with <paramref name="key"/> already.</exception>
    /// <remarks>
    /// The inside state is stored in this plugin's saved state, so a save loaded with the player inside raises no <see cref="ITriggerZone.OnEnter"/>.
    /// Callable from a plugin's start, before the game has data or a save.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="getCenterId"/> is <c>null</c>.</exception>
    ITriggerZone Create(string key, Func<FormId<ObjectReference>?> getCenterId, float radius);
}
