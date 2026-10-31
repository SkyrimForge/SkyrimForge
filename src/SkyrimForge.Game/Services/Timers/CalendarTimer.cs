using System;

namespace SkyrimForge.Game;

internal sealed class CalendarTimer(string key, ISavedStateRecordPair<SavedCalendarTimer> timerPair, Func<TimeSpan> getTimePassed, Action dueChanged, ILog log) : ICalendarTimer
{
    private readonly GameEvent<CalendarTimerElapsedEventArgs> onElapsed = new(ex => log.Write($"A subscriber to the OnElapsed of the calendar timer \"{key}\" threw: {ex}"));

    internal string Key { get; } = key;

    internal TimeSpan? DueTime => timerPair.Value?.DueTime;

    /// <inheritdoc/>
    public bool IsRunning => timerPair.Value != null;

    /// <inheritdoc/>
    public IGameEvent<CalendarTimerElapsedEventArgs> OnElapsed => onElapsed;

    /// <inheritdoc/>
    public void Start(TimeSpan dueTime, TimeSpan? period = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dueTime, TimeSpan.Zero);
        if (period is { } periodValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(periodValue, TimeSpan.Zero, nameof(period));
        }
        timerPair.Value = new SavedCalendarTimer(getTimePassed() + dueTime, period);
        dueChanged();
    }

    /// <inheritdoc/>
    public void Stop()
    {
        timerPair.Value = null;
        dueChanged();
    }

    internal void RaiseIfDue(TimeSpan now)
    {
        if (timerPair.Value is not { } savedTimer || now < savedTimer.DueTime)
        {
            return;
        }
        int elapsedPeriodCount = 1;
        if (savedTimer.Interval is { } interval)
        {
            elapsedPeriodCount += (int)((now - savedTimer.DueTime) / interval);
            timerPair.Value = savedTimer with { DueTime = savedTimer.DueTime + (interval * elapsedPeriodCount) };
        }
        else
        {
            timerPair.Value = null;
        }
        onElapsed.Raise(new CalendarTimerElapsedEventArgs(elapsedPeriodCount));
    }
}
