using System;

namespace SkyrimForge;

/// <summary>Creates timers on the game's calendar that are saved with the game.</summary>
/// <remarks>A running timer is due at the same calendar time after a save is loaded. Call members on the game's thread.</remarks>
public interface ICalendarTimerFactory
{
    /// <summary>Creates a timer, which runs if the loaded game stored it as running.</summary>
    /// <param name="key">The saved-state name of the timer, unique among this plugin's timers and the same in every version of the plugin.</param>
    /// <exception cref="ArgumentException">This plugin created a timer with <paramref name="key"/> already.</exception>
    /// <remarks>Callable from a plugin's start, before the game has data or a save.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    ICalendarTimer Create(string key);
}
