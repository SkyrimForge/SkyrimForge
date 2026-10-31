using System.Collections.Generic;

namespace SkyrimForge.Game;

// A null rank means the actor was not in the faction.
internal sealed record SavedActorAlias(IReadOnlyDictionary<FormId<Faction>, sbyte?> FactionRanks, IReadOnlyDictionary<ActorValue, float> ActorValues);
