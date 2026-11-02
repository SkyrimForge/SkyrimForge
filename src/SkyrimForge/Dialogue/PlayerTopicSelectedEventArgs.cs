using System.Threading.Tasks;

namespace SkyrimForge.Dialogue;

// ReplyEndTask completes on the game's thread.
internal readonly record struct PlayerTopicSelectedEventArgs(IPlayerTopic Topic, string Text, Task ReplyEndTask);
