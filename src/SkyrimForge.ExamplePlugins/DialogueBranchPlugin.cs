using SkyrimForge.Game;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>When F2 is pressed, shows a dialogue branch in conversations with each actor of the same race as the actor under the crosshair.</summary>
public sealed class DialogueBranchPlugin(GameTarget gameTarget, IGameEvents events, IDialogueBranchFactory dialogueBranchFactory, IDebug debug, IUtility utility, ILog log) : IPlugin
{
    private IDialogueBranch? branch;

    /// <inheritdoc/>
    public void Start()
    {
        branch = dialogueBranchFactory.Create("example", CreateStartingTopic(debug), greeting: _ => ["Well met."]);
        branch.OnConversationEnd.Subscribe(args => log.Write($"Conversation with {args.Speaker?.GetDisplayFullName() ?? "an actor the game no longer has"} ended: {args.Reason}."));
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF2, ModifierKeys.None).Subscribe(_ => ShowAsync());
        log.Write("F2 shows the example dialogue branch for actors of the crosshair actor's race.");
    }

    // A choice's responses are computed after the starting topic's BeginCallback, so the count includes the current one.
    private static DialogueTopic CreateStartingTopic(IDebug debug)
    {
        int words = 0;
        var goodbye = new DialogueTopic("Goodbye.") { IsGoodbye = true };
        return new DialogueTopic("May I have a word?", ["Go on."])
        {
            BeginCallback = _ =>
            {
                words++;
                return Task.CompletedTask;
            },
            Choices =
            {
                new DialogueTopic("How often have I said that?", _ => [$"That makes {words}."]),
                new DialogueTopic(speaker => $"Tell me about yourself, {speaker.GetDisplayFullName() ?? "friend"}.", ["There is not much to tell."]),
                new DialogueTopic("Tell me a secret.", ["Only this once...", "Secret."]) { IsSaidOnce = true },
                new DialogueTopic("Send me a notification.", ["Look up once I finish."])
                {
                    EndCallback = context =>
                    {
                        debug.Notification($"{context.Speaker.GetDisplayFullName()} finished the reply.");
                        return Task.CompletedTask;
                    }
                },
                goodbye
            }
        };
    }

    private async Task ShowAsync()
    {
        await utility.WaitForGameThreadAsync();
        if (gameTarget.CrosshairReference?.AsActor() is not { } actor)
        {
            return;
        }
        FormId<Race>? raceId = actor.GetRace()?.FormId;
        branch!.Show(candidate => candidate.GetRace()?.FormId == raceId);
        string message = $"Showing the example dialogue branch for actors of race {raceId}, the race of {actor.GetDisplayFullName()}.";
        log.Write(message);
        debug.Notification(message);
    }
}
