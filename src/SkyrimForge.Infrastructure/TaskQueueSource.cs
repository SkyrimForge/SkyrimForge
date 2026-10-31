using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Infrastructure;

internal sealed class TaskQueueSource
{
    // The native side receives a token, not the delegate. The delegate stays rooted here until its token runs.
    private readonly ConcurrentDictionary<ulong, Job> pendingJobs = new();

    private ulong lastToken;

    private static TaskQueueSource? instance;

    public static unsafe void Install(TaskQueueSource queue, ILog hostLog)
    {
        instance = queue;
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
        pendingJobs[token] = new Job(work, log);
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
        if (instance != null && instance.pendingJobs.TryRemove(token, out Job? job))
        {
            RunWithTryCatch(job);
        }
    }

    private static void RunWithTryCatch(Job job)
    {
        try
        {
            job.Run();
        }
        catch (Exception ex)
        {
            job.Log.Write($"Queued work threw and was dropped: {ex}");
        }
    }

    private sealed class Job(Action run, ILog log)
    {
        internal Action Run { get; } = run;

        internal ILog Log { get; } = log;
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
