using System;

namespace SkyrimForge;

/// <summary>Queues work to run while the game is stopped for it. A plugin uses this to call the game from a thread of its own.</summary>
/// <remarks>
/// Each piece runs on a worker thread SKSE selects, never the game's thread,
/// so no thread-local value survives between pieces and a lock acquired in one cannot be released in the next.
/// Work that blocks until the game's thread runs never finishes, because the game's thread is waiting for that work.
/// BGSSaveLoadManager.Save on Skyrim VR can block this way, or return false, when called from queued work.
/// The game waits throughout, so long work costs frames.
/// Queued work never runs at the same time as code on the game's thread, and queued pieces, from either queue, run one at a time,
/// so a member documented as called on the game's thread may be called from queued work.
/// <para>
/// A handler on a worker thread can queue that call here, or run it after an await of <see cref="UtilityExtensions.WaitForGameThreadAsync"/>.
/// A task a framework member returns completes on the game's thread or in queued work, so the code after awaiting it may call a member documented as called on the game's thread.
/// The exception is <see cref="QuestStage.SetAsync"/>. For a stage with a <see cref="QuestStage.SetCallback"/>, the task completes when the task SetCallback returns completes.
/// </para>
/// </remarks>
public interface ITaskQueue
{
    /// <summary>Queues <paramref name="work"/> to run at the next point the game stops for it. An exception it throws is written to the log of the plugin that queued it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="work"/> is <c>null</c>.</exception>
    void AddTask(Action work);

    /// <summary>Queues <paramref name="work"/> to run while the game updates its interface. An exception it throws is written to the log of the plugin that queued it.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="work"/> is <c>null</c>.</exception>
    void AddUITask(Action work);
}
