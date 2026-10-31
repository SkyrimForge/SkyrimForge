using System.Threading.Tasks;

namespace SkyrimForge;

internal interface IQuestStages
{
    IRuntimeQuest Quest { get; }

    Task<bool> SetAsync(QuestStage stage);

    bool IsDone(QuestStage stage);
}
