using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Runs the console commands the game declares, and reports the arguments one of them declares.</summary>
/// <remarks>Call members on the game's thread.</remarks>
public interface IConsoleCommands
{
    /// <summary>Determines whether the game has a console command with the long name <paramref name="name"/>.</summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    bool Exists(string name);

    /// <summary>
    /// Reads how many arguments the command with the long name <paramref name="name"/> declares, or <c>null</c> if the game has no such console command.
    /// An argument may be optional, so read <see cref="GetRequiredArgumentCount"/> to determine whether <see cref="Run(string)"/> accepts the command.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    int? GetArgumentCount(string name);

    /// <summary>
    /// Reads how many arguments the command with the long name <paramref name="name"/> requires, or <c>null</c> if the game has no such console command.
    /// <see cref="Run(string)"/> throws for any count but zero.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    int? GetRequiredArgumentCount(string name);

    /// <summary>
    /// Runs the command with the long name <paramref name="name"/> with no reference selected.
    /// </summary>
    /// <exception cref="System.ArgumentException">
    /// The game has no such console command, the command requires an argument, or the command declares that it runs on a reference. The command did not run.
    /// </exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    /// <exception cref="System.InvalidOperationException">The command ran and reported failure.</exception>
    void Run(string name);

    /// <summary>
    /// Runs the command with the long name <paramref name="name"/> on <paramref name="reference"/>.
    /// </summary>
    /// <exception cref="System.ArgumentException">The game has no such console command, or the command requires an argument. The command did not run.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
    /// <exception cref="System.InvalidOperationException">The command ran and reported failure.</exception>
    void Run<TObjectReference>(TObjectReference reference, string name) where TObjectReference : IObjectReference;

    /// <summary>
    /// Runs the command with the long name <paramref name="name"/> with no reference selected and returns the number it reported.
    /// The number is 0 for a command that writes no number.
    /// </summary>
    /// <inheritdoc cref="Run(string)" path="/exception"/>
    double RunAndGetResult(string name);

    /// <summary>
    /// Runs the command with the long name <paramref name="name"/> on <paramref name="reference"/> and returns the number it reported.
    /// The number is 0 for a command that writes no number.
    /// </summary>
    /// <inheritdoc cref="Run{TObjectReference}(TObjectReference, string)" path="/exception"/>
    double RunAndGetResult<TObjectReference>(TObjectReference reference, string name) where TObjectReference : IObjectReference;

    /// <summary>
    /// Runs <paramref name="commandLine"/>, the text as typed in the console, such as <c>additem f 100</c>, on <paramref name="reference"/>.
    /// </summary>
    /// <remarks>
    /// The game returns no result, so read a command's output from <c>ConsoleLog.LastMessage</c> after the call.
    /// A command the compiler rejects prints its error there. A command that prints nothing, such as <c>coc</c>, leaves the previous message there.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">The game could not create the script that compiles the line.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="commandLine"/> is <c>null</c>.</exception>
    void RunLine<TObjectReference>(TObjectReference reference, string commandLine) where TObjectReference : IObjectReference;

    /// <summary>
    /// Runs <paramref name="commandLine"/>, the text as typed in the console, such as <c>player.additem f 100</c>, with no reference selected.
    /// </summary>
    /// <remarks>
    /// The game returns no result, so read a command's output from <c>ConsoleLog.LastMessage</c> after the call.
    /// A command the compiler rejects prints its error there. A command that prints nothing, such as <c>coc</c>, leaves the previous message there.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">The game could not create the script that compiles the line.</exception>
    /// <exception cref="System.ArgumentNullException"><paramref name="commandLine"/> is <c>null</c>.</exception>
    void RunLine(string commandLine);
}
