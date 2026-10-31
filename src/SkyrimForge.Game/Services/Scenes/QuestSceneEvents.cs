namespace SkyrimForge.Game;

internal sealed class QuestSceneEvents(IQuestSceneEvents sceneEvents, QuestScene scene) : IQuestSceneEvents
{
    public IGameEvent<QuestSceneBeginEventArgs> OnBegin { get; } = new FilteredGameEvent<QuestSceneBeginEventArgs>(sceneEvents.OnBegin, args => args.Scene == scene);

    public IGameEvent<QuestScenePhaseEventArgs> OnPhaseBegin { get; } =
        new FilteredGameEvent<QuestScenePhaseEventArgs>(sceneEvents.OnPhaseBegin, args => args.Scene == scene);

    public IGameEvent<QuestScenePhaseEventArgs> OnPhaseEnd { get; } =
        new FilteredGameEvent<QuestScenePhaseEventArgs>(sceneEvents.OnPhaseEnd, args => args.Scene == scene);

    public IGameEvent<QuestSceneLineEventArgs> OnLineBegin { get; } = new FilteredGameEvent<QuestSceneLineEventArgs>(sceneEvents.OnLineBegin, args => args.Scene == scene);

    public IGameEvent<QuestSceneLineEventArgs> OnLineEnd { get; } = new FilteredGameEvent<QuestSceneLineEventArgs>(sceneEvents.OnLineEnd, args => args.Scene == scene);

    public IGameEvent<QuestSceneActionEndEventArgs> OnActionEnd { get; } =
        new FilteredGameEvent<QuestSceneActionEndEventArgs>(sceneEvents.OnActionEnd, args => args.Scene == scene);

    public IGameEvent<QuestSceneEndEventArgs> OnEnd { get; } = new FilteredGameEvent<QuestSceneEndEventArgs>(sceneEvents.OnEnd, args => args.Scene == scene);
}
