using System;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="Calendar"/>.</summary>
public static class CalendarExtensions
{
    extension(Calendar calendar)
    {
        /// <summary>The whole days since the game began, counting the first day as zero.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public int WholeDaysPassed => (int)calendar.GetDaysPassed();

        /// <summary>The calendar time since the game began.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public TimeSpan TimePassed => TimeSpan.FromHours(calendar.GetHoursPassed());

        /// <summary>The calendar time since the last midnight.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public TimeSpan TimeOfDay => TimeSpan.FromHours(calendar.GetHour());
    }
}
