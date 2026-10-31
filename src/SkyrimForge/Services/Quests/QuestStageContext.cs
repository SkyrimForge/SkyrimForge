using System.Threading;

namespace SkyrimForge;

/// <summary>Arguments for <see cref="QuestStage.SetCallback"/>.</summary>
/// <param name="Quest">The quest the stage is set on.</param>
/// <param name="Stage">The stage set.</param>
/// <param name="NewGameOrSaveLoading">The <see cref="IGameSession.NewGameOrSaveLoading"/> of the game the stage was set in.</param>
public readonly record struct QuestStageContext(IRuntimeQuest Quest, QuestStage Stage, CancellationToken NewGameOrSaveLoading);
