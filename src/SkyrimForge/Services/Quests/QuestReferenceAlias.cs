using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>A named placeholder for a reference, equivalent to a Creation Kit reference alias.</summary>
/// <param name="name">
/// The alias's name, unique among the quest's aliases of both kinds and the same in every version of the plugin.
/// The saved state stores the reference under it.
/// </param>
/// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
public sealed class QuestReferenceAlias(string name) : QuestAlias(name)
{
    /// <summary>The alias every quest fills with the player.</summary>
    /// <remarks>It belongs to no quest. Each member that acts on a quest throws, apart from <see cref="ReferenceId"/>.</remarks>
    public static QuestReferenceAlias Player { get; } = new("Player");

    private IQuestReferenceAliasEvents? events;

    /// <summary>The name a reference shows once it fills the alias. Null shows the reference's own name.</summary>
    /// <remarks>The game saves the name with the reference, which retains it after the alias is cleared.</remarks>
    public string? DisplayName { get; init; }

    /// <summary>Whether an actor filling the alias is essential, entering bleedout rather than dying.</summary>
    public bool IsEssential { get; init; }

    /// <summary>Whether an actor filling the alias is protected, dying only to the player.</summary>
    public bool IsProtected { get; init; }

    /// <summary>Whether an item filling the alias is a quest item, which the player cannot drop or sell.</summary>
    /// <remarks>The flag applies from the time the quest displays its first objective until the item leaves the alias.</remarks>
    public bool IsQuestObject { get; init; }

    /// <summary>The rank an actor filling the alias is given in each faction. A null rank removes the actor from the faction.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyDictionary<FormId<Faction>, sbyte?> Factions { get; init => field = new Dictionary<FormId<Faction>, sbyte?>(value ?? throw new ArgumentNullException(nameof(value))); } = new Dictionary<FormId<Faction>, sbyte?>();

    /// <summary>The base value an actor filling the alias is given for each actor value, such as Aggression 0.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyDictionary<ActorValue, float> ActorValues { get; init => field = new Dictionary<ActorValue, float>(value ?? throw new ArgumentNullException(nameof(value))); } = new Dictionary<ActorValue, float>();

    /// <summary>Fills the alias with the reference <paramref name="referenceId"/> identifies.</summary>
    /// <remarks>A reference the game does not have shows no marker, and is logged while an open objective targets the alias.</remarks>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public void Fill<TReference>(FormId<TReference> referenceId) where TReference : struct, IObjectReference => QuestAliases.Fill(this, referenceId);

    /// <summary>Creates an actor of the base <paramref name="actorBaseId"/> at <paramref name="position"/> and fills the alias with it.</summary>
    /// <param name="actorBaseId">The actor base, which may be built on a template.</param>
    /// <param name="position">The position, in the player's worldspace or, in an interior, the player's cell.</param>
    /// <param name="persistent">
    /// Whether the actor is persistent. While the actor's cell is unloaded, the game returns no form for <see cref="ReferenceId"/> unless the actor is persistent.
    /// The quest never deletes an actor it creates, so a persistent actor remains in the save.
    /// </param>
    /// <remarks>Before the 3D loads, <c>SetOutfit</c> takes effect but <c>EquipItem</c> does not, so call <c>EquipItem</c> after <c>WaitFor3DLoadAsync</c>.</remarks>
    /// <exception cref="InvalidOperationException">
    /// The alias belongs to no quest, no form has <paramref name="actorBaseId"/>, the form is not an actor base, or the game created no marker or no actor.
    /// </exception>
    public Actor CreateActor(FormId<ActorBase> actorBaseId, NiPoint3 position, bool persistent = false) => QuestAliases.CreateActor(this, actorBaseId, position, persistent);

    /// <summary>Creates an actor and fills the alias with it, as <see cref="CreateActor"/> does, then sets its outfit and weapons once its 3D loads.</summary>
    /// <param name="actorBaseId">The actor base, which may be built on a template.</param>
    /// <param name="position">The position, in the player's worldspace or, in an interior, the player's cell.</param>
    /// <param name="outfitId">The outfit the actor wears in place of its own, or null to leave its own.</param>
    /// <param name="weaponIds">
    /// The weapons that replace every weapon the actor has, the first of them equipped. An empty list leaves the actor unarmed, and null leaves its weapons.
    /// </param>
    /// <param name="persistent">Whether the actor is persistent, as for <see cref="CreateActor"/>.</param>
    /// <param name="cancellationToken">Cancels the outfit and weapon changes. A new game or a save load also cancels them.</param>
    /// <exception cref="InvalidOperationException">
    /// The alias belongs to no quest. The exception is stored in the returned task instead if no form has <paramref name="actorBaseId"/>,
    /// the form is not an actor base, or the game created no marker or no actor.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// The actor's 3D did not load within ten seconds, which happens when its cell is not loaded. The actor remains in the alias. The exception is stored in the returned task.
    /// </exception>
    public Task<Actor> CreateActorAsync(FormId<ActorBase> actorBaseId, NiPoint3 position, FormId<Outfit>? outfitId = null, IEnumerable<FormId<Weapon>>? weaponIds = null,
        bool persistent = false, CancellationToken cancellationToken = default) =>
        QuestAliases.CreateActorAsync(this, actorBaseId, position, outfitId, weaponIds is null ? null : [.. weaponIds], persistent, cancellationToken);

    /// <summary>Creates an XMarker at <paramref name="position"/> and fills the alias with it.</summary>
    /// <remarks>The quest deletes the marker once the alias is filled again or cleared, or once <see cref="IRuntimeQuest.RemoveFromJournalAndReset"/> runs.</remarks>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest, or the game created no marker.</exception>
    public ObjectReference CreateMarker(NiPoint3 position) => QuestAliases.CreateMarker(this, position);

    /// <summary>
    /// Disables the reference filling the alias once it is <paramref name="distance"/> units or more from the player, or once <paramref name="timeout"/>
    /// of unpaused time has passed, and then empties the alias.
    /// </summary>
    /// <returns>
    /// A task that completes once the alias is emptied, immediately for an empty alias.
    /// A new game, a load, or <paramref name="cancellationToken"/> cancels the task and leaves the alias filled.
    /// </returns>
    /// <remarks>The reference is disabled rather than deleted, since deleting an actor the dialogue system still references ends the process.</remarks>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public Task DisableAndClearAsync(float distance, TimeSpan timeout, CancellationToken cancellationToken = default) => QuestAliases.DisableAndClearAsync(this, distance, timeout, cancellationToken);

    /// <summary>Empties the alias.</summary>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public void Clear() => QuestAliases.Clear(this);

    /// <summary>
    /// Gets the form ID of the reference that fills the alias.
    /// An empty alias returns null, and <see cref="Player"/> returns the form ID of the player's reference.
    /// </summary>
    /// <exception cref="InvalidOperationException">The alias is not <see cref="Player"/> and belongs to no quest.</exception>
    public FormId<ObjectReference>? ReferenceId => this == Player ? KnownForms.PlayerRef.Cast<ObjectReference>() : QuestAliases.GetReference(this);

    /// <summary>The game's events about the reference that fills the alias.</summary>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public IQuestReferenceAliasEvents Events => events ??= QuestAliases.GetEvents(this);

    /// <summary>Gives the actor filling the alias the AI package <paramref name="packageId"/> while <paramref name="condition"/> returns true.</summary>
    /// <param name="packageId">The package, whose record specifies its targets.</param>
    /// <param name="condition">Returns whether the actor it receives runs the package. Null runs it whenever the alias is filled.</param>
    /// <remarks>
    /// The packages added to one alias form a stack, evaluated on the game's thread while the actor is loaded and alive.
    /// The actor runs the first added whose condition passes, ahead of its own packages, and returns to its own once none passes.
    /// A completed package remains the actor's package.
    /// The stack is not evaluated while the actor speaks in an open conversation or is an actor of a scene one of this plugin's quests plays.
    /// A condition that throws is logged and counts as failing.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public ISubscription AddPackage(FormId<Package> packageId, Func<Actor, bool>? condition = null) => QuestAliases.AddPackage(this, packageId, condition);

    /// <summary>
    /// Gives the actor filling the alias a package whose location or target is the actor's linked reference,
    /// and points that link at the reference <paramref name="getLinkedRefId"/> returns.
    /// </summary>
    /// <param name="package">The package record and the keyword of its link, such as <see cref="LinkedRefPackage.Sandbox"/> or <see cref="LinkedRefPackage.Sit"/>.</param>
    /// <param name="getLinkedRefId">Returns the reference the link is set to. Null counts as the condition failing.</param>
    /// <param name="condition">Returns whether the actor it receives runs the package. Null runs it whenever the alias is filled.</param>
    /// <remarks>
    /// The package joins the stack <see cref="AddPackage(FormId{Package}, Func{Actor, bool}?)"/> describes,
    /// and restarts whenever the reference <paramref name="getLinkedRefId"/> returns changes.
    /// The actor's original link is restored once the package stops running.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="package"/> or <paramref name="getLinkedRefId"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public ISubscription AddPackage(LinkedRefPackage package, Func<FormId<ObjectReference>?> getLinkedRefId, Func<Actor, bool>? condition = null) =>
        QuestAliases.AddPackage(this, package, getLinkedRefId, condition);

    /// <summary>Gives the actor filling the alias a travel package whose location is near the reference <paramref name="getDestinationId"/> returns.</summary>
    /// <param name="getDestinationId">Returns the reference the package's location is near. Null counts as the condition failing.</param>
    /// <param name="radius">The distance from the reference, in units, that counts as at the location.</param>
    /// <param name="condition">Returns whether the actor it receives runs the package. Null runs it whenever the alias is filled.</param>
    /// <param name="speed">The speed the actor moves at. Null selects the package's default.</param>
    /// <remarks>
    /// The package joins the stack <see cref="AddPackage(FormId{Package}, Func{Actor, bool}?)"/> describes.
    /// It restarts whenever the reference <paramref name="getDestinationId"/> returns changes, and when the actor has stopped short of the reference.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="getDestinationId"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The alias belongs to no quest.</exception>
    public ISubscription AddTravelPackage(Func<FormId<ObjectReference>?> getDestinationId, uint radius, Func<Actor, bool>? condition = null, PACKAGE_DATA.PreferredSpeed? speed = null) =>
        QuestAliases.AddTravelPackage(this, getDestinationId, radius, condition, speed);
}
