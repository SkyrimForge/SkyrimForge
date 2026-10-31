using SkyrimForge.Game;
using System;

namespace SkyrimForge;

/// <summary>A named placeholder for a location, equivalent to a Creation Kit location alias.</summary>
/// <param name="name">
/// The alias's name, unique among the quest's aliases of both kinds and the same in every version of the plugin.
/// The saved state stores the location under it.
/// </param>
/// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
public sealed class QuestLocationAlias(string name) : QuestAlias(name)
{
    private IQuestLocationAliasEvents? events;

    /// <summary>Fills the alias with the location <paramref name="locationId"/> identifies.</summary>
    /// <remarks>
    /// Objectives and a <see cref="ScenePackage"/> destination use the location's world location marker.
    /// A location the game does not have, or one with no world location marker, shows no marker, and an open objective logs it.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public void Fill(FormId<Location> locationId) => QuestAliases.Fill(this, locationId);

    /// <summary>Empties the alias.</summary>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public void Clear() => QuestAliases.Clear(this);

    /// <summary>Gets the location the alias is filled with in the loaded game. The result is null for an empty alias.</summary>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public FormId<Location>? LocationId => QuestAliases.GetLocationId(this);

    /// <summary>The game's events about the player and the location the alias is filled with.</summary>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public IQuestLocationAliasEvents Events => events ??= QuestAliases.GetEvents(this);
}
