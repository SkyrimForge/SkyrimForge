using SkyrimForge.Game;
using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>Creates dialogue branches at runtime, without an .esp file, from <see cref="DialogueTopic"/> trees.</summary>
/// <remarks>
/// Topics of several branches shown for one speaker appear in no set order.
/// An exception thrown by a function given to a branch reaches the caller of IDialogueBranch.Show when that call runs the function.
/// Elsewhere it is logged and ends the handler that ran it, such as the branch's update on each frame or its tracking of the crosshair.
/// Call members on the game's thread.
/// </remarks>
public interface IDialogueBranchFactory
{
    /// <summary>Creates a hidden branch.</summary>
    /// <param name="key">
    /// The name under which the topics the player has chosen are stored. Use a name unique among this plugin's branches and the same in every version of the plugin.
    /// </param>
    /// <param name="startingTopic">The topic the branch starts with.</param>
    /// <param name="greeting">
    /// Returns the responses that replace the speaker's greeting as a conversation opens while the branch shows its starting topic.
    /// An empty result makes the speaker say nothing. It is called on each frame with no conversation open. A null value leaves the game's greeting.
    /// </param>
    /// <param name="goodbye">
    /// Returns the responses that replace the speaker's line as the player leaves a conversation without choosing a topic whose <see cref="DialogueTopic.IsGoodbye"/> is true.
    /// An empty result makes the speaker say nothing. It is called on each frame the branch shows a topic for the speaker. A null value leaves the game's goodbye.
    /// </param>
    /// <param name="resumes">
    /// Whether a new conversation with the speaker of the last topic chosen shows that topic's choices in place of the starting topic, with no greeting.
    /// Choosing a goodbye, starting a new game, and loading a save clear that topic.
    /// </param>
    /// <param name="questId">
    /// The ID of the quest that lists the topics, from an .esm, .esp, or .esl file. Null means DialogueGeneric.
    /// If no quest has the ID once the game has loaded its data, the topics have no records, and the log states the reason.
    /// To show the branch for an alias of a runtime quest, pass the alias to <see cref="IDialogueBranch.Show(QuestReferenceAlias, Func{bool})"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// This plugin created a branch with <paramref name="key"/> already, or the branch shows more than 50 goodbye topics or 50 other topics at once.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">The voice file of a fixed response does not exist.</exception>
    /// <remarks>
    /// Callable from a plugin's start, before the game has loaded its data. The game never frees a branch's records, so create each branch once.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="startingTopic"/> is <c>null</c>.</exception>
    IDialogueBranch Create(string key, DialogueTopic startingTopic, Func<Actor, IEnumerable<TopicResponse>>? greeting = null, Func<Actor, IEnumerable<TopicResponse>>? goodbye = null, bool resumes = false, FormId<Quest>? questId = null);
}
