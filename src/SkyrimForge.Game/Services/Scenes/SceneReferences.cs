using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class SceneReferences(ISceneOwner owner)
{
    internal static readonly FormId<ObjectReference> Player = new(KnownForms.PlayerRef.Value);

    private readonly ISceneOwner owner = owner;

    internal bool IsPlayer(QuestReferenceAlias alias) => owner.GetReference(alias)?.Value == Player.Value;

    // The game ends a scene missing an actor that is only looked at.
    // An actor that is not loaded runs its travel package by appearing in the player's cell without walking there.
    internal string? GetUnavailabilityReason(QuestScene scene, int fromPhase, bool loadedRequired = true)
    {
        SceneAction[] actions = [.. scene.GetActions(fromPhase).Select(recordAction => recordAction.Action)];
        HashSet<QuestReferenceAlias> optionalAliases = [.. scene.ActorSettings.Where(settings => settings.IsOptional).Select(settings => settings.Actor)];
        if (actions.SelectMany(action => action.Actors).Distinct().Where(alias => !optionalAliases.Contains(alias))
            .Select(alias => GetActorUnavailabilityReason(alias, loadedRequired)).WhereNotNull().FirstOrDefault() is { } unavailabilityReason)
        {
            return unavailabilityReason;
        }
        if (actions.OfType<ScenePackage>().Select(action => action.LinkedRef).WhereNotNull().FirstOrDefault(alias => owner.GetReference(alias)?.FormOrNull == null) is { } unlinkedAlias)
        {
            return $"the alias \"{unlinkedAlias.Name}\" a package's link points at is empty";
        }
        return actions.OfType<ScenePackage>().FirstOrDefault(action => action.Travels && GetDestination(action) == null) switch
        {
            { Destination: { } alias } => $"the alias \"{alias.Name}\" a travel package travels to is empty",
            { LocationDestination: { } location } => $"the location alias \"{location.Name}\" a travel package travels to is empty, or its location has no world location marker",
            _ => null
        };
    }

    internal ObjectReference? GetDestination(ScenePackage action) => action switch
    {
        { Destination: { } alias } => owner.GetReference(alias)?.FormOrNull,
        { LocationDestination: { } location } => owner.GetLocationMarker(location)?.FormOrNull,
        _ => Player.FormOrNull
    };

    private string? GetActorUnavailabilityReason(QuestReferenceAlias alias, bool loadedRequired)
    {
        if (owner.GetReference(alias)?.FormOrNull?.AsActor() is { } actor && !actor.IsDead() && !actor.IsDisabled() && !actor.IsDeleted())
        {
            return !loadedRequired || actor.Is3DLoaded() ? null : $"the actor of the alias \"{alias.Name}\" is not loaded";
        }
        return $"the alias \"{alias.Name}\" is empty, or its actor is dead, disabled, or deleted";
    }

    internal QuestReferenceAlias? GetActorInCombat(QuestScene scene, ScenePlay play) => scene.GetActors(play.FirstPhase).FirstOrDefault(alias =>
        (scene.ActorSettings.FirstOrDefault(listedSettings => listedSettings.Actor == alias) ?? new SceneActorSettings(alias)).CombatInterruption == SceneInterruption.End
        && owner.GetReference(alias)?.FormOrNull?.AsActor()?.IsInCombat() == true);
}
