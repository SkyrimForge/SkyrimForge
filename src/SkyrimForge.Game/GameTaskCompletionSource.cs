using System;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

// The completion is queued as ITaskQueue work and continuations run inline, so the code after an await may call the game.
internal sealed class GameTaskCompletionSource<T>(ITaskQueue taskQueue)
{
    private readonly ITaskQueue taskQueue = taskQueue;
    private readonly TaskCompletionSource<T> source = new();

    internal Task<T> Task => source.Task;

    internal void SetResult(T value)
    {
        taskQueue.AddTask(() => source.TrySetResult(value));
    }

    internal void SetException(Exception failure)
    {
        taskQueue.AddTask(() => source.TrySetException(failure));
    }

    internal void SetCanceled()
    {
        taskQueue.AddTask(() => source.TrySetCanceled());
    }
}
