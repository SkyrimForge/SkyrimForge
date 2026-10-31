using SkyrimForge.Game;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Shows dialogue lines that have no voice recording as subtitles under the speaker, with a timed mouth animation.</summary>
/// <remarks>
/// Outside a conversation, the speaker is given <see cref="KnownForms.zzzDEFAULTzzzWaitForDialogue"/> while speaking, so it stops and faces the player.
/// Call members on the game's thread.
/// </remarks>
public interface IUnvoicedSpeech
{
    /// <summary>
    /// Shows <paramref name="lines"/> in order under <paramref name="speaker"/>, interrupting any lines the speaker is already saying. With no lines, the call only interrupts.
    /// </summary>
    /// <remarks>Each line is shown for 0.045 seconds per character, from 2 to 12 total seconds.</remarks>
    /// <returns>
    /// A task that completes when the last line is removed.
    /// It is canceled if the lines are interrupted: by another call for the speaker, by <see cref="Stop{TSpeaker}"/>, or when a save starts loading or a new game begins.
    /// </returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
    Task SayAsync<TSpeaker>(TSpeaker speaker, params IEnumerable<string> lines) where TSpeaker : IActor;

    /// <summary>Interrupts the lines of <paramref name="speaker"/> and removes them from the screen.</summary>
    void Stop<TSpeaker>(TSpeaker speaker) where TSpeaker : IActor;
}
