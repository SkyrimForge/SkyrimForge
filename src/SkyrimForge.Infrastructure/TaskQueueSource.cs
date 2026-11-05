using SkyrimForge.Events;
using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Infrastructure;

internal static class TaskQueueSource
{
    private static readonly TokenRegistry<HandlerRegistration<Action>> tasksByToken = new();

    public static unsafe void Install(ILog hostLog)
    {
        NativeGlobals.SF_Task_SetHandler(&OnRun);
        if (!NativeGlobals.SF_Task_IsAvailable())
        {
            hostLog.Write("SKSE provides no task interface. Queued tasks will not run.");
        }
    }

    internal static ITaskQueue CreateLogTaskQueue(ILog log) => new LogTaskQueue(log);

    private static void Add(Action work, bool isUITask, ILog log)
    {
        ArgumentNullException.ThrowIfNull(work);

        // Recorded before the token is queued, because SKSE may run it at once.
        int token = tasksByToken.Add(new HandlerRegistration<Action>(work, log));
        if (isUITask ? NativeGlobals.SF_Task_AddUI((ulong)token) : NativeGlobals.SF_Task_Add((ulong)token))
        {
            return;
        }

        tasksByToken.TryRemove(token, out _);
        log.Write($"{(isUITask ? nameof(ITaskQueue.AddUITask) : nameof(ITaskQueue.AddTask))} could not queue the work. It was dropped.");
    }

    [UnmanagedCallersOnly]
    private static void OnRun(ulong token)
    {
        if (tasksByToken.TryRemove((int)token, out HandlerRegistration<Action>? job))
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

    private sealed class LogTaskQueue(ILog log) : ITaskQueue
    {
        /// <inheritdoc/>
        public void AddTask(Action work) => Add(work, false, log);

        /// <inheritdoc/>
        public void AddUITask(Action work) => Add(work, true, log);
    }
}
