using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge;

internal sealed class QuestDeclaration
{
    internal QuestDeclaration(IEnumerable<QuestStage> stages, IEnumerable<QuestScene>? scenes = null, IEnumerable<QuestAlias>? aliases = null)
    {
        ArgumentNullException.ThrowIfNull(stages);
        QuestStage[] declaredStages = [.. stages];
        Dictionary<ushort, QuestStage> stagesByIndex = [];
        Dictionary<ushort, QuestObjective> objectivesByIndex = [];
        Dictionary<QuestObjective, ushort> objectiveIndexes = [];
        foreach (QuestStage stage in declaredStages.OrderBy(stage => stage.Index))
        {
            foreach (QuestObjective objective in stage.ObjectivesToDisplay)
            {
                objectiveIndexes.TryAdd(objective, objective.Index ?? stage.Index);
            }
        }
        Dictionary<string, object> aliasesByName = new(StringComparer.Ordinal);
        void Declare(string name, object alias, string parameterName)
        {
            if (!aliasesByName.TryAdd(name, alias) && aliasesByName[name] != alias)
            {
                throw new ArgumentException($"Two aliases have the name \"{name}\".", parameterName);
            }
        }
        foreach (QuestStage stage in declaredStages)
        {
            if (!stagesByIndex.TryAdd(stage.Index, stage))
            {
                throw new ArgumentException($"Two stages have the index {stage.Index}.", nameof(stages));
            }
            foreach (QuestObjective objective in stage.ObjectivesToComplete.Concat(stage.ObjectivesToFail).Concat(stage.ObjectivesToDisplay))
            {
                if (!objectiveIndexes.TryGetValue(objective, out ushort index))
                {
                    index = objective.Index ?? throw new ArgumentException($"Stage {stage.Index} completes or fails an objective created with no index, which no stage displays.", nameof(stages));
                    objectiveIndexes.Add(objective, index);
                }
                if (!objectivesByIndex.TryAdd(index, objective) && objectivesByIndex[index] != objective)
                {
                    throw new ArgumentException($"Two objectives have the index {index}.", nameof(stages));
                }
                foreach (QuestReferenceAlias alias in objective.Targets)
                {
                    Declare(alias.Name, alias, nameof(stages));
                }
                foreach (QuestLocationAlias alias in objective.LocationTargets)
                {
                    Declare(alias.Name, alias, nameof(stages));
                }
            }
            if (stage.ObjectivesToComplete.Intersect(stage.ObjectivesToFail).FirstOrDefault() is { } conflictingObjective)
            {
                throw new ArgumentException($"Stage {stage.Index} both completes and fails objective {objectiveIndexes[conflictingObjective]}.", nameof(stages));
            }
        }
        Dictionary<string, QuestScene> scenesByName = new(StringComparer.Ordinal);
        foreach (QuestScene scene in (scenes ?? []).Concat(stagesByIndex.Values.SelectMany(stage => stage.ScenesToStart)))
        {
            if (!scenesByName.TryAdd(scene.Name, scene) && scenesByName[scene.Name] != scene)
            {
                throw new ArgumentException($"Two scenes have the name \"{scene.Name}\".", nameof(scenes));
            }
            foreach (SceneAction action in scene.Phases.SelectMany(phase => phase.Actions))
            {
                foreach (QuestReferenceAlias alias in action.Aliases)
                {
                    Declare(alias.Name, alias, nameof(scenes));
                }
                foreach (QuestLocationAlias alias in action.LocationAliases)
                {
                    Declare(alias.Name, alias, nameof(scenes));
                }
            }
            foreach (QuestStage stage in scene.Phases.Select(phase => phase.StageSetWhenBegun).Append(scene.StageSetWhenCompleted).WhereNotNull())
            {
                if (stagesByIndex.GetValueOrDefault(stage.Index) != stage)
                {
                    throw new ArgumentException($"The scene \"{scene.Name}\" sets a stage with index {stage.Index}, which the quest does not declare.", nameof(scenes));
                }
            }
        }
        // Declared last, so an alias that a stage or a scene references retains its place in the order.
        foreach (QuestAlias alias in aliases ?? [])
        {
            Declare(alias.Name, alias, nameof(aliases));
        }
        Stages = stagesByIndex;
        Objectives = objectivesByIndex;
        ObjectiveIndexes = objectiveIndexes;
        Scenes = [.. scenesByName.Values];
        Aliases = [.. aliasesByName.Values.OfType<QuestReferenceAlias>()];
        LocationAliases = [.. aliasesByName.Values.OfType<QuestLocationAlias>()];
    }

    internal IReadOnlyDictionary<ushort, QuestStage> Stages { get; }

    internal IReadOnlyDictionary<ushort, QuestObjective> Objectives { get; }

    internal IReadOnlyDictionary<QuestObjective, ushort> ObjectiveIndexes { get; }

    internal IReadOnlyList<QuestScene> Scenes { get; }

    internal IReadOnlyList<QuestReferenceAlias> Aliases { get; }

    internal IReadOnlyList<QuestLocationAlias> LocationAliases { get; }
}
