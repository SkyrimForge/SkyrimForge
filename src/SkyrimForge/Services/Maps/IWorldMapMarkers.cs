using SkyrimForge.Game;
using System;

namespace SkyrimForge;

/// <summary>Adds markers to the world map at runtime, each a place the player can fast travel to.</summary>
/// <remarks>Each marker is a reference the game saves. Call members on the game's thread with a game loaded.</remarks>
public interface IWorldMapMarkers
{
    /// <summary>
    /// Adds a marker named <paramref name="name"/> with the icon <paramref name="kind"/> at <paramref name="position"/> in the worldspace with <paramref name="worldspaceId"/>.
    /// <paramref name="key"/> is unique among this plugin's markers and the same in every version of the plugin.
    /// </summary>
    /// <returns>The marker's reference. Its parent cell is null while the cell containing the position is not loaded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="name"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">No worldspace has <paramref name="worldspaceId"/>, or this plugin added a marker with <paramref name="key"/> already.</exception>
    /// <exception cref="InvalidOperationException">The game created no reference or set no marker on it.</exception>
    ObjectReference Add(string key, FormId<WorldSpace> worldspaceId, NiPoint3 position, string name, MARKER_TYPE kind);

    /// <summary>Returns the reference ID of the marker with <paramref name="key"/>, or null if this plugin added no marker with that key.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    FormId<ObjectReference>? GetReferenceId(string key);

    /// <summary>Removes the marker with <paramref name="key"/> from the world map and deletes its reference. A key with no marker has no effect.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    void Remove(string key);
}
