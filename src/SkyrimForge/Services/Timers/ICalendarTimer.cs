using System;

namespace SkyrimForge;

/// <summary>A timer on the game's calendar that <see cref="ICalendarTimerFactory"/> creates.</summary>
/// <remarks>Call members on the game's thread with a game loaded, except when subscribing to an event.</remarks>
public interface ICalendarTimer
{
    /// <summary>Starts or restarts the timer, which is due once <paramref name="dueTime"/> has passed on the game's calendar.</summary>
    /// <param name="dueTime">The calendar time until the timer is first due.</param>
    /// <param name="period">The calendar time from one due time to the next. A null value stops the timer once it is due.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dueTime"/> or <paramref name="period"/> is zero or negative.</exception>
    void Start(TimeSpan dueTime, TimeSpan? period = null);

    /// <summary>Stops the timer if it is running.</summary>
    void Stop();

    /// <summary>Whether the timer runs in the loaded game.</summary>
    bool IsRunning { get; }

    /// <summary>Raised on the game's thread on the first frame at which the timer is due.</summary>
    /// <remarks>
    /// A timer that does not repeat is stopped before the event is raised, so a handler can start it again.
    /// A repeating timer raises it once on that frame, with the number of its periods that have ended.
    /// </remarks>
    IGameEvent<CalendarTimerElapsedEventArgs> OnElapsed { get; }
}
