namespace SkyrimForge;

/// <summary>Events of <see cref="IGameEvents"/> filtered to the reference that fills a <see cref="QuestReferenceAlias"/> when the event is raised.</summary>
/// <remarks>
/// The activation, hit, combat, and death events are raised on worker threads, and the load and cell events on the game's thread.
/// The container event is raised on the thread that moved the item, which is a worker thread for a pickup or a Papyrus call,
/// and the game's thread for a console command or a CommonLib call made there.
/// </remarks>
public interface IQuestReferenceAliasEvents
{
    /// <summary>Raised when the reference is activated.</summary>
    IGameEvent<ActivateEventArgs> OnActivate { get; }

    /// <summary>Raised when the player activates the reference.</summary>
    IGameEvent<ActivateEventArgs> OnPlayerActivate { get; }

    /// <summary>Raised when the game loads the reference.</summary>
    IGameEvent<ObjectLoadedEventArgs> OnLoad { get; }

    /// <summary>Raised when the game unloads the reference.</summary>
    IGameEvent<ObjectLoadedEventArgs> OnUnload { get; }

    /// <summary>Raised as the actor starts dying, with <see cref="DeathEventArgs.Dead"/> false.</summary>
    IGameEvent<DeathEventArgs> OnDying { get; }

    /// <summary>Raised once the actor is dead, with <see cref="DeathEventArgs.Dead"/> true.</summary>
    IGameEvent<DeathEventArgs> OnDeath { get; }

    /// <summary>Raised when the reference is hit.</summary>
    IGameEvent<HitEventArgs> OnHit { get; }

    /// <summary>Raised when the actor's combat state changes.</summary>
    IGameEvent<CombatEventArgs> OnCombatStateChanged { get; }

    /// <summary>Raised when the reference's cell is attached.</summary>
    IGameEvent<CellAttachDetachEventArgs> OnCellAttach { get; }

    /// <summary>Raised when the reference's cell is detached.</summary>
    IGameEvent<CellAttachDetachEventArgs> OnCellDetach { get; }

    /// <summary>Raised when the reference moves from one container to another.</summary>
    IGameEvent<ContainerChangedEventArgs> OnContainerChanged { get; }
}
