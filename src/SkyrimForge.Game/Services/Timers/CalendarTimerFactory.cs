using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class CalendarTimerFactory : ICalendarTimerFactory
{
    private readonly IPerPluginSavedState savedState;
    private readonly ILog log;
    private readonly Func<TimeSpan> getTimePassed;
    private readonly List<CalendarTimer> timers = [];

    // Frames run at the main menu and during a load.
    private bool gameLoaded;

    private TimeSpan? nextDueTime;

    internal CalendarTimerFactory(IGameEvents events, IPerPluginSavedState savedState, ILog log, Func<TimeSpan> getTimePassed)
    {
        this.savedState = savedState;
        this.log = log;
        this.getTimePassed = getTimePassed;
        events.OnFrame.Subscribe(_ => Check());
        events.OnNewGameOrSaveLoading.Subscribe(_ => gameLoaded = false);
        events.OnNewGameOrSaveLoaded.Subscribe(_ => Load());
    }

    /// <inheritdoc/>
    public ICalendarTimer Create(string key)
    {
        const string keyPrefix = "SkyrimForge.CalendarTimer.";
        ArgumentNullException.ThrowIfNull(key);
        if (timers.Any(timer => timer.Key == key))
        {
            throw new ArgumentException($"This plugin already created a calendar timer with the key \"{key}\".", nameof(key));
        }
        var timer = new CalendarTimer(key, savedState.GetRecordPair<SavedCalendarTimer>(keyPrefix + key), getTimePassed, UpdateNextDueTime, log);
        timers.Add(timer);
        // A timer created in a loaded game can be running in it.
        if (gameLoaded)
        {
            UpdateNextDueTime();
        }
        return timer;
    }

    private void Load()
    {
        gameLoaded = true;
        UpdateNextDueTime();
    }

    private void UpdateNextDueTime() => nextDueTime = timers.Min(timer => timer.DueTime);

    private void Check()
    {
        if (!gameLoaded || nextDueTime is not { } dueTime)
        {
            return;
        }
        TimeSpan now = getTimePassed();
        if (now < dueTime)
        {
            return;
        }
        // Copied since a handler can create a timer.
        foreach (CalendarTimer timer in timers.ToArray())
        {
            timer.RaiseIfDue(now);
        }
        UpdateNextDueTime();
    }
}
