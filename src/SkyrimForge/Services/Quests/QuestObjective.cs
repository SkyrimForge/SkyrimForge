using SkyrimForge.Game;
using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>An objective that a <see cref="QuestStage"/> displays, completes, or fails.</summary>
/// <remarks>
/// A token <c>&lt;Alias=Name&gt;</c> in the text is replaced, as the objective is displayed, with the name of the reference or location that fills that alias.
/// An alias converts to its token in an interpolated string: <c>$"Bring {friend} a sweetroll"</c>.
/// Call <see cref="Display"/>, <see cref="Complete"/>, <see cref="Fail"/>, <see cref="SetText"/>, and <see cref="SetTargetReferences"/> on the game's thread with a game loaded.
/// </remarks>
public sealed class QuestObjective
{
    private readonly Func<string>? computeText;
    private readonly string? text;

    /// <summary>Creates an objective whose text <see cref="SetText"/> sets before a stage displays it.</summary>
    public QuestObjective() { }

    /// <summary>Creates an objective with a fixed text.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
    public QuestObjective(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        this.text = text;
    }

    /// <summary>Creates an objective with a computed text.</summary>
    /// <param name="text">Returns the text the journal shows. It is called each time a stage displays the objective.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
    public QuestObjective(Func<string> text)
    {
        ArgumentNullException.ThrowIfNull(text);
        computeText = text;
    }

    /// <summary>
    /// The objective's index, unique among the quest's objectives and the same in every version of the plugin. The saved state records it.
    /// Null means the lowest index of the stages that display it.
    /// </summary>
    public ushort? Index { get; init; }

    /// <summary>The aliases the objective's markers point at while it is neither completed nor failed.</summary>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyList<QuestReferenceAlias> Targets { get; init => field = [.. value ?? throw new ArgumentNullException(nameof(value))]; } = [];

    /// <summary>The location aliases whose locations' world location markers the objective's markers point at while it is neither completed nor failed.</summary>
    /// <remarks>A location with no world location marker shows no marker and is logged.</remarks>
    /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
    public IReadOnlyList<QuestLocationAlias> LocationTargets { get; init => field = [.. value ?? throw new ArgumentNullException(nameof(value))]; } = [];

    /// <summary>The quest the objective belongs to.</summary>
    /// <exception cref="InvalidOperationException">No quest whose stages display, complete, or fail the objective has been created.</exception>
    public IRuntimeQuest Quest => Owner.Quest;

    internal IQuestObjectives? OwnerOrNull { get; set; }

    private IQuestObjectives Owner => OwnerOrNull ?? throw new InvalidOperationException(
        $"The objective {(Index is { } index ? $"with index {index}" : "created with no index")} belongs to no quest. A quest declares the objectives its stages display, complete, or fail.");

    /// <summary>Displays the objective in the journal, and starts the quest if the quest has displayed no objective.</summary>
    /// <remarks>Displaying an objective again leaves it completed or failed if it was.</remarks>
    /// <exception cref="InvalidOperationException">No quest declares the objective, or it has no text declared or set.</exception>
    public void Display() => Owner.Display(this);

    /// <summary>Marks the objective completed, replacing a failure.</summary>
    /// <exception cref="InvalidOperationException">No quest declares the objective, or the quest has not displayed it.</exception>
    public void Complete() => Owner.Complete(this);

    /// <summary>Marks the objective failed, replacing a completion.</summary>
    /// <exception cref="InvalidOperationException">No quest declares the objective, or the quest has not displayed it.</exception>
    public void Fail() => Owner.Fail(this);

    /// <summary>Sets the text the objective displays, replacing its declared text.</summary>
    /// <remarks>The text is saved with the quest's progress, and a stage that displays the objective later displays that text.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">No quest declares the objective.</exception>
    public void SetText(string text) => Owner.SetText(this, text);

    /// <summary>
    /// Sets the references the objective's markers point at besides the aliases in <see cref="Targets"/>, replacing those set before. An empty list removes them.
    /// </summary>
    /// <remarks>The references are saved with the quest's progress. A reference the loaded game does not have shows no marker, and the log identifies it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="referenceIds"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">No quest declares the objective.</exception>
    public void SetTargetReferences<TReference>(params IEnumerable<FormId<TReference>> referenceIds) where TReference : struct, IObjectReference =>
        Owner.SetTargetReferences(this, referenceIds);

    internal string? GetText() => computeText != null ? computeText() : text;
}
