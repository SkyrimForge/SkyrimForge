using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="Quest"/>.</summary>
public static class QuestExtensions
{
    extension(Quest quest)
    {
        /// <summary>Sets the text of the quest's objective with <paramref name="index"/>, adding the objective if the quest has none with that index.</summary>
        /// <returns>The objective, which the quest retains until the process exits.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public BGSQuestObjective SetObjective(ushort index, string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            if (quest.Objectives.Get().FirstOrNull(existing => existing.Index == index) is not { } objective)
            {
                objective = BGSQuestObjective.New();
                objective.OwnerQuest = quest;
                objective.Index = index;
                objective.Initialized = true;
                quest.Objectives.Add(objective);
            }
            objective.DisplayText = text;
            return objective;
        }

        /// <summary>Adds <paramref name="topic"/> to this quest's player dialogue, in the branch it starts or else in a new top-level branch.</summary>
        /// <returns>Whether the quest's branch table lists the branch afterward.</returns>
        /// <remarks>The dialogue menu lists only the starting topics of top-level branches. Call on the game's thread, while no conversation is open.</remarks>
        public bool AddPlayerTopic(Topic topic) =>
            FindBranch(quest, topic) is { } branch
            && NativeSupplements.SF_Repair_TESQuest_BranchedDialogue_Emplace(quest.Address, (int)DIALOGUE_TYPES.DIALOGUE_TYPE.kPlayerDialogue, branch.Address, topic.Address);
    }

    private static BGSDialogueBranch? FindBranch(Quest quest, Topic topic)
    {
        if (topic.OwnerBranch is { } ownerBranch && ownerBranch.StartingTopic?.Address == topic.Address)
        {
            return ownerBranch;
        }
        if (IFormFactory.GetFormFactoryByType(FormType.DialogueBranch)?.Create()?.AsBGSDialogueBranch() is not { } branch)
        {
            return null;
        }
        branch.Flags |= BGSDialogueBranch.Flag.kTopLevel;
        branch.Quest = quest;
        branch.StartingTopic = topic;
        branch.Type = DIALOGUE_TYPES.DIALOGUE_TYPE.kPlayerDialogue;
        topic.OwnerBranch = branch;
        topic.OwnerQuest = quest;
        return branch;
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Repair_TESQuest_BranchedDialogue_Emplace(nint quest, int type, nint branch, nint topic);
}
