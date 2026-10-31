using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class AliasActorState(IPerPluginSavedState savedState, string key)
{
    private readonly IPerPluginSavedState savedState = savedState;
    private readonly string key = key;

    internal void Apply(QuestReferenceAlias alias, FormId<ObjectReference>? referenceId, bool inAlias)
    {
        if (!(alias.IsEssential || alias.IsProtected || alias.Factions.Count > 0 || alias.ActorValues.Count > 0) || referenceId?.FormOrNull?.AsActor() is not { } actor)
        {
            return;
        }
        SetFlags(alias, actor, inAlias);
        ISavedStateRecordPair<Dictionary<string, SavedActorAlias>> aliasesPair = savedState.GetFormEntries(actor.FormId).GetRecordPair<Dictionary<string, SavedActorAlias>>(key);
        Dictionary<string, SavedActorAlias> savedActorAliases = aliasesPair.Value ?? [];
        if (inAlias)
        {
            // Stored once, as the actor fills the alias, so applying the alias again after a load does not store the alias's ranks and values in their place.
            if (!savedActorAliases.ContainsKey(alias.Name))
            {
                savedActorAliases.Add(alias.Name, GetRanksAndValuesBeforeAlias(alias, actor));
                aliasesPair.Value = savedActorAliases;
            }
            SetRanksAndValues(actor, alias.Factions, alias.ActorValues);
        }
        else if (savedActorAliases.Remove(alias.Name, out SavedActorAlias? savedActorAlias))
        {
            SetRanksAndValues(actor, savedActorAlias.FactionRanks, savedActorAlias.ActorValues);
            aliasesPair.Value = savedActorAliases.Count > 0 ? savedActorAliases : null;
        }
    }

    private static SavedActorAlias GetRanksAndValuesBeforeAlias(QuestReferenceAlias alias, Actor actor)
    {
        Dictionary<FormId<Faction>, sbyte?> ranks = [];
        foreach (FormId<Faction> factionId in alias.Factions.Keys)
        {
            if (factionId.FormOrNull is { } faction)
            {
                ranks.Add(factionId, actor.IsInFaction(faction) ? (sbyte)actor.GetFactionRank(faction) : null);
            }
        }
        ActorValueOwner? actorValues = actor.AsActorValueOwner();
        Dictionary<ActorValue, float> values = actorValues is { } owner ? alias.ActorValues.Keys.ToDictionary(actorValue => actorValue, actorValue => owner.GetBaseActorValue(actorValue)) : [];
        return new SavedActorAlias(ranks, values);
    }

    private static void SetRanksAndValues(Actor actor, IReadOnlyDictionary<FormId<Faction>, sbyte?> ranks, IReadOnlyDictionary<ActorValue, float> values)
    {
        foreach ((FormId<Faction> factionId, sbyte? rank) in ranks)
        {
            if (factionId.FormOrNull is not { } faction)
            {
                continue;
            }
            if (rank is { } factionRank)
            {
                actor.AddToFaction(faction, factionRank);
            }
            else
            {
                actor.RemoveFromFaction(faction);
            }
        }
        if (actor.AsActorValueOwner() is { } actorValues)
        {
            foreach ((ActorValue actorValue, float value) in values)
            {
                actorValues.SetBaseActorValue(actorValue, value);
            }
        }
    }

    private static void SetFlags(QuestReferenceAlias alias, Actor actor, bool inAlias)
    {
        if (!(alias.IsEssential || alias.IsProtected))
        {
            return;
        }
        TESActorBaseData? baseData = actor.GetActorBase()?.AsTESActorBaseData();
        Actor.ACTOR_RUNTIME_DATA runtimeData = actor.ActorRuntimeData;
        Actor.BOOL_FLAGS flags = runtimeData.BoolFlags & ~(Actor.BOOL_FLAGS.kEssential | Actor.BOOL_FLAGS.kProtected);
        if ((alias.IsEssential && inAlias) || baseData?.IsEssential() == true)
        {
            flags |= Actor.BOOL_FLAGS.kEssential;
        }
        if ((alias.IsProtected && inAlias) || baseData?.IsProtected() == true)
        {
            flags |= Actor.BOOL_FLAGS.kProtected;
        }
        runtimeData.BoolFlags = flags;
    }
}
