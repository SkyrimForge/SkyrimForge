using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Arguments for <see cref="IDialogueBranch.OnConversationEnd"/>. <see cref="Speaker"/> is null if the game no longer has the actor.</summary>
public readonly record struct ConversationEndEventArgs(Actor? Speaker, ConversationEndReason Reason);
