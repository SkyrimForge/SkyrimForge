namespace SkyrimForge;

/// <summary>Arguments for <see cref="ICalendarTimer.OnElapsed"/>.</summary>
/// <param name="ElapsedPeriodCount">The number of the timer's periods that have ended since the event was last raised. The value is 1 for a timer that does not repeat.</param>
public readonly record struct CalendarTimerElapsedEventArgs(int ElapsedPeriodCount);
