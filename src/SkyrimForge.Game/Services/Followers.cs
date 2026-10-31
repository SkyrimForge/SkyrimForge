using SkyrimForge.Papyrus;
using System;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class Followers : IFollowers
{
    private const string followerAlias = "Follower", animalAlias = "Animal";

    private static readonly PapyrusScriptDispatcher script = new("DialogueFollowerScript");

    public Actor? Follower => GetAliasActor(followerAlias);

    public Actor? Animal => GetAliasActor(animalAlias);

    public async Task SetFollowerAsync<TActor>(TActor actor) where TActor : IActor
    {
        ThrowIfOtherActor(Follower, actor, followerAlias);
        await script.CallAsync(Quest, "SetFollower", new PapyrusArguments().Add(actor));
    }

    public async Task DismissFollowerAsync()
    {
        if (Follower != null)
        {
            // DismissFollower(iMessage, iSayLine): message 0 is the standard dismissal message, and 1 plays the dismissal line.
            await script.CallAsync(Quest, "DismissFollower", new PapyrusArguments().Add(0).Add(1));
        }
    }

    public async Task SetAnimalAsync<TActor>(TActor actor) where TActor : IActor
    {
        ThrowIfOtherActor(Animal, actor, animalAlias);
        await script.CallAsync(Quest, "SetAnimal", new PapyrusArguments().Add(actor));
    }

    public async Task DismissAnimalAsync()
    {
        if (Animal != null)
        {
            await script.CallAsync(Quest, "DismissAnimal");
        }
    }

    private static Quest Quest => KnownForms.DialogueFollower.Form;

    private static Actor? GetAliasActor(string aliasName)
    {
        Quest quest = Quest;
        Alias alias = quest.Aliases.Get().FirstOrNull(entry => entry.AliasName == aliasName)
            ?? throw new InvalidOperationException($"The DialogueFollower quest has no {aliasName} alias.");
        // A dead or deleted actor is excluded. The dismiss functions skip a dead actor, so it would otherwise occupy the alias indefinitely.
        return quest.GetAliasedRef(alias.AliasID)?.AsActor() is { } aliasedActor && !aliasedActor.IsDead() && !aliasedActor.IsDeleted() ? aliasedActor : null;
    }

    private static void ThrowIfOtherActor(Actor? current, IActor actor, string aliasName)
    {
        if (current == null || current.Value.FormId == actor.GetFormId<Actor>())
        {
            return;
        }
        throw new InvalidOperationException($"{current.Value.FormId} is in the {aliasName} alias. Dismiss it first.");
    }
}
