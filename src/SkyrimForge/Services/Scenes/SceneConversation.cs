using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A conversation the actor filling an alias opens with the player in a <see cref="ScenePhase"/>.</summary>
/// <remarks>
/// The actor walks to the player and greets, the player chooses among the topics of a dialogue branch, and the phase ends once that conversation has ended.
/// The branch is shown for the actor as the phase begins, replacing any speaker it was shown for, and hidden as the phase ends.
/// The next phase begins in the frame the conversation closes, so a goodbye the actor says as the player leaves is said over the first line of that phase.
/// A branch created with a goodbye of no responses makes the actor say none.
/// A later phase's <see cref="ScenePhase.Condition"/> reads the choice from the branch's <see cref="IDialogueBranch.ChosenTopics"/>.
/// The scene ends with <see cref="QuestSceneEndReason.Interrupted"/> as the conversation opens if the actor's <see cref="SceneActorSettings.DialogueInterruption"/>
/// is <see cref="SceneInterruption.End"/>.
/// </remarks>
public sealed record SceneConversation : SceneAction
{
    /// <summary>Creates a conversation.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> or <paramref name="branch"/> is <c>null</c>.</exception>
    public SceneConversation(QuestReferenceAlias actor, IDialogueBranch branch) : base(actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(branch);
        Actor = actor;
        Branch = branch;
    }

    /// <summary>The alias whose actor opens the conversation.</summary>
    public QuestReferenceAlias Actor { get; }

    /// <summary>The dialogue branch whose topics the player chooses among.</summary>
    public IDialogueBranch Branch { get; }

    internal override IEnumerable<QuestReferenceAlias> Aliases => [Actor];
}
