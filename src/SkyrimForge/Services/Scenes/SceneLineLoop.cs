using System;

namespace SkyrimForge;

/// <summary>
/// The repetition of a <see cref="SceneLine"/>, said again after a delay from <see cref="MinimumDelay"/> to <see cref="MaximumDelay"/> until its phase ends.
/// </summary>
public sealed class SceneLineLoop
{
    /// <summary>Creates a repetition.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="minimumDelay"/> is negative, or <paramref name="maximumDelay"/> is less than <paramref name="minimumDelay"/>.
    /// </exception>
    public SceneLineLoop(TimeSpan minimumDelay, TimeSpan maximumDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumDelay, minimumDelay);
        MinimumDelay = minimumDelay;
        MaximumDelay = maximumDelay;
    }

    /// <summary>The shortest delay before the line is said again.</summary>
    public TimeSpan MinimumDelay { get; }

    /// <summary>The longest delay before the line is said again.</summary>
    public TimeSpan MaximumDelay { get; }
}
