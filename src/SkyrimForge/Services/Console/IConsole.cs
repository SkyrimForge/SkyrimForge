namespace SkyrimForge;

/// <summary>Writes to the game's console.</summary>
/// <remarks>
/// A write made before the game builds its console is dropped. The console exists by the first <see cref="IGameEvents.OnFrame"/>.
/// Call members on the game's thread.
/// </remarks>
public interface IConsole
{
    /// <summary>Writes one line to the console.</summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
    void Write(string message);
}
