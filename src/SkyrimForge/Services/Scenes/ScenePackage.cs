using SkyrimForge.Game;
using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>An AI package the actor filling an alias runs in a <see cref="ScenePhase"/>.</summary>
/// <remarks>
/// A package that does not complete, such as a sandbox package,
/// leaves the phase it ends in playing until the scene is stopped or the phase's <see cref="ScenePhase.EndsWhen"/> returns true.
/// <para>
/// The player performs a package only while AI-driven. A scene with a package action whose alias the player fills sets that state as it begins to play and clears it as it ends.
/// The state is one flag, so clearing it clears it for another mod that set it.
/// A travel action whose alias the player fills ignores the radius, and two such actions running in one phase share the destination of the one listed last.
/// </para>
/// </remarks>
public sealed record ScenePackage : SceneAction
{
    /// <summary>Creates an action that runs package records.</summary>
    /// <param name="actor">The alias whose actor runs the packages.</param>
    /// <param name="packageIds">The package records, in the order of the Creation Kit's package list.</param>
    /// <exception cref="ArgumentException"><paramref name="packageIds"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> or <paramref name="packageIds"/> is <c>null</c>.</exception>
    public ScenePackage(QuestReferenceAlias actor, params IEnumerable<FormId<Package>> packageIds) : base(actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(packageIds);
        Actor = actor;
        PackageIds = [.. packageIds];
        if (PackageIds.Count == 0)
        {
            throw new ArgumentException($"A package action of the alias \"{actor.Name}\" has no package.", nameof(packageIds));
        }
    }

    /// <summary>
    /// Creates an action that runs a package record whose location or target is the actor's linked reference,
    /// with that link set to the reference filling <paramref name="linkedRef"/>.
    /// </summary>
    /// <remarks>The link is set to the reference from the scene's start until it ends, and is then restored to the reference it was set to before.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/>, <paramref name="package"/>, or <paramref name="linkedRef"/> is <c>null</c>.</exception>
    public ScenePackage(QuestReferenceAlias actor, LinkedRefPackage package, QuestReferenceAlias linkedRef) : this(actor, (package ?? throw new ArgumentNullException(nameof(package))).PackageId)
    {
        ArgumentNullException.ThrowIfNull(linkedRef);
        LinkedPackage = package;
        LinkedRef = linkedRef;
    }

    private ScenePackage(QuestReferenceAlias actor, QuestReferenceAlias? destination, QuestLocationAlias? locationDestination, uint radius, PACKAGE_DATA.PreferredSpeed? speed)
        : base(actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        Actor = actor;
        Destination = destination;
        LocationDestination = locationDestination;
        Radius = radius;
        Speed = speed;
    }

    /// <summary>
    /// Creates an action that runs a travel package whose location is within <paramref name="radius"/> units of the reference filling <paramref name="destination"/>.
    /// A null <paramref name="speed"/> selects the package's default.
    /// </summary>
    /// <remarks>
    /// An actor does not reliably reach a load door as the destination.
    /// To travel to a position, fill the destination with a marker <see cref="QuestReferenceAlias.CreateMarker"/> creates.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> or <paramref name="destination"/> is <c>null</c>.</exception>
    public static ScenePackage Travel(QuestReferenceAlias actor, QuestReferenceAlias destination, uint radius, PACKAGE_DATA.PreferredSpeed? speed = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return new(actor, destination, null, radius, speed);
    }

    /// <summary>
    /// Creates an action that runs a travel package,
    /// whose location is within <paramref name="radius"/> units of the world location marker of the location filling <paramref name="destination"/>.
    /// A null <paramref name="speed"/> selects the package's default.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> or <paramref name="destination"/> is <c>null</c>.</exception>
    public static ScenePackage Travel(QuestReferenceAlias actor, QuestLocationAlias destination, uint radius, PACKAGE_DATA.PreferredSpeed? speed = null)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return new(actor, null, destination, radius, speed);
    }

    /// <summary>
    /// Creates an action that runs a travel package whose location is within <paramref name="radius"/> units of the player.
    /// A null <paramref name="speed"/> selects the package's default.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> is <c>null</c>.</exception>
    public static ScenePackage TravelToPlayer(QuestReferenceAlias actor, uint radius, PACKAGE_DATA.PreferredSpeed? speed = null) => new(actor, null, null, radius, speed);

    /// <summary>The alias whose actor runs the package.</summary>
    public QuestReferenceAlias Actor { get; }

    /// <summary>The package records. A travel package has none.</summary>
    public IReadOnlyList<FormId<Package>> PackageIds { get; } = [];

    /// <summary>The package record whose location or target is a linked reference, with the keyword of that link. The value is null for another package.</summary>
    public LinkedRefPackage? LinkedPackage { get; }

    /// <summary>The alias whose reference the link of <see cref="LinkedPackage"/> is set to. The value is null for another package.</summary>
    public QuestReferenceAlias? LinkedRef { get; }

    /// <summary>The alias whose reference a travel package's actor travels to. The value is null for a package record or another destination.</summary>
    public QuestReferenceAlias? Destination { get; }

    /// <summary>
    /// The location alias whose location's world location marker a travel package's actor travels to. The value is null for a package record or another destination.
    /// </summary>
    public QuestLocationAlias? LocationDestination { get; }

    /// <summary>The distance from the destination at which a travel package's actor has arrived.</summary>
    public uint Radius { get; }

    /// <summary>The speed a travel package's actor travels at. A null value selects the package's default.</summary>
    public PACKAGE_DATA.PreferredSpeed? Speed { get; }

    internal bool Travels => PackageIds.Count == 0;

    internal override IEnumerable<QuestReferenceAlias> Aliases => (Destination ?? LinkedRef) is { } otherAlias ? [Actor, otherAlias] : [Actor];

    internal override IEnumerable<QuestLocationAlias> LocationAliases => LocationDestination != null ? [LocationDestination] : [];
}
