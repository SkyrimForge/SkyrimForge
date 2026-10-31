using SkyrimForge.Game;
using System;
using System.Threading;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Queues work from the plugin's own thread through both queues of <see cref="ITaskQueue"/> when Shift+F4 is pressed.</summary>
/// <remarks>
/// A plugin's own thread must not call the game. Work queued through <see cref="ITaskQueue"/> runs on a thread SKSE chooses while the game is stopped for it,
/// so it may call the game.
/// </remarks>
public sealed class TaskPlugin(IGameEvents events, ITaskQueue taskQueue, IDebug debug, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF4, ModifierKeys.Shift)
            .Subscribe(_ => new Thread(QueueWork) { IsBackground = true, Name = "SkyrimForge Task Example" }.Start());
        log.Write("Shift+F4 queues work from a background thread.");
    }

    private void QueueWork()
    {
        int workerThreadId = Environment.CurrentManagedThreadId;
        taskQueue.AddTask(() => Report(nameof(ITaskQueue.AddTask), workerThreadId));
        taskQueue.AddUITask(() => Report(nameof(ITaskQueue.AddUITask), workerThreadId));
    }

    private void Report(string queue, int workerThreadId)
    {
        log.Write($"{queue} work queued on thread {workerThreadId} ran on thread {Environment.CurrentManagedThreadId}.");
        debug.Notification($"SkyrimForge: {queue} work ran.");
    }
}
