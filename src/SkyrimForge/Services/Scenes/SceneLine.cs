using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge;

/// <summary>A dialogue action of a <see cref="ScenePhase"/>, in which the actor filling an alias says responses.</summary>
public sealed record SceneLine : SceneAction
{
    /// <summary>Creates a line of one variant.</summary>
    /// <param name="speaker">The alias whose actor says the line.</param>
    /// <param name="responses">The responses, said in order. A string converts to an unvoiced response.</param>
    /// <exception cref="ArgumentException"><paramref name="responses"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="speaker"/> or <paramref name="responses"/> is <c>null</c>.</exception>
    public SceneLine(QuestReferenceAlias speaker, params IEnumerable<TopicResponse> responses) : this(speaker, [GetResponses(speaker, responses, nameof(responses))])
    {
    }

    /// <summary>Creates a line of which the game says one variant, chosen at random each time the line plays.</summary>
    /// <param name="speaker">The alias whose actor says the line.</param>
    /// <param name="variants">The variants, each its responses said in order, as in <c>[["Hello."], ["Well met.", "What brings you here?"]]</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="variants"/> is empty, or a variant has no response.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="speaker"/>, <paramref name="variants"/>, or a variant is <c>null</c>.</exception>
    public SceneLine(QuestReferenceAlias speaker, IEnumerable<IEnumerable<TopicResponse>> variants) : base(speaker)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(variants);
        Speaker = speaker;
        Variants = [.. variants.Select(variant => GetResponses(speaker, variant, nameof(variants)))];
        if (Variants.Count == 0)
        {
            throw new ArgumentException($"A scene line said by the alias \"{speaker.Name}\" has no variant.", nameof(variants));
        }
    }

    /// <summary>Creates a line whose responses are computed each time its phase begins.</summary>
    /// <param name="speaker">The alias whose actor says the line.</param>
    /// <param name="responses">
    /// Returns the responses for the speaker it receives, said in order.
    /// It is called on the game's thread each time the line's phase begins, after <see cref="IQuestSceneEvents.OnPhaseBegin"/> is raised for that phase.
    /// The line says nothing if it returns no response, throws, or returns a response whose voice file does not exist. The last two are logged.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="speaker"/> or <paramref name="responses"/> is <c>null</c>.</exception>
    public SceneLine(QuestReferenceAlias speaker, Func<Actor, IEnumerable<TopicResponse>> responses) : base(speaker)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(responses);
        Speaker = speaker;
        Variants = [[]];
        ComputeResponses = responses;
    }

    private static TopicResponse[] GetResponses(QuestReferenceAlias speaker, IEnumerable<TopicResponse> responses, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        ArgumentNullException.ThrowIfNull(responses, parameterName);
        TopicResponse[] responseArray = [.. responses];
        if (responseArray.Length == 0)
        {
            throw new ArgumentException($"A scene line said by the alias \"{speaker.Name}\" has a variant with no response.", parameterName);
        }
        return responseArray;
    }

    /// <summary>The alias whose actor says the line.</summary>
    public QuestReferenceAlias Speaker { get; }

    /// <summary>The variants, each its responses in the order said. A line whose responses are computed has one variant with no response.</summary>
    public IReadOnlyList<IReadOnlyList<TopicResponse>> Variants { get; }

    internal Func<Actor, IEnumerable<TopicResponse>>? ComputeResponses { get; }

    /// <summary>Whether each variant is said only once in this save and every later one, until <see cref="IRuntimeQuest.RemoveFromJournalAndReset"/> resets its quest.</summary>
    /// <remarks>
    /// A random line chooses among the variants not yet said, and a line whose variants have all been said says nothing.
    /// The saved state identifies a variant by its scene and its responses' texts, so a changed text is said again.
    /// </remarks>
    /// <exception cref="ArgumentException">The line's responses are computed.</exception>
    public bool IsSaidOnce
    {
        get;
        init
        {
            if (value && ComputeResponses != null)
            {
                throw new ArgumentException(
                    $"A scene line said by the alias \"{Speaker.Name}\" computes its responses, so it cannot be said once. A line said once is recorded by its text.",
                    nameof(IsSaidOnce));
            }
            field = value;
        }
    }

    /// <summary>Returns whether the line is said. Null means the line is always said.</summary>
    /// <remarks>
    /// It runs on the game's thread on each frame the scene plays, and the game reads its last result one frame later.
    /// An exception from it is logged and counts as false.
    /// Of two lines of one speaker in one phase, the first ends as it begins if the condition of the second is false.
    /// To choose between two lines, put each in its own phase with a <see cref="ScenePhase.Condition"/>.
    /// </remarks>
    public Func<QuestScene, bool>? Condition { get; init; }

    /// <summary>
    /// The alias whose actor the speaker looks at while saying the line. The alias is an actor of the scene,
    /// so a scene whose alias is not filled with a living, loaded actor ends with <see cref="QuestSceneEndReason.ActorUnavailable"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="LooksAtPlayer"/> is set as well.</exception>
    public QuestReferenceAlias? LookTarget
    {
        get;
        init
        {
            if (value != null && LooksAtPlayer)
            {
                throw new ArgumentException("A scene line cannot set both LookTarget and LooksAtPlayer.", nameof(LookTarget));
            }
            field = value;
        }
    }

    /// <summary>Whether the speaker looks at the player while saying the line.</summary>
    /// <exception cref="ArgumentException"><see cref="LookTarget"/> is set as well.</exception>
    public bool LooksAtPlayer
    {
        get;
        init
        {
            if (value && LookTarget != null)
            {
                throw new ArgumentException("A scene line cannot set both LookTarget and LooksAtPlayer.", nameof(LooksAtPlayer));
            }
            field = value;
        }
    }

    /// <summary>Whether the speaker turns its body to face the target it looks at.</summary>
    /// <remarks>A speaker running a <see cref="LinkedRefPackage.Follow"/> action turns its head only.</remarks>
    public bool FacesTarget { get; init; }

    /// <summary>The repetition of the line. Null means the line is not repeated.</summary>
    public SceneLineLoop? Loop { get; init; }

    internal override IEnumerable<QuestReferenceAlias> Actors => LookTarget is { } lookTarget ? [Speaker, lookTarget] : [Speaker];

    internal override IEnumerable<QuestReferenceAlias> Aliases => Actors;
}
