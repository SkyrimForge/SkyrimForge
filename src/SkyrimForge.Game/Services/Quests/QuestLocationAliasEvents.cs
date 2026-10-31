using System;

namespace SkyrimForge.Game;

internal sealed class QuestLocationAliasEvents(GameTarget gameTarget, IGameEvents events, Func<FormId<Location>?> getLocationId) : IQuestLocationAliasEvents
{
    public IGameEvent<ActorLocationChangeEventArgs> OnEnter { get; } = new FilteredGameEvent<ActorLocationChangeEventArgs>(events.OnActorLocationChange,
        args => args.Actor?.IsPlayerRef() == true && Contains(getLocationId, args.NewLocation) && !Contains(getLocationId, args.OldLocation));

    public IGameEvent<ActorLocationChangeEventArgs> OnLeave { get; } = new FilteredGameEvent<ActorLocationChangeEventArgs>(events.OnActorLocationChange,
        args => args.Actor?.IsPlayerRef() == true && Contains(getLocationId, args.OldLocation) && !Contains(getLocationId, args.NewLocation));

    public IGameEvent<LocationClearedEventArgs> OnCleared { get; } = new FilteredGameEvent<LocationClearedEventArgs>(events.OnLocationCleared,
        _ => getLocationId()?.FormOrNull is { } filledLocation && filledLocation.IsCleared() && Contains(getLocationId, gameTarget.Player.GetCurrentLocation()));

    private static bool Contains(Func<FormId<Location>?> getLocationId, Location? other) =>
        other is { } candidate && getLocationId()?.FormOrNull is { } filledLocation && (candidate.FormId == filledLocation.FormId || filledLocation.IsChild(candidate));
}
