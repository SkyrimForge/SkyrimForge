using System.Collections.Generic;

namespace SkyrimForge.Game;

internal sealed record SceneRecord(Scene Scene, IReadOnlyList<SceneRecordTravel> Travels, IReadOnlyList<ConditionGlobal> Conditions, IReadOnlyDictionary<int, ConditionGlobal> PhaseStarts, IReadOnlyList<SceneRecordAction> Actions, IReadOnlyList<SceneRecordLine> Lines, IReadOnlyList<SceneRecordSayOnceInfo> SayOnceInfos, IReadOnlyList<SceneRecordConversation> ConversationPackages)
{
    internal void WriteConditions(ILog log, string sceneName, string questName)
    {
        foreach (ConditionGlobal condition in Conditions)
        {
            condition.Write(ex => log.Write($"A condition of the scene \"{sceneName}\" of the quest \"{questName}\" threw, and counts as false: {ex}"));
        }
    }
}
