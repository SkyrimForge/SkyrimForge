using SkyrimForge.Game;
using SkyrimForge.Papyrus;
using System;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Starts a quest on Shift+F1 to bring the actor under the crosshair a sweetroll, with a journal objective, a trigger zone, and a calendar timer.</summary>
public sealed class QuestPlugin(GameTarget gameTarget, IGameEvents events, IRuntimeQuestFactory questFactory, ITriggerZoneFactory zoneFactory, ICalendarTimerFactory timerFactory,
    IUtility utility, IDebug debug, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        ICalendarTimer cravingTimer = timerFactory.Create("craving");
        cravingTimer.OnElapsed.Subscribe(_ => debug.Notification("Your friend would like another sweetroll."));
        var friend = new QuestReferenceAlias("Friend");
        var wantedStage = new QuestStage(10, $"{friend} would pay good coin for a sweetroll.")
        {
            ObjectivesToDisplay = [new QuestObjective($"Bring {friend} a sweetroll") { Targets = [friend] }]
        };
        var deliveredStage = new QuestStage(20, $"I brought {friend} a sweetroll, and was paid for it.")
        {
            QuestOutcome = QuestOutcome.Completed,
            SetCallback = async context =>
            {
                const int reward = 100;
                cravingTimer.Start(TimeSpan.FromDays(1));
                PlayerCharacter player = gameTarget.Player;
                await player.Papyrus.RemoveItem(ExampleForms.FoodSweetroll);
                // An await can resume after another save loads. The token is canceled when that load starts.
                context.NewGameOrSaveLoading.ThrowIfCancellationRequested();
                await player.Papyrus.AddItem(KnownForms.Gold001, reward);
            }
        };
        IRuntimeQuest quest = questFactory.Create("sweetroll", "A Sweet Tooth", [wantedStage, deliveredStage], activeOnStart: true);
        zoneFactory.Create("friend", () => friend.ReferenceId, 400f).OnEnter.Subscribe(_ =>
        {
            if (wantedStage.IsCurrent)
            {
                debug.Notification("You still owe your friend a sweetroll.");
            }
        });
        friend.Events.OnPlayerActivate.Subscribe(async _ =>
        {
            await utility.WaitForGameThreadAsync();
            if (!wantedStage.IsCurrent)
            {
                return;
            }
            // Math.Abs matches Papyrus's GetItemCount, which returns the magnitude.
            int sweetrollCount = Math.Abs(gameTarget.Player.GetItemCount(ExampleForms.FoodSweetroll.Form));
            if (sweetrollCount > 0)
            {
                await deliveredStage.SetAsync();
            }
            else
            {
                debug.Notification("You have no sweetroll to give.");
            }
        });
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF1, ModifierKeys.Shift).Subscribe(async _ =>
        {
            await utility.WaitForGameThreadAsync();
            if (gameTarget.CrosshairReference?.AsActor() is { } actor)
            {
                quest.RemoveFromJournalAndReset();
                friend.Fill(actor);
                await wantedStage.SetAsync();
            }
        });
        log.Write("Shift+F1 starts the example quest with the actor under the crosshair.");
    }
}
