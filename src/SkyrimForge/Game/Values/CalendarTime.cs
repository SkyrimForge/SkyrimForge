namespace SkyrimForge.Game;

/// <summary>
/// A date and time with the fields of the C runtime's tm, storing the game's calendar values rather than tm's.
/// An interval's time fills only <see cref="Minute"/> and <see cref="Hour"/>, and every other field is zero.
/// </summary>
/// <param name="Second">Zero.</param>
/// <param name="Minute">The minute of the hour.</param>
/// <param name="Hour">The hour of the day, from 0.</param>
/// <param name="DayOfMonth">The day of the month, from 1.</param>
/// <param name="Month">The month, as an index of <c>Calendar.Month</c>, where 0 is Morning Star.</param>
/// <param name="Year">The year of the era.</param>
/// <param name="DayOfWeek">The day of the week, as an index of <c>Calendar.Day</c>, where 0 is Sundas.</param>
/// <param name="DayOfYear">The day of the year, from 1. A CommonLib defect makes the value short by the previous month's length after Morning Star.</param>
/// <param name="DaylightSaving">Zero.</param>
public readonly record struct CalendarTime(int Second, int Minute, int Hour, int DayOfMonth, int Month, int Year, int DayOfWeek, int DayOfYear, int DaylightSaving);
