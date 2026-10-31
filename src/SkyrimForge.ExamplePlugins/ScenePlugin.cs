using SkyrimForge.Game;
using SkyrimForge.Papyrus;
using System;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Plays two scenes in turn on Shift+F3, with the actor under the crosshair as their host, and stops them on a second press.</summary>
public sealed class ScenePlugin(GameTarget gameTarget, IGameEvents events, IRuntimeQuestFactory questFactory, IDialogueBranchFactory dialogueBranchFactory, IUtility utility, IDebug debug, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        const uint arrivalRadius = 150;
        const float steppedBackDistance = 350f;
        bool wasFirstPerson = false;

        QuestReferenceAlias host = new("Host"), guest = new("Guest");
        var invitation = new QuestScene("invitation",
            ScenePackage.TravelToPlayer(host, arrivalRadius, PACKAGE_DATA.PreferredSpeed.kRun),
            new SceneLine(host, actor => [$"I am {actor.GetDisplayFullName() ?? "a stranger"}. Step back, and I will call a friend over."]) { LooksAtPlayer = true, FacesTarget = true },
            new ScenePhase { EndsWhen = _ => host.ReferenceId?.FormOrNull?.GetDistance(gameTarget.Player) >= steppedBackDistance })
        {
            ActorsStayInPlace = true,
            IsInterruptible = false
        };

        IDialogueBranch branch = dialogueBranchFactory.Create("scene",
            new DialogueTopic("Tell me of your trade.", ["I hunt and sell the pelts in town."]), greeting: _ => ["You wished to speak?"], goodbye: _ => []);
        var meeting = new QuestScene("meeting",
            new ScenePhase(
                ScenePackage.TravelToPlayer(guest, arrivalRadius, PACKAGE_DATA.PreferredSpeed.kRun),
                new SceneLine(host, TopicResponse.Silence(TimeSpan.FromSeconds(3))) { LookTarget = guest }),
            new SceneLine(guest, [["You sent for me?"], ["I came as soon as I heard."]]) { LookTarget = host, FacesTarget = true },
            new SceneLine(QuestReferenceAlias.Player, "This is the friend, then."),
            new SceneConversation(guest, branch),
            new SceneLine(host, _ => branch.ChosenTopics.Count > 0
                ? [TopicResponse.Idle(ExampleForms.IdleApplaud2, TimeSpan.FromSeconds(3)), "You two will get along."]
                : ["Not one for talk, I see."]))
        {
            ActorsStayInPlace = true,
            IsInterruptible = false,
            DisabledPlayerControls = ScenePlayerControls.Movement | ScenePlayerControls.Fighting | ScenePlayerControls.Sneaking
        };

        IQuestScenes scenes = questFactory.Create("example", "Example Scenes", scenes: [invitation, meeting]).Scenes;
        scenes.OnPhaseBegin.Subscribe(args => log.Write($"Phase {args.PhaseIndex} of the scene \"{args.Scene.Name}\" began."));
        scenes.OnEnd.Subscribe(args => Report($"The example scene \"{args.Scene.Name}\" ended: {args.Reason}."));
        meeting.Events.OnEnd.Subscribe(EndMeeting);
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF3, ModifierKeys.Shift).Subscribe(_ => PlayOrStopAsync());
        log.Write("Shift+F3 plays the example scenes with the actor under the crosshair, or stops them.");

        async Task PlayOrStopAsync()
        {
            await utility.WaitForGameThreadAsync();
            if (scenes.IsAnyPlaying)
            {
                scenes.StopAll();
            }
            else if (gameTarget.CrosshairReference?.AsActor() is { } actor)
            {
                await PlayAsync(actor);
            }
        }

        async Task PlayAsync(Actor actor)
        {
            host.Fill(actor);
            // Resumes on the game's thread when the scene ends.
            if (await invitation.PlayAsync() != QuestSceneEndReason.Completed)
            {
                return;
            }
            // A scene that shares an actor with the previous scene starts after two seconds, so the fade covers the gap.
            GameScript.FadeOutGame(false, true, 2, 1).LogFault(log);
            ExampleForms.QSTDwemerGong01.FormOrNull?.Play(gameTarget.AudioManager, actor);
            guest.CreateActor(ExampleForms.EncHunterNordF, actor.GetPosition());
            wasFirstPerson = gameTarget.PlayerCamera.IsInFirstPerson();
            GameScript.ForceThirdPerson().LogFault(log);
            meeting.Start();
        }

        void EndMeeting(QuestSceneEndEventArgs args)
        {
            const float disableDistance = 1500f;
            if (args.Reason != QuestSceneEndReason.NewGameOrSaveLoading)
            {
                guest.DisableAndClearAsync(disableDistance, TimeSpan.FromSeconds(10)).LogFault(log);
                if (wasFirstPerson)
                {
                    GameScript.ForceFirstPerson().LogFault(log);
                }
            }
        }

        void Report(string message)
        {
            log.Write(message);
            debug.Notification(message);
        }
    }
}
