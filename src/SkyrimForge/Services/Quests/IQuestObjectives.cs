using SkyrimForge.Game;
using System.Collections.Generic;

namespace SkyrimForge;

internal interface IQuestObjectives
{
    IRuntimeQuest Quest { get; }

    void Display(QuestObjective objective);

    void Complete(QuestObjective objective);

    void Fail(QuestObjective objective);

    void SetText(QuestObjective objective, string text);

    void SetTargetReferences<TReference>(QuestObjective objective, params IEnumerable<FormId<TReference>> referenceIds) where TReference : struct, IObjectReference;
}
