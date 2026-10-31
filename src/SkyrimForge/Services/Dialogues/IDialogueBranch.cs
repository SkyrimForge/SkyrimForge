using SkyrimForge.Game;
using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A dialogue branch created by <see cref="IDialogueBranchFactory"/>.</summary>
/// <remarks>Call members on the game's thread.</remarks>
public interface IDialogueBranch
{
    /// <summary>Shows the branch in conversations with <paramref name="speaker"/> only, replacing any earlier speaker.</summary>
    /// <remarks>
    /// A call during a conversation with the speaker shows the topics after the next reply. A save starting to load or a new game beginning hides the branch.
    /// The branch clears its speaker in its own <see cref="IGameEvents.OnSaveLoaded"/> handler, subscribed when the branch is created,
    /// so subscribe a handler that calls this after creating the branch.
    /// </remarks>
    void Show<TSpeaker>(TSpeaker speaker) where TSpeaker : IActor;

    /// <summary>Shows the branch in conversations with the actor <paramref name="getSpeaker"/> returns, and hides the branch while it returns null.</summary>
    /// <remarks>
    /// <paramref name="getSpeaker"/> is called on each frame while no conversation with the current speaker is open.
    /// The function replaces any earlier speaker, and remains in place when a save starts loading or a new game begins.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="getSpeaker"/> is <c>null</c>.</exception>
    void Show(Func<Actor?> getSpeaker);

    /// <summary>
    /// Shows the branch in conversations with the actor that fills <paramref name="speaker"/>,
    /// and hides the branch while the alias is empty or <paramref name="condition"/> returns false.
    /// </summary>
    /// <remarks>
    /// <paramref name="condition"/> is called on each frame while no conversation with the current speaker is open.
    /// The alias replaces any earlier speaker, and remains in place when a save starts loading or a new game begins.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="speaker"/> is <c>null</c>.</exception>
    void Show(QuestReferenceAlias speaker, Func<bool>? condition = null);

    /// <summary>Shows the branch in conversations with each actor <paramref name="speakerFilter"/> accepts, one conversation at a time.</summary>
    /// <remarks>
    /// <paramref name="speakerFilter"/> is called for the actor under the crosshair at once, and then each time the crosshair lands on an actor,
    /// except during a conversation with the current speaker.
    /// It is also called for the current speaker on each frame the crosshair is on that actor, and once as a conversation ends.
    /// The filter replaces any earlier speaker, and remains in place when a save starts loading or a new game begins.
    /// A conversation the actor starts, such as a force greet, shows the branch only if the crosshair last landed on that actor.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="speakerFilter"/> is <c>null</c>.</exception>
    void Show(Func<Actor, bool> speakerFilter);

    /// <summary>Hides every topic of the branch, after the next reply if a conversation is open.</summary>
    void Hide();

    /// <summary>Forgets every topic of the branch the player has chosen, with every speaker.</summary>
    /// <remarks>A topic whose <see cref="DialogueTopic.IsSaidOnce"/> is true is shown again once no conversation with the speaker is open.</remarks>
    void ForgetSaidTopics();

    /// <summary>The topics of the branch the player chose in the open conversation, or in the last one once it has ended, in the order chosen.</summary>
    /// <remarks>It is emptied as a conversation with the speaker opens, and as a save starts loading or a new game begins.</remarks>
    IReadOnlyList<DialogueTopic> ChosenTopics { get; }

    /// <summary>Raised on the game's thread once a conversation in which the branch showed a topic ends.</summary>
    IGameEvent<ConversationEndEventArgs> OnConversationEnd { get; }
}
