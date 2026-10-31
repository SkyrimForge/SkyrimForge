using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

// The package is placed in the actor's instance of the alias, which the game runs ahead of the actor's own packages.
// A package given with Actor.PutCreatedPackage ends as it completes, and the actor's own package then runs.
internal sealed class AliasPackages
{
    private readonly string questName;
    private readonly Func<QuestReferenceAlias, FormId<ObjectReference>?> getReference;
    private readonly Func<Actor, bool> isBusy;
    private readonly Action<QuestReferenceAlias, ObjectReference, Package?> setPackage;
    private readonly LinkedRefChanges linkedRefs;
    private readonly IGameSession session;
    private readonly ILog log;

    private readonly List<AliasPackage> packages = [];

    private readonly Dictionary<QuestReferenceAlias, AppliedPackage> appliedPackages = [];

    internal AliasPackages(string questName, Func<QuestReferenceAlias, FormId<ObjectReference>?> getReference, Func<Actor, bool> isBusy, Action<QuestReferenceAlias, ObjectReference, Package?> setPackage, LinkedRefChanges linkedRefs, IGameSession session, ILog log)
    {
        this.questName = questName;
        this.getReference = getReference;
        this.isBusy = isBusy;
        this.setPackage = setPackage;
        this.linkedRefs = linkedRefs;
        this.session = session;
        this.log = log;
    }

    internal ISubscription Add(QuestReferenceAlias alias, FormId<Package> packageId, Func<Actor, bool>? condition) => Add(new AliasPackage(alias, packageId, condition));

    internal ISubscription Add(QuestReferenceAlias alias, LinkedRefPackage package, Func<FormId<ObjectReference>?> getLinkedRefId, Func<Actor, bool>? condition) =>
        Add(new AliasPackage(alias, package, getLinkedRefId, condition));

    internal ISubscription AddTravelPackage(QuestReferenceAlias alias, Func<FormId<ObjectReference>?> getDestinationId, uint radius, Func<Actor, bool>? condition, PACKAGE_DATA.PreferredSpeed? speed) =>
        Add(new AliasPackage(alias, getDestinationId, radius, condition, new SessionValue<Package?>(session, () => CreateTravelPackage(alias, speed))));

    private PackageRegistration Add(AliasPackage aliasPackage)
    {
        packages.Add(aliasPackage);
        return new PackageRegistration(this, aliasPackage);
    }

    private void Remove(AliasPackage aliasPackage)
    {
        if (!packages.Remove(aliasPackage))
        {
            return;
        }
        linkedRefs.Release(aliasPackage);
        if (appliedPackages.GetValueOrDefault(aliasPackage.Alias)?.AliasPackage == aliasPackage)
        {
            RemoveAppliedPackage(aliasPackage.Alias);
        }
    }

    // A completed travel package stays completed after its location changes, so a travel package whose actor stands still outside its radius is restarted.
    internal void Run()
    {
        foreach (IGrouping<QuestReferenceAlias, AliasPackage> stack in packages.ToArray().GroupBy(aliasPackage => aliasPackage.Alias))
        {
            if (getReference(stack.Key)?.FormOrNull is not { } reference || reference.AsActor() is not { } actor || !actor.Is3DLoaded() || actor.IsDead() || isBusy(actor))
            {
                continue;
            }
            AliasPackage? selectedPackage = stack.FirstOrDefault(aliasPackage => Passes(aliasPackage, actor));
            foreach (AliasPackage other in stack.Where(aliasPackage => aliasPackage != selectedPackage))
            {
                linkedRefs.Release(other);
            }
            if (selectedPackage == null || Prepare(actor, selectedPackage) is not { } preparedPackage)
            {
                RemoveAppliedPackage(stack.Key);
                continue;
            }
            AppliedPackage? appliedPackage = appliedPackages.GetValueOrDefault(stack.Key);
            if (appliedPackage == null || appliedPackage.AliasPackage != selectedPackage || appliedPackage.ReferenceId != reference.FormId || appliedPackage.PackageId != preparedPackage.Package.FormId)
            {
                Place(reference, actor, selectedPackage, preparedPackage);
            }
            else if (preparedPackage.NewLinkTarget != null || preparedPackage.NearReference?.FormId != appliedPackage.NearReferenceId)
            {
                Restart(reference, actor, selectedPackage, preparedPackage, appliedPackage);
                log.Write($"The quest \"{questName}\" restarted {selectedPackage.Name} of its alias \"{selectedPackage.Alias.Name}\" on {actor.FormId}{Describe(preparedPackage)}.");
            }
            else if (IsStoppedOutsideRadius(actor, selectedPackage, preparedPackage, appliedPackage))
            {
                Restart(reference, actor, selectedPackage, preparedPackage, appliedPackage);
            }
        }
    }

    // A loaded game's alias instances have no packages.
    internal void ClearAppliedPackages() => appliedPackages.Clear();

    internal void ReleaseAlias(QuestReferenceAlias alias)
    {
        foreach (AliasPackage aliasPackage in packages.Where(aliasPackage => aliasPackage.Alias == alias))
        {
            linkedRefs.Release(aliasPackage);
        }
        RemoveAppliedPackage(alias);
    }

    private bool Passes(AliasPackage aliasPackage, Actor actor)
    {
        try
        {
            return aliasPackage.Condition?.Invoke(actor) != false && aliasPackage switch
            {
                { LinkedPackage: { } linkedPackage } => linkedPackage.PackageId.FormOrNull != null && (linkedPackage.KeywordId is not { } keywordId || keywordId.FormOrNull != null)
                    && aliasPackage.GetLinkedRefId!()?.FormOrNull != null,
                { PackageId: { } packageId } => packageId.FormOrNull != null,
                _ => aliasPackage.GetDestinationId!()?.FormOrNull != null
            };
        }
        catch (Exception ex)
        {
            log.Write($"The condition of {aliasPackage.Name} on the alias \"{aliasPackage.Alias.Name}\" of the quest \"{questName}\" threw: {ex}");
            return false;
        }
    }

    private PreparedPackage? Prepare(Actor actor, AliasPackage aliasPackage)
    {
        if (aliasPackage.PackageId is { } packageId)
        {
            return new PreparedPackage(packageId.Form, null, aliasPackage.LinkedPackage != null ? SetLink(actor, aliasPackage, aliasPackage.LinkedPackage) : null);
        }
        if (aliasPackage.GetDestinationId!() is not { } destinationId || destinationId.FormOrNull is not { } nearReference || aliasPackage.RuntimePackage!.Value is not { } travelPackage)
        {
            return null;
        }
        return new PreparedPackage(travelPackage, nearReference, null);
    }

    private void Place(ObjectReference reference, Actor actor, AliasPackage aliasPackage, PreparedPackage preparedPackage)
    {
        if (preparedPackage.NearReference is { } nearReference)
        {
            TravelPackages.SetLocation(preparedPackage.Package, nearReference, aliasPackage.Radius);
        }
        setPackage(aliasPackage.Alias, reference, preparedPackage.Package);
        actor.EvaluatePackage(true, false);
        appliedPackages[aliasPackage.Alias] = new AppliedPackage(aliasPackage, reference.FormId, preparedPackage.Package.FormId, preparedPackage.NearReference?.FormId, actor.GetPosition());
        log.Write($"The quest \"{questName}\" gave {actor.FormId} {aliasPackage.Name} of its alias \"{aliasPackage.Alias.Name}\"{Describe(preparedPackage)}.");
    }

    // Removing the package and placing it again in one frame, with the actor's packages evaluated after each, starts a completed package again.
    // Evaluating the actor's packages alone does not.
    private void Restart(ObjectReference reference, Actor actor, AliasPackage aliasPackage, PreparedPackage preparedPackage, AppliedPackage appliedPackage)
    {
        if (preparedPackage.NearReference is { } nearReference)
        {
            TravelPackages.SetLocation(preparedPackage.Package, nearReference, aliasPackage.Radius);
        }
        setPackage(aliasPackage.Alias, reference, null);
        actor.EvaluatePackage(true, false);
        setPackage(aliasPackage.Alias, reference, preparedPackage.Package);
        actor.EvaluatePackage(true, false);
        appliedPackage.NearReferenceId = preparedPackage.NearReference?.FormId;
        appliedPackage.StationaryCheckCount = 0;
    }

    private static bool IsStoppedOutsideRadius(Actor actor, AliasPackage aliasPackage, PreparedPackage preparedPackage, AppliedPackage appliedPackage)
    {
        const float restartMargin = 60f;
        const float stationaryDistance = 8f;
        const int requiredStationaryChecks = 4;
        if (preparedPackage.NearReference is not { } nearReference)
        {
            return false;
        }
        NiPoint3 position = actor.GetPosition();
        bool isStationary = position.GetDistance(appliedPackage.Position) < stationaryDistance;
        appliedPackage.Position = position;
        appliedPackage.StationaryCheckCount = isStationary && position.GetDistance(nearReference.GetPosition()) > aliasPackage.Radius + restartMargin && actor.GetCurrentPackage()?.FormId == appliedPackage.PackageId
            ? appliedPackage.StationaryCheckCount + 1
            : 0;
        return appliedPackage.StationaryCheckCount >= requiredStationaryChecks;
    }

    private void RemoveAppliedPackage(QuestReferenceAlias alias)
    {
        if (!appliedPackages.Remove(alias, out AppliedPackage? appliedPackage) || appliedPackage.ReferenceId.FormOrNull is not { } reference)
        {
            return;
        }
        setPackage(alias, reference, null);
        if (reference.AsActor() is { } actor && !actor.IsDead())
        {
            actor.EvaluatePackage(true, false);
        }
    }

    private static string Describe(PreparedPackage preparedPackage) =>
        preparedPackage.NearReference is { } nearReference ? $" near {nearReference.FormId}" : preparedPackage.NewLinkTarget is { } target ? $", with the actor's link set to {target.FormId}" : "";

    private ObjectReference? SetLink(Actor actor, AliasPackage aliasPackage, LinkedRefPackage linkedPackage)
    {
        if (aliasPackage.GetLinkedRefId!()?.FormOrNull is not { } target)
        {
            return null;
        }
        Keyword? keyword = linkedPackage.KeywordId?.Form;
        if (linkedRefs.HasLink(aliasPackage, actor, keyword, target))
        {
            return null;
        }
        linkedRefs.SetLink(aliasPackage, actor, keyword, target);
        return target;
    }

    private Package? CreateTravelPackage(QuestReferenceAlias alias, PACKAGE_DATA.PreferredSpeed? speed)
    {
        Package? package = TravelPackages.Create(speed);
        if (package == null)
        {
            log.Write($"The game created no travel package with a location for the alias \"{alias.Name}\" of the quest \"{questName}\", so none is given.");
        }
        return package;
    }

    // A class rather than a record, so removing one registration never removes an equal one.
    private sealed class AliasPackage
    {
        internal AliasPackage(QuestReferenceAlias alias, FormId<Package> packageId, Func<Actor, bool>? condition)
        {
            Alias = alias;
            PackageId = packageId;
            Condition = condition;
        }

        internal AliasPackage(QuestReferenceAlias alias, LinkedRefPackage linkedPackage, Func<FormId<ObjectReference>?> getLinkedRefId, Func<Actor, bool>? condition)
        {
            Alias = alias;
            PackageId = linkedPackage.PackageId;
            LinkedPackage = linkedPackage;
            GetLinkedRefId = getLinkedRefId;
            Condition = condition;
        }

        internal AliasPackage(QuestReferenceAlias alias, Func<FormId<ObjectReference>?> getDestinationId, uint radius, Func<Actor, bool>? condition, SessionValue<Package?> runtimePackage)
        {
            Alias = alias;
            GetDestinationId = getDestinationId;
            Radius = radius;
            Condition = condition;
            RuntimePackage = runtimePackage;
        }

        internal QuestReferenceAlias Alias { get; }

        internal Func<Actor, bool>? Condition { get; }

        internal FormId<Package>? PackageId { get; }

        internal LinkedRefPackage? LinkedPackage { get; }

        internal Func<FormId<ObjectReference>?>? GetLinkedRefId { get; }

        internal Func<FormId<ObjectReference>?>? GetDestinationId { get; }

        internal uint Radius { get; }

        internal SessionValue<Package?>? RuntimePackage { get; }

        internal string Name => PackageId is { } packageId ? $"the package {packageId}" : "the travel package";
    }

    private sealed record PreparedPackage(Package Package, ObjectReference? NearReference, ObjectReference? NewLinkTarget);

    private sealed class AppliedPackage(AliasPackage aliasPackage, FormId<ObjectReference> referenceId, FormId<Package> packageId, FormId<ObjectReference>? nearReferenceId, NiPoint3 position)
    {
        internal AliasPackage AliasPackage { get; } = aliasPackage;

        internal FormId<ObjectReference> ReferenceId { get; } = referenceId;

        internal FormId<Package> PackageId { get; } = packageId;

        internal FormId<ObjectReference>? NearReferenceId { get; set; } = nearReferenceId;

        internal NiPoint3 Position { get; set; } = position;

        internal int StationaryCheckCount { get; set; }
    }

    private sealed class PackageRegistration(AliasPackages aliasPackages, AliasPackage package) : ISubscription
    {
        public void Unsubscribe() => aliasPackages.Remove(package);
    }
}
