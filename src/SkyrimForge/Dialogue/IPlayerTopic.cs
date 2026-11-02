using SkyrimForge.Events;
using SkyrimForge.Game;

namespace SkyrimForge.Dialogue;

// Call members on the game's thread.
internal interface IPlayerTopic
{
    string Text { get; set; }

    IGameEvent<PlayerTopicSelectedEventArgs> OnSelected { get; }

    bool IsSaid { get; set; }

    void Show<TSpeaker>(TSpeaker speaker) where TSpeaker : IObjectReference;

    void Hide();
}
