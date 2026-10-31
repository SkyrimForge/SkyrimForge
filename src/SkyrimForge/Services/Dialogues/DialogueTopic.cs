using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>A player dialogue topic in a dialogue branch that <see cref="IDialogueBranchFactory"/> creates.</summary>
/// <remarks>After a topic with no <see cref="Choices"/> that pass and with <see cref="IsGoodbye"/> false, the menu shows the branch's starting topic again.</remarks>
public sealed class DialogueTopic
{
    private readonly Func<Actor, string>? computeText;

    private readonly Func<Actor, IEnumerable<TopicResponse>>? computeResponses;

    private readonly ChoiceCollection choices = [];

    /// <summary>Creates a topic whose responses are fixed. A string converts to an unvoiced response.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="responses"/> is <c>null</c>.</exception>
    public DialogueTopic(string text, params IEnumerable<TopicResponse> responses) : this(Require(text), null, Require(responses), null) { }

    /// <summary>Creates a topic whose responses are computed each time the branch shows it.</summary>
    /// <remarks>For a choice, <paramref name="responses"/> runs after the <see cref="BeginCallback"/> of the topic before it.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="responses"/> is <c>null</c>.</exception>
    public DialogueTopic(string text, Func<Actor, IEnumerable<TopicResponse>> responses) : this(Require(text), null, [], Require(responses)) { }

    /// <summary>Creates a topic whose text is computed each time the branch shows it, and whose responses are fixed. A string converts to an unvoiced response.</summary>
    /// <remarks>
    /// For a choice, <paramref name="text"/> runs after the <see cref="BeginCallback"/> of the topic before it. It also runs on each frame <see cref="Condition"/> is called.
    /// The game never frees the record created for each distinct text, so return few distinct texts.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="responses"/> is <c>null</c>.</exception>
    public DialogueTopic(Func<Actor, string> text, params IEnumerable<TopicResponse> responses) : this(null, Require(text), Require(responses), null) { }

    /// <summary>Creates a topic whose text and responses are computed each time the branch shows it.</summary>
    /// <remarks>
    /// For a choice, both run after the <see cref="BeginCallback"/> of the topic before it. <paramref name="text"/> also runs on each frame <see cref="Condition"/> is called.
    /// The game never frees the record created for each distinct text, so return few distinct texts.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="responses"/> is <c>null</c>.</exception>
    public DialogueTopic(Func<Actor, string> text, Func<Actor, IEnumerable<TopicResponse>> responses) : this(null, Require(text), [], Require(responses)) { }

    private DialogueTopic(string? text, Func<Actor, string>? computeText, IEnumerable<TopicResponse> fixedResponses, Func<Actor, IEnumerable<TopicResponse>>? computeResponses)
    {
        Text = text;
        this.computeText = computeText;
        FixedResponses = [.. fixedResponses];
        this.computeResponses = computeResponses;
    }

    /// <summary>The text the dialogue menu shows. The value is null for a computed text.</summary>
    public string? Text { get; }

    /// <summary>Whether the conversation ends after the speaker's responses, with no goodbye line from the game.</summary>
    public bool IsGoodbye { get; init; }

    /// <summary>Whether the topic is shown only until the player chooses it with the speaker, in this save and every later one.</summary>
    /// <remarks>The saved state records the speaker and the text, so a topic whose text changes is shown again, as is the same topic with another speaker.</remarks>
    public bool IsSaidOnce { get; init; }

    /// <summary>Whether the speaker's other topics are hidden while the branch shows this topic for that speaker.</summary>
    /// <remarks>If several branches show such a topic for one speaker, the topics of those branches all remain.</remarks>
    public bool HidesOtherTopics { get; init; }

    /// <summary>Returns whether the menu shows the topic for the speaker. A null value always shows the topic.</summary>
    /// <remarks>
    /// It is called on each frame for a topic the branch shows with no conversation open.
    /// For a choice, it is called once the topic before it is chosen, after that topic's <see cref="BeginCallback"/>.
    /// </remarks>
    public Func<Actor, bool>? Condition { get; init; }

    /// <summary>Runs on the game's thread when the player chooses the topic, as the speaker's responses start.</summary>
    /// <remarks>The conditions of <see cref="Choices"/> are evaluated when it returns its task, so only state changed before its first await affects them.</remarks>
    public Func<DialogueTopicContext, Task>? BeginCallback { get; init; }

    /// <summary>
    /// Runs once <see cref="BeginCallback"/> has completed and the speaker's responses have ended or the conversation has closed,
    /// on the thread that completed the later of the two.
    /// </summary>
    public Func<DialogueTopicContext, Task>? EndCallback { get; init; }

    /// <summary>The topics shown once the player chooses this one, in the order the menu shows them.</summary>
    /// <remarks>
    /// A choice may be a topic shown earlier, which returns the conversation to that topic, and a topic may be a choice of several topics.
    /// Add choices before <see cref="IDialogueBranchFactory.Create"/> is called. A change after it throws <see cref="InvalidOperationException"/>.
    /// </remarks>
    public IList<DialogueTopic> Choices => choices;

    internal IReadOnlyList<TopicResponse> FixedResponses { get; }

    internal void MakeChoicesReadOnly() => choices.IsReadOnly = true;

    internal IReadOnlyList<TopicResponse> GetResponses(Actor speaker) => computeResponses != null ? [.. computeResponses(speaker)] : FixedResponses;

    internal string GetText(Actor speaker) => computeText != null ? computeText(speaker) : Text!;

    private static T Require<T>(T value, [CallerArgumentExpression(nameof(value))] string? name = null) where T : class => value ?? throw new ArgumentNullException(name);

    private sealed class ChoiceCollection : Collection<DialogueTopic>
    {
        internal bool IsReadOnly { get; set; }

        protected override void InsertItem(int index, DialogueTopic item)
        {
            ThrowIfReadOnly();
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, DialogueTopic item)
        {
            ThrowIfReadOnly();
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            ThrowIfReadOnly();
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            ThrowIfReadOnly();
            base.ClearItems();
        }

        private void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("The choices of a topic cannot change once a dialogue branch is created from it.");
            }
        }
    }
}
