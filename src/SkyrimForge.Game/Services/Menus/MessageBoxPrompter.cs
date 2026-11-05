using SkyrimForge.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class MessageBoxPrompter(ILog log, ITaskQueue taskQueue) : IMessageBoxPrompter
{
    private static readonly TokenRegistry<GameTaskCompletionSource<int>> promptsByToken = new();

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

        NativeSupplements.SF_MessageBoxData_SetCallback(&OnClosed);
        var taskCompletionSource = new GameTaskCompletionSource<int>(taskQueue);
        // Registered before QueueMessage, which can report a press before it returns.
        int token = promptsByToken.Add(taskCompletionSource);

        int queueResult = NativeSupplements.SF_MessageBoxData_QueueMessage(message, token, [.. buttons], buttons.Count);
        if (queueResult == 0)
        {
            CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (promptsByToken.TryRemove(token, out GameTaskCompletionSource<int>? canceledTask))
                {
                    canceledTask.SetCanceled();
                }
            });
            taskCompletionSource.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default).LogFault(log);
            return taskCompletionSource.Task;
        }

        promptsByToken.TryRemove(token, out _);
        string failure = queueResult switch
        {
            1 => "The game has no message box factory.",
            2 => "The address library has no ID for MessageBoxData::QueueMessage.",
            3 => "Queueing the message box faulted. Its data and callback are not freed.",
            _ => $"Queueing the message box returned the unrecognized status {queueResult}."
        };
        return Task.FromException<int>(new InvalidOperationException(failure));
    }

    [UnmanagedCallersOnly]
    private static void OnClosed(int token, uint button, byte wasButtonChosen)
    {
        if (!promptsByToken.TryRemove(token, out GameTaskCompletionSource<int>? taskCompletionSource))
        {
            return;
        }
        if (wasButtonChosen != 0)
        {
            taskCompletionSource.SetResult((int)button);
        }
        else
        {
            taskCompletionSource.SetCanceled();
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
