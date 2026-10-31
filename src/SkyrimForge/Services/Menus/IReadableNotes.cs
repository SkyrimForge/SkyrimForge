using SkyrimForge.Game;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Makes references readable notes whose pages a plugin writes at runtime.</summary>
/// <remarks>
/// The pages are stored in this plugin's saved state. Call members on the game's thread.
/// </remarks>
public interface IReadableNotes
{
    /// <summary>Makes <paramref name="note"/> a readable note named <paramref name="name"/>, whose pages are <paramref name="htmlPages"/>.</summary>
    /// <param name="note">The reference, such as one whose base object is <see cref="KnownForms.dunLabyrinthianMazeClue"/>.</param>
    /// <param name="name">The name the reference shows.</param>
    /// <param name="htmlPages">The pages as HTML, as <see cref="IBookMenuPresenter.ShowAsync"/> receives them. They replace the pages written before.</param>
    /// <param name="canBeTaken">Whether the player can take the note.</param>
    /// <remarks>The reference's activation is blocked, or unblocked for a note that can be taken.</remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="name"/> or <paramref name="htmlPages"/> is <c>null</c>.</exception>
    void Write<TNote>(TNote note, string name, IEnumerable<string> htmlPages, bool canBeTaken = false) where TNote : struct, IObjectReference;

    /// <summary>Sends the player a letter named <paramref name="name"/> by the game's courier, whose pages are <paramref name="htmlPages"/>.</summary>
    /// <param name="name">The name the letter shows.</param>
    /// <param name="htmlPages">The pages as HTML, as <see cref="IBookMenuPresenter.ShowAsync"/> receives them.</param>
    /// <returns>The letter, a reference whose base object is <see cref="KnownForms.dunLabyrinthianMazeClue"/> and which the player can take, once the courier has it.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="name"/> or <paramref name="htmlPages"/> is <c>null</c>.</exception>
    /// <exception cref="System.InvalidOperationException">The game created no letter. The exception is stored in the returned task.</exception>
    Task<ObjectReference> SendByCourierAsync(string name, IEnumerable<string> htmlPages);

    /// <summary>Raised on the game's thread when the book menu opens on the pages of a note this plugin wrote.</summary>
    IGameEvent<NoteReadEventArgs> OnRead { get; }
}
