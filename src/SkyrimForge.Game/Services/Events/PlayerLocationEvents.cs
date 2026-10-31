namespace SkyrimForge.Game;

internal sealed class PlayerLocationEvents : IPlayerLocationEvents
{
    private readonly GameTarget gameTarget;
    private readonly IGameEvents events;
    private readonly ILog log;
    private readonly GameEvent<PlayerLocationEnterEventArgs> onEnter;

    private Location? pendingLocation;
    private bool watching;
    private ISubscription? frameSubscription;

    public PlayerLocationEvents(GameTarget gameTarget, IGameEvents events, ILog log)
    {
        this.gameTarget = gameTarget;
        this.events = events;
        this.log = log;
        onEnter = new GameEvent<PlayerLocationEnterEventArgs>(ex => this.log.Write($"{nameof(OnEnter)} threw: {ex}"));
    }

    public IGameEvent<PlayerLocationEnterEventArgs> OnEnter
    {
        get
        {
            if (!watching)
            {
                watching = true;
                events.OnActorLocationChange.Subscribe(Record);
                events.OnNewGameOrSaveLoading.Subscribe(_ => Reset());
            }
            return onEnter;
        }
    }

    private void Record(ActorLocationChangeEventArgs args)
    {
        if (args.Actor?.IsPlayerRef() == true && args.NewLocation != null)
        {
            pendingLocation = args.NewLocation;
            frameSubscription ??= events.OnFrame.Subscribe(_ => RaiseOnceLoaded());
        }
    }

    // Tests pendingLocation first, since GameTarget.Player throws before the game builds the player.
    private void RaiseOnceLoaded()
    {
        if (pendingLocation is not { } enteredLocation || !gameTarget.Player.Is3DLoaded())
        {
            return;
        }
        Reset();
        onEnter.Raise(new PlayerLocationEnterEventArgs(enteredLocation));
    }

    private void Reset()
    {
        pendingLocation = null;
        frameSubscription?.Unsubscribe();
        frameSubscription = null;
    }
}
