using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="Task"/>.</summary>
public static class TaskExtensions
{
    extension(Task task)
    {
        /// <summary>Writes the task's exception to <paramref name="log"/> when the task faults.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="log"/> is <c>null</c>.</exception>
        public void LogFault(ILog log)
        {
            ArgumentNullException.ThrowIfNull(log);
            task.OnFault(exception => log.Write($"An unawaited task faulted: {exception}"));
        }

        internal void OnFault(Action<Exception> exceptionHandler) =>
            _ = task.ContinueWith(
                faultedTask => exceptionHandler(faultedTask.Exception!.InnerExceptions is [Exception singleException] ? singleException : faultedTask.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }
}
