namespace SkyrimForge;

/// <summary>The files under the game's Data folder, whether loose or packed in a .bsa archive.</summary>
/// <remarks>
/// A path is relative to the Data folder, as in <c>Interface\Controls\PC\keyboard_english.txt</c>, and is not case-sensitive.
/// Either separator is accepted.
/// Call members on the game's thread.
/// </remarks>
public interface IGameFileSystem
{
    /// <summary>Whether the game finds a file at the path.</summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
    bool Exists(string path);

    /// <summary>Reads the entire file at the path.</summary>
    /// <exception cref="System.IO.FileNotFoundException">The game finds no file at the path.</exception>
    /// <exception cref="System.IO.IOException">The game found the file and reported an error reading it.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
    byte[] ReadAllBytes(string path);
}
