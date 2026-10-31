using System;

namespace SkyrimForge;

/// <summary>Arguments for <see cref="IGameEvents.OnFrame"/>.</summary>
/// <param name="Interval">
/// How long the last frame took, from the game's GetSecondsSinceLastFrame.
/// A running total measures the time the game advanced, not elapsed time, and does not advance while a message box is open.
/// </param>
/// <param name="UnpausedTime">The real time between frames, summed over the frames on which the game is not paused, with each frame counted as at most 0.25 seconds.</param>
public readonly record struct FrameEventArgs(TimeSpan Interval, TimeSpan UnpausedTime);
