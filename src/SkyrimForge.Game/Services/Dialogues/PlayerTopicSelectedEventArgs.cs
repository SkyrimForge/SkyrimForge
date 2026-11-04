using System.Threading.Tasks;

namespace SkyrimForge.Game;

// ReplyEndTask completes on the game's thread.
internal readonly record struct PlayerTopicSelectedEventArgs(PlayerTopic Topic, string Text, Task ReplyEndTask);
