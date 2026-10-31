using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Adds console commands that run managed handlers.</summary>
/// <remarks>
/// The console command table has a fixed size, so each command replaces a disabled entry and remains until the game exits. Call members on the game's thread.
/// </remarks>
public interface IConsoleCommandRegistrar
{
    /// <summary>Adds a console command whose handler returns a task.</summary>
    /// <remarks>
    /// <paramref name="handler"/> runs on the game's thread. Its first argument is the reference the command runs on, or null if there is none.
    /// It receives as many text arguments as were entered, up to the count of <paramref name="parameterNames"/>.
    /// The console does not wait for its task. An exception it throws, or one its task faults with, is written to the plugin's log.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty, a name contains white space, or a console or script command already has <paramref name="name"/> or <paramref name="shortName"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">No disabled entry remains.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/>, <paramref name="help"/>, <paramref name="parameterNames"/>, or <paramref name="handler"/> is <c>null</c>.
    /// </exception>
    void Register(string name, string? shortName, string help, IEnumerable<string> parameterNames, Func<ObjectReference?, IReadOnlyList<string>, Task> handler);
}
