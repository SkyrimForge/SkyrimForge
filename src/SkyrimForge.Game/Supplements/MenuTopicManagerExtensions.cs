namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="MenuTopicManager"/>.</summary>
public static class MenuTopicManagerExtensions
{
    extension(MenuTopicManager manager)
    {
        /// <summary>Whether the dialogue menu is open and its speaker is the reference <paramref name="speakerId"/> identifies.</summary>
        public bool IsConversationOpenWith<TReference>(FormId<TReference> speakerId) where TReference : struct, IObjectReference =>
            manager.MenuOpen && manager.Speaker?.FormId == speakerId;
    }
}
