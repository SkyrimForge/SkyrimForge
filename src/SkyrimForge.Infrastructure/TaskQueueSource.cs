using SkyrimForge.Events;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Infrastructure;

internal sealed class TaskQueueSource
{
    // The native side receives a token, not the delegate. The delegate stays rooted here until its token runs.
    private readonly ConcurrentDictionary<ulong, HandlerRegistration<Action>> pendingJobs = new();

    private ulong lastToken;

    private static TaskQueueSource? instance;

    public unsafe void Install(ILog hostLog)
    {
        instance = this;
        NativeGlobals.SF_Task_SetHandler(&OnRun);
        if (!NativeGlobals.SF_Task_IsAvailable())
        {
            hostLog.Write("SKSE provides no task interface. Queued tasks will not run.");
        }
    }

    internal ITaskQueue CreateLogTaskQueue(ILog log) => new LogTaskQueue(this, log);

    private void Add(Action work, bool isUITask, ILog log)
    {
        ArgumentNullException.ThrowIfNull(work);

        // Recorded before the token is queued, because SKSE may run it at once.
        ulong token = Interlocked.Increment(ref lastToken);
        pendingJobs[token] = new HandlerRegistration<Action>(work, log);
        if (isUITask ? NativeGlobals.SF_Task_AddUI(token) : NativeGlobals.SF_Task_Add(token))
        {
            return;
        }

        pendingJobs.TryRemove(token, out _);
        log.Write($"{(isUITask ? nameof(ITaskQueue.AddUITask) : nameof(ITaskQueue.AddTask))} could not queue the work. It was dropped.");
    }

    [UnmanagedCallersOnly]
    private static void OnRun(ulong token)
    {
        if (instance != null && instance.pendingJobs.TryRemove(token, out HandlerRegistration<Action>? job))
        {
            try
            {
                job.Handler();
            }
            catch (Exception ex)
            {
                job.Log.Write($"Queued work threw and was dropped: {ex}");
            }
        }
    }

    private sealed class LogTaskQueue(TaskQueueSource queue, ILog log) : ITaskQueue
    {
        private readonly TaskQueueSource queue = queue;
        private readonly ILog log = log;

        /// <inheritdoc/>
        public void AddTask(Action work) => queue.Add(work, false, log);

        /// <inheritdoc/>
        public void AddUITask(Action work) => queue.Add(work, true, log);
    }
}
