using SkyrimForge.Papyrus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class RuntimeQuestAliases(IRuntimeQuest quest, RuntimeQuestFactory factory, GameTarget gameTarget, IGameEvents events, IUtility utility, IGameSession session, SavedQuestStore store, RuntimeQuestRecord record, AliasPackages packages, AliasActorState actorState, QuestDeclaration declaration) : IQuestAliases
{
    private static readonly TimeSpan distanceCheckInterval = TimeSpan.FromSeconds(1);

    private readonly RuntimeQuestFactory factory = factory;
    private readonly GameTarget gameTarget = gameTarget;
    private readonly IGameEvents events = events;
    private readonly IUtility utility = utility;
    private readonly IGameSession session = session;
    private readonly SavedQuestStore store = store;
    private readonly RuntimeQuestRecord record = record;
    private readonly AliasActorState actorState = actorState;
    private readonly QuestDeclaration declaration = declaration;

    public IRuntimeQuest Quest { get; } = quest;

    internal AliasPackages Packages { get; } = packages;

    public IQuestReferenceAliasEvents GetEvents(QuestReferenceAlias alias) => new QuestReferenceAliasEvents(events, () => GetReference(alias));

    public ISubscription AddPackage(QuestReferenceAlias alias, FormId<Package> packageId, Func<Actor, bool>? condition) =>
        RunPackages(Packages.Add(alias, packageId, condition));

    public ISubscription AddPackage(QuestReferenceAlias alias, LinkedRefPackage package, Func<FormId<ObjectReference>?> getLinkedRefId, Func<Actor, bool>? condition)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(getLinkedRefId);
        return RunPackages(Packages.Add(alias, package, getLinkedRefId, condition));
    }

    public ISubscription AddTravelPackage(QuestReferenceAlias alias, Func<FormId<ObjectReference>?> getDestinationId, uint radius, Func<Actor, bool>? condition, PACKAGE_DATA.PreferredSpeed? speed)
    {
        ArgumentNullException.ThrowIfNull(getDestinationId);
        return RunPackages(Packages.AddTravelPackage(alias, getDestinationId, radius, condition, speed));
    }

    private ISubscription RunPackages(ISubscription registration)
    {
        factory.StartAliasPackageRuns();
        return registration;
    }

    private void ReleaseAlias(QuestReferenceAlias alias, FormId<ObjectReference>? nextReferenceId)
    {
        FormId<ObjectReference>? referenceId = GetReference(alias);
        // A runtime alias object cannot be emptied, so its last reference stays a quest item until its instance is removed.
        if (alias.IsQuestObject && referenceId != nextReferenceId && referenceId?.FormOrNull is { } reference)
        {
            record.RemoveAliasInstance(alias, reference);
        }
        if (store.Read().Aliases.GetValueOrDefault(alias.Name)?.HasCreatedMarker == true && referenceId?.FormOrNull is { } marker)
        {
            marker.DisableAndDelete();
        }
        actorState.Apply(alias, referenceId, false);
        Packages.ReleaseAlias(alias);
    }

    public void Fill<TReference>(QuestReferenceAlias alias, FormId<TReference> referenceId) where TReference : struct, IObjectReference =>
        Fill(alias, referenceId.Cast<ObjectReference>(), false);

    private void Fill(QuestReferenceAlias alias, FormId<ObjectReference> referenceId, bool createdMarker)
    {
        ReleaseAlias(alias, referenceId);
        SetAlias(alias, new SavedAlias(referenceId, createdMarker));
        if (alias.DisplayName != null)
        {
            referenceId.FormOrNull?.SetDisplayName(alias.DisplayName, true);
        }
        actorState.Apply(alias, referenceId, true);
    }

    public Actor CreateActor(QuestReferenceAlias alias, FormId<ActorBase> actorBaseId, NiPoint3 position, bool persistent)
    {
        Actor actor = gameTarget.Player.PlaceActor(gameTarget.DataHandler, actorBaseId, position, persistent: persistent);
        Fill(alias, actor.FormId);
        return actor;
    }

    public async Task<Actor> CreateActorAsync(QuestReferenceAlias alias, FormId<ActorBase> actorBaseId, NiPoint3 position, FormId<Outfit>? outfitId, IReadOnlyList<FormId<Weapon>>? weaponIds,
        bool persistent, CancellationToken cancellationToken)
    {
        Actor actor = CreateActor(alias, actorBaseId, position, persistent);
        TimeSpan loadTimeout = TimeSpan.FromSeconds(10);
        TimeSpan equipTime = TimeSpan.FromSeconds(2);
        using (CancellationTokenSource stopSource = LinkToGame(cancellationToken))
        {
            await actor.WaitFor3DLoadAsync(utility, loadTimeout, stopSource.Token);
            if (outfitId is { } outfit)
            {
                actor.SetOutfit(outfit.Form);
            }
            if (weaponIds == null)
            {
                return actor;
            }
            await utility.WaitAsync(equipTime, stopSource.Token);
            List<(TESBoundObject Weapon, int Count)> heldWeapons;
            using (DisposableGameList<KeyValuePair<TESBoundObject, (int First, InventoryEntryData Second)>> inventory = actor.GetInventory())
            {
                heldWeapons = [.. inventory.Where(item => item.Key.AsWeapon() != null && item.Value.First > 0).Select(item => (item.Key, item.Value.First))];
            }
            foreach ((TESBoundObject heldWeapon, int count) in heldWeapons)
            {
                actor.RemoveItem(heldWeapon, count, ITEM_REMOVE_REASON.kRemove);
            }
            foreach (FormId<Weapon> weaponId in weaponIds)
            {
                actor.AddObjectToContainer(weaponId.Form, null, 1);
            }
            if (weaponIds.Count > 0)
            {
                gameTarget.ActorEquipManager.EquipObject(actor, weaponIds[0].Form, forceEquip: true, playSounds: false);
            }
            return actor;
        }
    }

    public ObjectReference CreateMarker(QuestReferenceAlias alias, NiPoint3 position)
    {
        ObjectReference marker = gameTarget.Player.Place(gameTarget.DataHandler, KnownForms.XMarker, position);
        Fill(alias, marker.FormId, true);
        return marker;
    }

    public async Task DisableAndClearAsync(QuestReferenceAlias alias, float distance, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using (CancellationTokenSource stopSource = LinkToGame(cancellationToken))
        {
            stopSource.Token.ThrowIfCancellationRequested();
            if (GetReference(alias) is not { } referenceId)
            {
                return;
            }
            for (TimeSpan elapsed = TimeSpan.Zero; elapsed < timeout && referenceId.FormOrNull?.GetDistance(gameTarget.Player) < distance; elapsed += distanceCheckInterval)
            {
                await utility.WaitAsync(distanceCheckInterval, stopSource.Token);
            }
            if (referenceId.FormOrNull is { } reference)
            {
                await reference.Papyrus.Disable(true);
            }
            stopSource.Token.ThrowIfCancellationRequested();
            if (GetReference(alias) == referenceId)
            {
                Clear(alias);
            }
        }
    }

    // Linked to the game the call begins in, so a reference created in one game never fills or empties an alias in the next.
    private CancellationTokenSource LinkToGame(CancellationToken cancellationToken) => CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.NewGameOrSaveLoading);

    public void Clear(QuestReferenceAlias alias)
    {
        ReleaseAlias(alias, null);
        SetAlias(alias, null);
    }

    private void SetAlias(QuestReferenceAlias alias, SavedAlias? savedAlias)
    {
        SavedQuest savedQuest = store.Read();
        Dictionary<string, SavedAlias> aliases = new(savedQuest.Aliases);
        if (savedAlias != null)
        {
            aliases[alias.Name] = savedAlias;
        }
        else
        {
            aliases.Remove(alias.Name);
        }
        Write(savedQuest with { Aliases = aliases });
    }

    public void Fill(QuestLocationAlias alias, FormId<Location> locationId) => SetLocation(alias, locationId);

    public void Clear(QuestLocationAlias alias) => SetLocation(alias, null);

    private void SetLocation(QuestLocationAlias alias, FormId<Location>? locationId)
    {
        SavedQuest savedQuest = store.Read();
        Dictionary<string, FormId<Location>> locationAliasIds = new(savedQuest.LocationAliases);
        if (locationId is { } id)
        {
            locationAliasIds[alias.Name] = id;
        }
        else
        {
            locationAliasIds.Remove(alias.Name);
        }
        Write(savedQuest with { LocationAliases = locationAliasIds });
    }

    public FormId<Location>? GetLocationId(QuestLocationAlias alias) => store.GetLocationId(alias);

    public IQuestLocationAliasEvents GetEvents(QuestLocationAlias alias) => new QuestLocationAliasEvents(gameTarget, events, () => GetLocationId(alias));

    // A load restores an actor's flags as its base describes them.
    internal void ApplyActorStates()
    {
        foreach (QuestReferenceAlias alias in declaration.Aliases)
        {
            actorState.Apply(alias, GetReference(alias), true);
        }
    }

    public FormId<ObjectReference>? GetReference(QuestReferenceAlias alias) => store.GetReference(alias);

    internal void ReleaseAllAliases()
    {
        foreach (QuestReferenceAlias alias in declaration.Aliases)
        {
            ReleaseAlias(alias, null);
        }
    }

    private void Write(SavedQuest savedQuest)
    {
        store.Write(savedQuest);
        record.Apply(false);
    }
}
