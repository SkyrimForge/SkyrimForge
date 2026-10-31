using SkyrimForge.Game;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>
/// When F6 is pressed, sets or dismisses the actor under the crosshair as the player's follower, or as the animal if the actor is not a person,
/// and logs the actor in the Follower or Animal alias.
/// </summary>
public sealed class FollowerPlugin(GameTarget gameTarget, IGameEvents events, IFollowers followers, IGameSession session, IDebug debug, IUtility utility, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF6, ModifierKeys.None).Subscribe(_ => ToggleAsync());
        log.Write("F6 sets or dismisses the actor under the crosshair as the follower or animal.");
    }

    private async Task ToggleAsync()
    {
        await utility.WaitForGameThreadAsync();
        if (gameTarget.CrosshairReference?.AsActor() is not { } actor)
        {
            return;
        }
        bool hasActorTypeNpc = actor.HasKeyword(KnownForms.ActorTypeNPC.Form);
        string role = hasActorTypeNpc ? "follower" : "animal";
        Actor? current = hasActorTypeNpc ? followers.Follower : followers.Animal;
        FormId<Actor> id = actor.FormId;
        // Canceled when a new game starts or a save starts loading, so nothing is reported into a different game.
        CancellationToken newGameOrSaveLoadingToken = session.NewGameOrSaveLoading;
        if (current?.FormId == id)
        {
            await (hasActorTypeNpc ? followers.DismissFollowerAsync() : followers.DismissAnimalAsync());
            newGameOrSaveLoadingToken.ThrowIfCancellationRequested();
            Report($"Dismissed {id} as the {role}.", hasActorTypeNpc);
            return;
        }
        if (current is { } currentActor)
        {
            Report($"{currentActor.FormId} is already the {role}. Dismiss it before setting {id}.", hasActorTypeNpc);
            return;
        }
        await (hasActorTypeNpc ? followers.SetFollowerAsync(actor) : followers.SetAnimalAsync(actor));
        newGameOrSaveLoadingToken.ThrowIfCancellationRequested();
        Report($"Set {id} as the {role}.", hasActorTypeNpc);
    }

    private void Report(string action, bool hasActorTypeNpc)
    {
        Actor? current = hasActorTypeNpc ? followers.Follower : followers.Animal;
        string message = $"{action} The {(hasActorTypeNpc ? "Follower" : "Animal")} alias contains {(current is { } currentActor ? $"{currentActor.FormId}" : "no actor")}.";
        log.Write(message);
        debug.Notification(message);
    }
}
