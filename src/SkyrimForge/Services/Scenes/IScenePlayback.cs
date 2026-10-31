using System.Threading.Tasks;

namespace SkyrimForge;

internal interface IScenePlayback
{
    IRuntimeQuest Quest { get; }

    QuestSceneStartResult Start(QuestScene scene, int firstPhaseIndex);

    QuestSceneStartResult ForceStart(QuestScene scene, int firstPhaseIndex);

    Task<QuestSceneEndReason?> PlayAsync(QuestScene scene, int firstPhaseIndex);

    void Stop(QuestScene scene);

    bool IsPlaying(QuestScene scene);

    IQuestSceneEvents GetEvents(QuestScene scene);

    int? GetCurrentPhaseIndex(QuestScene scene);
}
