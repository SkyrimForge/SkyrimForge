using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Infrastructure;

internal sealed class Utility : IUtility
{
    private readonly IGameSession session;
    private readonly Lock stateLock = new();

    private readonly List<PendingWait> waits = [];

    private TimeSpan elapsed;

    public Utility(IGameEvents events, IGameSession session)
    {
        this.session = session;
        events.OnFrame.Subscribe(OnFrame);
    }

    /// <inheritdoc/>
    public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        var newGameOrSaveLoadingToken = session.NewGameOrSaveLoading;
        var completionSource = new TaskCompletionSource();
        lock (stateLock)
        {
            waits.Add(new(elapsed + duration, completionSource, cancellationToken, newGameOrSaveLoadingToken));
        }
        return completionSource.Task;
    }

    /// <inheritdoc/>
    public ICooldown CreateCooldown(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        return new Cooldown(this, duration);
    }

    // Ending waits are collected before any completes, so a continuation that waits again is not ended in this pass.
    // Each wait is tested once, so a cancellation token canceled mid-pass cannot remove a wait without completing it.
    private void OnFrame(FrameEventArgs args)
    {
        List<PendingWait> endingWaits = [];
        lock (stateLock)
        {
            elapsed = args.UnpausedTime;
            waits.RemoveAll(wait =>
            {
                bool ends = wait.DueTime <= elapsed || wait.CancellationToken.IsCancellationRequested || wait.NewGameOrSaveLoading.IsCancellationRequested;
                if (ends)
                {
                    endingWaits.Add(wait);
                }
                return ends;
            });
        }
        foreach ((_, TaskCompletionSource completionSource, CancellationToken cancellationToken, CancellationToken newGameOrSaveLoadingToken) in endingWaits)
        {
            _ = cancellationToken.IsCancellationRequested ? completionSource.TrySetCanceled(cancellationToken)
                : newGameOrSaveLoadingToken.IsCancellationRequested ? completionSource.TrySetCanceled(newGameOrSaveLoadingToken)
                : completionSource.TrySetResult();
        }
    }

    private sealed class Cooldown(Utility utility, TimeSpan duration) : ICooldown
    {
        private TimeSpan dueTime;
        private CancellationToken newGameOrSaveLoadingToken;

        public bool IsActive
        {
            get
            {
                lock (utility.stateLock)
                {
                    return IsActiveLocked();
                }
            }
        }

        public bool TryStart()
        {
            var newGameOrSaveLoadingToken = utility.session.NewGameOrSaveLoading;
            lock (utility.stateLock)
            {
                if (IsActiveLocked())
                {
                    return false;
                }
                dueTime = utility.elapsed + duration;
                this.newGameOrSaveLoadingToken = newGameOrSaveLoadingToken;
                return true;
            }
        }

        // Called with stateLock held.
        private bool IsActiveLocked() => !newGameOrSaveLoadingToken.IsCancellationRequested && utility.elapsed < dueTime;
    }

    private sealed record PendingWait(TimeSpan DueTime, TaskCompletionSource CompletionSource, CancellationToken CancellationToken, CancellationToken NewGameOrSaveLoading);
}
