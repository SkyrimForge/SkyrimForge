using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

internal interface IQuestAliases
{
    IRuntimeQuest Quest { get; }

    void Fill<TReference>(QuestReferenceAlias alias, FormId<TReference> referenceId) where TReference : struct, IObjectReference;

    Actor CreateActor(QuestReferenceAlias alias, FormId<ActorBase> actorBaseId, NiPoint3 position, bool persistent);

    Task<Actor> CreateActorAsync(QuestReferenceAlias alias, FormId<ActorBase> actorBaseId, NiPoint3 position, FormId<Outfit>? outfitId, IReadOnlyList<FormId<Weapon>>? weaponIds, bool persistent,
        CancellationToken cancellationToken);

    ObjectReference CreateMarker(QuestReferenceAlias alias, NiPoint3 position);

    Task DisableAndClearAsync(QuestReferenceAlias alias, float distance, TimeSpan timeout, CancellationToken cancellationToken);

    void Clear(QuestReferenceAlias alias);

    FormId<ObjectReference>? GetReference(QuestReferenceAlias alias);

    IQuestReferenceAliasEvents GetEvents(QuestReferenceAlias alias);

    void Fill(QuestLocationAlias alias, FormId<Location> locationId);

    void Clear(QuestLocationAlias alias);

    FormId<Location>? GetLocationId(QuestLocationAlias alias);

    IQuestLocationAliasEvents GetEvents(QuestLocationAlias alias);

    ISubscription AddPackage(QuestReferenceAlias alias, FormId<Package> packageId, Func<Actor, bool>? condition);

    ISubscription AddPackage(QuestReferenceAlias alias, LinkedRefPackage package, Func<FormId<ObjectReference>?> getLinkedRefId, Func<Actor, bool>? condition);

    ISubscription AddTravelPackage(QuestReferenceAlias alias, Func<FormId<ObjectReference>?> getDestinationId, uint radius, Func<Actor, bool>? condition, PACKAGE_DATA.PreferredSpeed? speed);
}
