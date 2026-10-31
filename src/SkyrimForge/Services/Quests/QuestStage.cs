using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>A stage of a quest, which <see cref="SetAsync"/> sets once.</summary>
/// <param name="index">The stage's index, unique among the quest's stages and the same in every version of the plugin. The saved state records it.</param>
/// <remarks>
/// Setting the stage completes <see cref="ObjectivesToComplete"/>, fails <see cref="ObjectivesToFail"/>,
/// ends the other open objectives as <see cref="OpenObjectivesOutcome"/> states, displays <see cref="ObjectivesToDisplay"/>,
/// writes the log entry as the quest's description, ends the quest as <see cref="QuestOutcome"/> states, starts <see cref="ScenesToStart"/>,
/// and then runs <see cref="SetCallback"/>, in that order.
/// Call <see cref="SetAsync"/>, <see cref="IsDone"/>, and <see cref="IsCurrent"/> on the game's thread with a game loaded.
/// </remarks>
public sealed class QuestStage(ushort index)
{
    private readonly Func<string>? computeLogEntry;

    /// <summary>Creates a stage with a fixed log entry.</summary>
    /// <param name="index">The stage's index, unique among the quest's stages and the same in every version of the plugin.</param>
    /// <param name="logEntry">
    /// The paragraph the journal shows under the quest's name from this stage onward. Its alias tokens are replaced as the stage is set, as <see cref="QuestObjective"/> describes.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="logEntry"/> is <c>null</c>.</exception>
    public QuestStage(ushort index, string logEntry) : this(index)
    {
        ArgumentNullException.ThrowIfNull(logEntry);
        LogEntry = logEntry;
    }

    /// <summary>Creates a stage whose log entry is computed each time it is set.</summary>
    /// <param name="index">The stage's index, unique among the quest's stages and the same in every version of the plugin.</param>
    /// <param name="logEntry">
    /// Returns the paragraph the journal shows under the quest's name from this stage onward. Its alias tokens are replaced as <see cref="QuestObjective"/> describes.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="logEntry"/> is <c>null</c>.</exception>
    public QuestStage(ushort index, Func<string> logEntry) : this(index)
    {
        ArgumentNullException.ThrowIfNull(logEntry);
        computeLogEntry = logEntry;
    }

    /// <summary>The stage's index.</summary>
    public ushort Index { get; } = index;

    /// <summary>The quest the stage belongs to.</summary>
    /// <exception cref="InvalidOperationException">No quest that declares the stage has been created.</exception>
    public IRuntimeQuest Quest => Owner.Quest;

    internal IQuestStages? OwnerOrNull { get; set; }

    private IQuestStages Owner => OwnerOrNull ?? throw new InvalidOperationException($"Stage {Index} belongs to no quest. A quest declares the stages passed to it as it is created.");

    /// <summary>Sets the stage on its quest. Setting a stage that is already set does nothing.</summary>
    /// <returns>
    /// A task whose result is whether this call set the stage.
    /// The stage is applied before this returns, and the task completes once <see cref="SetCallback"/> does.
    /// An exception a computed log entry throws faults the task and leaves the stage unset.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// No quest declares the stage. The exception is stored in the returned task instead, and nothing is changed,
    /// if the stage completes or fails the quest while the quest has displayed no objective and the stage displays none,
    /// or if the stage displays an objective with no text declared or set.
    /// </exception>
    public Task<bool> SetAsync() => Owner.SetAsync(this);

    /// <summary>Whether the stage is set in the loaded game.</summary>
    /// <exception cref="InvalidOperationException">No quest declares the stage.</exception>
    public bool IsDone => Owner.IsDone(this);

    /// <summary>Whether the stage is the quest's <see cref="IRuntimeQuest.CurrentStage"/> in the loaded game.</summary>
    /// <exception cref="InvalidOperationException">No quest declares the stage.</exception>
    public bool IsCurrent => Quest.CurrentStage == this;

    /// <summary>
    /// The fixed paragraph the journal shows under the quest's name from this stage on.
    /// It is null for a stage created with a computed log entry or with none. A stage with no log entry leaves the description unchanged.
    /// </summary>
    public string? LogEntry { get; }

    internal string? GetLogEntry() => computeLogEntry != null ? computeLogEntry() : LogEntry;

    /// <summary>The objectives the stage completes. An objective that is not displayed, or is already completed or failed, is unchanged.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyList<QuestObjective> ObjectivesToComplete { get; init => field = [.. value ?? throw new ArgumentNullException(nameof(value))]; } = [];

    /// <summary>The objectives the stage fails. An objective that is not displayed, or is already completed or failed, is unchanged.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyList<QuestObjective> ObjectivesToFail { get; init => field = [.. value ?? throw new ArgumentNullException(nameof(value))]; } = [];

    /// <summary>The objectives the stage displays.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyList<QuestObjective> ObjectivesToDisplay { get; init => field = [.. value ?? throw new ArgumentNullException(nameof(value))]; } = [];

    /// <summary>
    /// The outcome the stage gives every open objective other than those it lists in <see cref="ObjectivesToComplete"/>, <see cref="ObjectivesToFail"/>,
    /// and <see cref="ObjectivesToDisplay"/>. Null means <see cref="QuestOutcome"/>, and leaves them open when that is null as well.
    /// </summary>
    public QuestOutcome? OpenObjectivesOutcome { get; init; }

    /// <summary>The outcome the stage gives the quest, and its open objectives unless <see cref="OpenObjectivesOutcome"/> is set. Null leaves the quest open.</summary>
    public QuestOutcome? QuestOutcome { get; init; }

    /// <summary>The scenes the stage starts.</summary>
    /// <remarks>A scene that does not start is logged, and the stage is set regardless.</remarks>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyList<QuestScene> ScenesToStart { get; init => field = [.. value ?? throw new ArgumentNullException(nameof(value))]; } = [];

    /// <summary>Runs on the thread that sets the stage, once it is set.</summary>
    /// <remarks>It does not run again when a save is loaded. An exception it throws faults the task <see cref="SetAsync"/> returns, with the stage set.</remarks>
    public Func<QuestStageContext, Task>? SetCallback { get; init; }
}
