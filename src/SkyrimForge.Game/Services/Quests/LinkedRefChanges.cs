using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

// A save records the changed links, so the plugin's saved state stores each change's original reference, and a load restores those links.
internal sealed class LinkedRefChanges
{
    private readonly ISavedStateRecordPair<SavedLinks> linksPair;
    private readonly Dictionary<Link, Change> changes = [];

    internal LinkedRefChanges(IGameEvents events, IPerPluginSavedState savedState)
    {
        linksPair = savedState.GetRecordPair<SavedLinks>("SkyrimForge.LinkedRefs");
        events.OnNewGameOrSaveLoading.Subscribe(_ => changes.Clear());
        events.OnSaveLoaded.Subscribe(_ => RestoreSaved());
    }

    private readonly record struct Link(FormId<Actor> ActorId, FormId<Keyword>? KeywordId);

    private sealed record Change(object Owner, FormId<ObjectReference>? OriginalTargetId, FormId<ObjectReference> TargetId);

    private sealed record SavedLinks(IReadOnlyList<SavedLink> Links);

    private sealed record SavedLink(FormId<Actor> ActorId, FormId<Keyword>? KeywordId, FormId<ObjectReference>? OriginalTargetId);

    internal bool HasLink(object owner, Actor actor, Keyword? keyword, ObjectReference target) =>
        changes.TryGetValue(new Link(actor.FormId, keyword?.FormId), out Change? change) && change.Owner == owner && change.TargetId == target.FormId;

    internal void SetLink(object owner, Actor actor, Keyword? keyword, ObjectReference target)
    {
        Link link = new(actor.FormId, keyword?.FormId);
        FormId<ObjectReference>? originalTargetId = changes.TryGetValue(link, out Change? change) ? change.OriginalTargetId : actor.ExtraList.GetLinkedRef(keyword)?.FormId;
        actor.ExtraList.SetLinkedRef(target, keyword);
        changes[link] = new Change(owner, originalTargetId, target.FormId);
        Store();
    }

    internal void Release(object owner)
    {
        KeyValuePair<Link, Change>[] releasedChanges = [.. changes.Where(entry => entry.Value.Owner == owner)];
        if (releasedChanges.Length == 0)
        {
            return;
        }
        foreach ((Link link, Change change) in releasedChanges)
        {
            changes.Remove(link);
            Restore(link, change.OriginalTargetId);
        }
        Store();
    }

    private void Store() => linksPair.Value = changes.Count > 0 ? new SavedLinks([.. changes.Select(entry => new SavedLink(entry.Key.ActorId, entry.Key.KeywordId, entry.Value.OriginalTargetId))]) : null;

    private void RestoreSaved()
    {
        if (linksPair.Value is not { } savedLinks)
        {
            return;
        }
        foreach (SavedLink link in savedLinks.Links)
        {
            Restore(new Link(link.ActorId, link.KeywordId), link.OriginalTargetId);
        }
        linksPair.Value = null;
    }

    private static void Restore(Link link, FormId<ObjectReference>? originalTargetId)
    {
        Keyword? keyword = link.KeywordId?.FormOrNull;
        if (link.ActorId.FormOrNull is { } actor && (link.KeywordId == null || keyword != null))
        {
            actor.ExtraList.SetLinkedRef(originalTargetId?.FormOrNull, keyword);
        }
    }
}
