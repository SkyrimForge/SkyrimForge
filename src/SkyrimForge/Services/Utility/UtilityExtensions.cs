using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="IUtility"/>.</summary>
public static class UtilityExtensions
{
    extension(IUtility utility)
    {
        /// <summary>Waits for the next frame, so the code after the await runs on the game's thread. A new game or a save load cancels the wait.</summary>
        public Task WaitForGameThreadAsync(CancellationToken cancellationToken = default) => utility.WaitAsync(TimeSpan.Zero, cancellationToken);

        /// <summary>
        /// Waits for <paramref name="seconds"/> of <see cref="FrameEventArgs.UnpausedTime"/>. Zero or less completes on the next frame. A new game or a save load cancels the wait.
        /// </summary>
        public Task WaitSecondsAsync(float seconds, CancellationToken cancellationToken = default) => utility.WaitAsync(TimeSpan.FromSeconds(seconds), cancellationToken);

        /// <summary>Waits until <paramref name="condition"/> returns true, testing it at the call and then once a frame.</summary>
        /// <param name="condition">Returns whether the wait is over. It runs on the game's thread.</param>
        /// <param name="timeout">
        /// The span of <see cref="FrameEventArgs.UnpausedTime"/> after which the wait ends, or <see cref="Timeout.InfiniteTimeSpan"/> for no limit.
        /// Zero ends it on the next frame.
        /// </param>
        /// <param name="cancellationToken">Cancels the wait on the next frame. A new game or a save load also cancels it.</param>
        /// <returns>A task whose result is true once <paramref name="condition"/> returns true, or false if <paramref name="timeout"/> passes first.</returns>
        /// <remarks>Subscribe to an event instead where the game raises one for what is awaited. Call on the game's thread.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative and not <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <c>null</c>.</exception>
        public Task<bool> WaitUntilAsync(Func<bool> condition, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(condition);
            if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The timeout is negative and not Timeout.InfiniteTimeSpan.");
            }
            return Core();

            async Task<bool> Core()
            {
                cancellationToken.ThrowIfCancellationRequested();
                Task? timeoutTask = timeout == Timeout.InfiniteTimeSpan ? null : utility.WaitAsync(timeout, cancellationToken);
                while (!condition())
                {
                    if (timeoutTask is { IsCompleted: true })
                    {
                        // Awaited so a wait canceled by the token or a load throws rather than reporting a timeout.
                        await timeoutTask;
                        return false;
                    }
                    await utility.WaitAsync(TimeSpan.Zero, cancellationToken);
                }
                return true;
            }
        }
    }
}
