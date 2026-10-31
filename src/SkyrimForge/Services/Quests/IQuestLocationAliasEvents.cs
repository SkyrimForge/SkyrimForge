namespace SkyrimForge;

/// <summary>The game's events about the player and the location a <see cref="QuestLocationAlias"/> is filled with.</summary>
/// <remarks>
/// A location inside that location, as Dragonsreach is inside Whiterun, counts as that location.
/// Each event is an <see cref="IGameEvents"/> event filtered to the alias, and its handlers run on the thread that raises that event.
/// </remarks>
public interface IQuestLocationAliasEvents
{
    /// <summary>Raised when the player moves into the location from outside it.</summary>
    IGameEvent<ActorLocationChangeEventArgs> OnEnter { get; }

    /// <summary>Raised when the player moves out of the location.</summary>
    IGameEvent<ActorLocationChangeEventArgs> OnLeave { get; }

    /// <summary>Raised when the game reports a location cleared while the alias's location is cleared and the player is in it.</summary>
    IGameEvent<LocationClearedEventArgs> OnCleared { get; }
}
