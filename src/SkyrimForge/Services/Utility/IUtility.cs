using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Waits and cooldowns measured by <see cref="FrameEventArgs.UnpausedTime"/>.</summary>
public interface IUtility
{
    /// <summary>
    /// Waits for <paramref name="duration"/> of <see cref="FrameEventArgs.UnpausedTime"/>, which does not advance while a menu pauses the game.
    /// Zero or less completes on the next frame.
    /// </summary>
    /// <remarks>
    /// The task completes inside <see cref="IGameEvents.OnFrame"/>, so the continuation may call the game.
    /// Canceling the token, a new game, or a save load cancels the wait on the next frame.
    /// Do not block on the task on the game's thread.
    /// </remarks>
    Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>Creates an <see cref="ICooldown"/> for <paramref name="duration"/> of <see cref="FrameEventArgs.UnpausedTime"/> from each start.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is zero or less.</exception>
    ICooldown CreateCooldown(TimeSpan duration);
}
