using System;

namespace SkyrimForge.Game;

internal sealed record SavedCalendarTimer(TimeSpan DueTime, TimeSpan? Interval);
