using System.Collections.Generic;

namespace SkyrimForge.Game;

internal interface ISceneOwner
{
    string Name { get; }

    IRuntimeQuest Quest { get; }

    FormId<ObjectReference>? GetReference(QuestReferenceAlias alias);

    FormId<ObjectReference>? GetLocationMarker(QuestLocationAlias alias);

    Quest CreateSceneQuestRecord(string name);

    IReadOnlyList<string> SavedSaidLines { get; }

    IEnumerable<RuntimeQuestScenes> PluginScenes { get; }
}
