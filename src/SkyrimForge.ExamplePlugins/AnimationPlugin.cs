using SkyrimForge.Game;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Receives the player's animation events and logs each kind once, along with three other events about the player.</summary>
/// <remarks>
/// An animation event marks a point in one actor's animation, such as a hit landing or an arrow being released.
/// Animation events arrive several times a second during movement or combat.
/// </remarks>
public sealed class AnimationPlugin(GameTarget gameTarget, IGameEvents events, IAnimationEvents animationEvents, ILog log) : IPlugin
{
    private const int eventNameLimit = 24;

    private readonly HashSet<string> eventNames = [];

    private readonly HashSet<PositionPlayerEvent.EVENT_TYPE> phases = [];

    private int heardCount;

    // Read on worker threads without the lock.
    private volatile bool eventNameLimitReached;

    /// <inheritdoc/>
    public void Start()
    {
        SubscribeToPlayerAnimationAsync().LogFault(log);
        events.OnPositionPlayer.Subscribe(ReportPhase);
        events.OnActorCell.Subscribe(args => log.Write($"Player cell event: {args.CellID}, {args.Flags}."));
        events.OnActorDeath.Subscribe(_ => log.Write("The player died."));
        log.Write("Waiting for the player's animation graph.");
    }

    // The player has no animation graph until the world loads, so this retries each frame.
    private async Task SubscribeToPlayerAnimationAsync()
    {
        await events.OnFrame.WaitForNextAsync(_ => animationEvents.Subscribe(gameTarget.Player, OnAnimationEvent) != null);
        log.Write("Subscribed to the player's animation events.");
    }

    private void OnAnimationEvent(AnimationEventArgs animationEvent)
    {
        if (eventNameLimitReached)
        {
            return;
        }
        // The game raises these on worker threads.
        int eventNameCount;
        int total;
        lock (eventNames)
        {
            total = ++heardCount;
            if (eventNames.Count >= eventNameLimit || !eventNames.Add(animationEvent.Tag))
            {
                return;
            }
            eventNameCount = eventNames.Count;
        }
        log.Write($"Animation event \"{animationEvent.Tag}\"{(animationEvent.Payload.Length > 0 ? $", payload \"{animationEvent.Payload}\"" : "")}. Kinds: {eventNameCount}. Events: {total}.");
        if (eventNameCount == eventNameLimit)
        {
            eventNameLimitReached = true;
            log.Write($"Logged {eventNameLimit} kinds. Further kinds are not logged.");
        }
    }

    private void ReportPhase(PositionPlayerEventArgs args)
    {
        int count = 0;
        lock (phases)
        {
            if (!phases.Add(args.Type))
            {
                return;
            }
            count = phases.Count;
        }
        log.Write($"Position-player phase {args.Type}. Phases seen: {count}.");
    }
}
