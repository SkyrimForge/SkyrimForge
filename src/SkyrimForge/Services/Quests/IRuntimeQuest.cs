using System;

namespace SkyrimForge;

/// <summary>A quest created by <see cref="IRuntimeQuestFactory"/>.</summary>
/// <remarks>Call members on the game's thread with a game loaded. Reading <see cref="Name"/> or <see cref="Scenes"/> does not need a game loaded.</remarks>
public interface IRuntimeQuest
{
    /// <summary>The name the journal shows.</summary>
    string Name { get; }

    /// <summary>The paragraph the journal shows under the quest's name in the loaded game.</summary>
    /// <remarks>Alias tokens in an assigned text are replaced as it is assigned, as <see cref="QuestObjective"/> describes.</remarks>
    /// <exception cref="ArgumentNullException">The assigned text is <c>null</c>.</exception>
    string Description { get; set; }

    /// <summary>Whether the quest has displayed an objective in the loaded game.</summary>
    bool IsStarted { get; }

    /// <summary>Whether the quest is completed in the loaded game.</summary>
    bool IsCompleted { get; }

    /// <summary>Whether the quest is failed in the loaded game.</summary>
    bool IsFailed { get; }

    /// <summary>The stage set last in the loaded game. Null means no stage is set.</summary>
    QuestStage? CurrentStage { get; }

    /// <summary>Moves the quest to the journal's completed quests, replacing a failure, and clears its active-quest mark.</summary>
    /// <remarks>A playing scene of the quest plays on.</remarks>
    /// <param name="completeObjectives">Whether every objective neither completed nor failed is marked completed as well.</param>
    /// <exception cref="InvalidOperationException">The quest has displayed no objective.</exception>
    void Complete(bool completeObjectives = false);

    /// <summary>Marks the quest failed, replacing a completion. It finishes the quest as <see cref="Complete"/> does.</summary>
    /// <param name="failObjectives">Whether every objective neither completed nor failed is marked failed as well.</param>
    /// <exception cref="InvalidOperationException">The quest has displayed no objective.</exception>
    void Fail(bool failObjectives = false);

    /// <summary>Whether the quest is the journal's active quest, whose markers the compass and the map show.</summary>
    /// <remarks>A quest that has displayed no objective, or is completed or failed, is not active, and setting it active leaves it as it is.</remarks>
    bool IsActive { get; set; }

    /// <summary>
    /// Removes the quest from the journal in the loaded game, resets its objectives, stages, and aliases,
    /// and clears its record of which scene line variants have been said.
    /// </summary>
    /// <remarks>Each playing scene of the quest is stopped.</remarks>
    void RemoveFromJournalAndReset();

    /// <summary>The quest's scenes.</summary>
    IQuestScenes Scenes { get; }
}
