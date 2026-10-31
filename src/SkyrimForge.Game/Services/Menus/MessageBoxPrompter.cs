using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class MessageBoxPrompter(ILog log, ITaskQueue taskQueue) : IMessageBoxPrompter
{
    private static readonly ConcurrentDictionary<int, PendingPrompt> pendingPrompts = new();
    private static int nextToken;
    private static bool installed;
    private static readonly Lock installLock = new();

    private sealed record PendingPrompt(GameTaskCompletionSource<int> TaskCompletionSource, ILog Log);

    private readonly ILog log = log;
    private readonly ITaskQueue taskQueue = taskQueue;

    public unsafe Task<int> ShowAsync(string message, IReadOnlyList<string> buttons, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(buttons);
        if (buttons.Count == 0 || buttons.Any(button => button == null))
        {
            throw new ArgumentException("A message box needs at least one button, and no button may be null.", nameof(buttons));
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        lock (installLock)
        {
            if (!installed)
            {
                NativeSupplements.SF_MessageBoxData_SetCallback(&OnClosed);
                installed = true;
            }
        }

        int token = Interlocked.Increment(ref nextToken);
        var pendingPrompt = new PendingPrompt(new GameTaskCompletionSource<int>(taskQueue), log);
        // Registered before QueueMessage, which can report a press before it returns.
        pendingPrompts[token] = pendingPrompt;

        int queueResult = NativeSupplements.SF_MessageBoxData_QueueMessage(message, token, [.. buttons], buttons.Count);
        if (queueResult == 0)
        {
            CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (pendingPrompts.TryRemove(token, out PendingPrompt? canceledTask))
                {
                    canceledTask.TaskCompletionSource.SetCanceled();
                }
            });
            pendingPrompt.TaskCompletionSource.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default).LogFault(log);
            return pendingPrompt.TaskCompletionSource.Task;
        }

        pendingPrompts.TryRemove(token, out _);
        string failure = queueResult switch
        {
            1 => "The native call rejected the message box arguments.",
            2 => "The game has no message box factory.",
            3 => "The address library has no ID for MessageBoxData::QueueMessage.",
            4 => "Queueing the message box faulted. Its data and callback are not freed.",
            _ => $"Queueing the message box returned the unrecognized status {queueResult}."
        };
        return Task.FromException<int>(new InvalidOperationException(failure));
    }

    [UnmanagedCallersOnly]
    private static void OnClosed(int token, uint button, byte wasButtonChosen)
    {
        if (!pendingPrompts.TryRemove(token, out PendingPrompt? pendingPrompt))
        {
            return;
        }

        try
        {
            if (wasButtonChosen != 0)
            {
                pendingPrompt.TaskCompletionSource.SetResult((int)button);
                return;
            }

            pendingPrompt.TaskCompletionSource.SetCanceled();
        }
        catch (Exception ex)
        {
            pendingPrompt.Log.Write($"Completing the task of a closed message box threw: {ex}");
        }
    }
}

internal static unsafe partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_MessageBoxData_SetCallback(delegate* unmanaged<int, uint, byte, void> handler);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_MessageBoxData_QueueMessage(string message, int token, [In] string[] buttonTexts, int count);
}
