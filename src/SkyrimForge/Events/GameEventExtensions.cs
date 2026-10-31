using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="IGameEvent{TArgs}"/>.</summary>
public static class GameEventExtensions
{
    extension<TArgs>(IGameEvent<TArgs> gameEvent) where TArgs : struct
    {
        /// <summary>Returns an event raised as this one is, for the arguments <paramref name="predicate"/> accepts.</summary>
        /// <remarks><paramref name="predicate"/> runs for each handler, on the thread that raises the event. If it throws, the event drops that handler.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
        public IGameEvent<TArgs> Where(Func<TArgs, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return new FilteredGameEvent<TArgs>(gameEvent, predicate);
        }

        /// <summary>Waits for the next raise of the event and returns its arguments. The code after the await runs on the thread that raises the event, during the raise.</summary>
        /// <remarks>
        /// A new game or a save load does not end the wait, unlike an <see cref="IUtility"/> wait.
        /// To end it then, pass <see cref="IGameSession.NewGameOrSaveLoading"/> as <paramref name="cancellationToken"/>.
        /// </remarks>
        public Task<TArgs> WaitForNextAsync(CancellationToken cancellationToken = default) => gameEvent.WaitForNextAsync(static _ => true, cancellationToken);

        /// <summary>
        /// Waits for the next raise of the event whose arguments <paramref name="predicate"/> accepts and returns them,
        /// or faults with the exception <paramref name="predicate"/> throws.
        /// <paramref name="predicate"/> and the code after the await run on the thread that raises the event, during the raise.
        /// </summary>
        /// <inheritdoc cref="extension{TArgs}(IGameEvent{TArgs}).WaitForNextAsync(CancellationToken)" path="/remarks"/>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <c>null</c>.</exception>
        public Task<TArgs> WaitForNextAsync(Func<TArgs, bool> predicate, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return Core();

            async Task<TArgs> Core()
            {
                var completionSource = new TaskCompletionSource<TArgs>();
                ISubscription subscription = gameEvent.Subscribe(args =>
                {
                    // Stored in the task rather than thrown, since the event drops a handler that throws, and the wait would then never end.
                    try
                    {
                        if (predicate(args))
                        {
                            completionSource.TrySetResult(args);
                        }
                    }
                    catch (Exception ex)
                    {
                        completionSource.TrySetException(ex);
                    }
                });
                try
                {
                    using (cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken)))
                    {
                        return await completionSource.Task;
                    }
                }
                finally
                {
                    subscription.Unsubscribe();
                }
            }
        }
    }

    extension(IGameEvent<FrameEventArgs> frames)
    {
        /// <summary>
        /// Registers <paramref name="handler"/> to run each time <see cref="FrameEventArgs.UnpausedTime"/> advances by <paramref name="interval"/>.
        /// It receives the unpaused time since the previous run, which is <paramref name="interval"/> or more.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is zero or negative.</exception>
        public ISubscription SubscribeEvery(TimeSpan interval, Action<TimeSpan> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            Func<FrameEventArgs, TimeSpan?> isDue = GetDueFunc(interval);
            return frames.Subscribe(args =>
            {
                if (isDue(args) is { } elapsedTime)
                {
                    handler(elapsedTime);
                }
            });
        }

        /// <inheritdoc cref="SubscribeEvery(IGameEvent{FrameEventArgs}, TimeSpan, Action{TimeSpan})"/>
        /// <remarks>An interval that completes while the previous run is in progress starts no run.</remarks>
        public ISubscription SubscribeEvery(TimeSpan interval, Func<TimeSpan, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            Func<FrameEventArgs, TimeSpan?> isDue = GetDueFunc(interval);
            Task runningTask = Task.CompletedTask;
            return frames.Subscribe(args =>
            {
                // The running task is tested first, since isDue starts a new interval each time it reports one due.
                if (!runningTask.IsCompleted || isDue(args) is not { } elapsedTime)
                {
                    return Task.CompletedTask;
                }
                runningTask = handler(elapsedTime);
                return runningTask;
            });
        }
    }

    private static Func<FrameEventArgs, TimeSpan?> GetDueFunc(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        TimeSpan? lastRun = null;
        return args =>
        {
            lastRun ??= args.UnpausedTime;
            TimeSpan elapsed = args.UnpausedTime - lastRun.Value;
            if (elapsed < interval)
            {
                return null;
            }
            lastRun = args.UnpausedTime;
            return elapsed;
        };
    }
}
