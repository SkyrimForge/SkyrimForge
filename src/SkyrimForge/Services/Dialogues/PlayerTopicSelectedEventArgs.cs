using System.Threading.Tasks;

namespace SkyrimForge;

// ReplyEndTask completes on the game's thread.
internal readonly record struct PlayerTopicSelectedEventArgs(IPlayerTopic Topic, string Text, Task ReplyEndTask);
