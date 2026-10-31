namespace SkyrimForge;

/// <summary>The story manager's events: crafting, arrest, a killing, a relationship changing, and every other kind the game registers.</summary>
/// <remarks>
/// A handler runs on the thread on which the game raised the event, before the manager traverses its tree.
/// Queue work that changes the game through <see cref="ITaskQueue"/>.
/// </remarks>
public interface IStoryEvents
{
    /// <summary>Raised for every story event the game passes to its story manager.</summary>
    IGameEvent<StoryEventArgs> OnStoryEvent { get; }
}
