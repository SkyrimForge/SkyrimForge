using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="IConsoleCommandRegistrar"/>.</summary>
public static class ConsoleCommandRegistrarExtensions
{
    extension(IConsoleCommandRegistrar registrar)
    {
        /// <summary>Adds a console command with no short name and no arguments, whose handler returns a task.</summary>
        /// <remarks>
        /// <paramref name="handler"/> runs on the game's thread. Its argument is the reference the command runs on, or null if there is none.
        /// The console does not wait for its task. An exception it throws, or one its task faults with, is written to the plugin's log.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="name"/> is empty, contains white space, or is the name of a console or script command already.
        /// </exception>
        /// <exception cref="InvalidOperationException">No disabled entry remains.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="help"/>, or <paramref name="handler"/> is <c>null</c>.</exception>
        public void Register(string name, string help, Func<ObjectReference?, Task> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            registrar.Register(name, null, help, [], (target, _) => handler(target));
        }

        /// <summary>Adds a console command with no short name and no arguments.</summary>
        /// <remarks>
        /// <paramref name="handler"/> runs on the game's thread. Its argument is the reference the command runs on, or null if there is none.
        /// An exception it throws is written to the plugin's log.
        /// </remarks>
        /// <inheritdoc cref="Register(IConsoleCommandRegistrar, string, string, Func{ObjectReference?, Task})" path="/exception"/>
        public void Register(string name, string help, Action<ObjectReference?> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            registrar.Register(name, null, help, [], (target, _) => handler(target));
        }

        /// <summary>Adds a console command.</summary>
        /// <remarks>
        /// <paramref name="handler"/> runs on the game's thread. Its first argument is the reference the command runs on, or null if there is none.
        /// It receives as many text arguments as were entered, up to the count of <paramref name="parameterNames"/>.
        /// An exception it throws is written to the plugin's log.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="name"/> is empty, a name contains white space, or a console or script command already has <paramref name="name"/> or <paramref name="shortName"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">No disabled entry remains.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/>, <paramref name="help"/>, <paramref name="parameterNames"/>, or <paramref name="handler"/> is <c>null</c>.
        /// </exception>
        public void Register(string name, string? shortName, string help, IEnumerable<string> parameterNames, Action<ObjectReference?, IReadOnlyList<string>> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            registrar.Register(name, shortName, help, parameterNames, (target, arguments) =>
            {
                handler(target, arguments);
                return Task.CompletedTask;
            });
        }
    }
}
