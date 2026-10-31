namespace SkyrimForge;

/// <summary>The way a conversation that showed a dialogue branch ended.</summary>
public enum ConversationEndReason
{
    /// <summary>The player chose one of the branch's topics whose <see cref="DialogueTopic.IsGoodbye"/> is true, and it was the last topic chosen.</summary>
    Goodbye,

    /// <summary>The conversation closed another way, such as the player leaving it or combat interrupting it.</summary>
    Closed,

    /// <summary>A new game started or a save started loading.</summary>
    NewGameOrSaveLoading
}
