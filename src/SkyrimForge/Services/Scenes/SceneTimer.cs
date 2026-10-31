using System;
using System.Collections.Generic;

namespace SkyrimForge;

/// <summary>A timer action of a <see cref="ScenePhase"/>, which does not end before the timer has elapsed.</summary>
public sealed record SceneTimer : SceneAction
{
    /// <summary>Creates a timer.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is not positive.</exception>
    public SceneTimer(TimeSpan duration) : base((QuestReferenceAlias?)null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
        Duration = duration;
    }

    /// <summary>The time the timer runs.</summary>
    public TimeSpan Duration { get; }

    internal override IEnumerable<QuestReferenceAlias> Aliases => [];
}
