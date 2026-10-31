using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed unsafe class ConsoleCommandRegistrar(ILog log) : IConsoleCommandRegistrar
{
    // Keyed by the command's parameter array address, which the native handler passes.
    private static readonly ConcurrentDictionary<long, Command> commands = new();
    private static bool installed;

    private sealed record Command(string Name, Func<ObjectReference?, IReadOnlyList<string>, Task> Handler, ILog Log);

    private readonly ILog log = log;

    public void Register(string name, string? shortName, string help, IEnumerable<string> parameterNames, Func<ObjectReference?, IReadOnlyList<string>, Task> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(help);
        ArgumentNullException.ThrowIfNull(parameterNames);
        ArgumentNullException.ThrowIfNull(handler);
        if (name.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("A console command name cannot contain white space.", nameof(name));
        }
        if (shortName?.Any(char.IsWhiteSpace) == true)
        {
            throw new ArgumentException("A console command name cannot contain white space.", nameof(shortName));
        }
        if (!installed)
        {
            NativeSupplements.SF_ConsoleCommand_SetHandler(&Run);
            installed = true;
        }
        string[] parameters = [.. parameterNames];
        long id = NativeSupplements.SF_ConsoleCommand_Add(name, shortName, help, parameters, parameters.Length);
        if (id > 0)
        {
            commands[id] = new Command(name, handler, log);
            return;
        }
        if (id == -1)
        {
            string names = shortName != null ? $"{name} or {shortName}" : name;
            throw new ArgumentException($"A console or script command already has the name {names}.", nameof(name));
        }
        if (id == -2)
        {
            throw new ArgumentException($"The native registration rejected the arguments for {name}.");
        }
        throw new InvalidOperationException($"No disabled console command entry remains, so {name} was not added.");
    }

    [UnmanagedCallersOnly]
    private static void Run(long id, nint target, int count, byte** arguments)
    {
        if (!commands.TryGetValue(id, out Command? command))
        {
            return;
        }
        try
        {
            var values = new string[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = Marshal.PtrToStringUTF8((nint)arguments[i]) ?? throw new InvalidOperationException($"The native handler passed a null pointer for argument {i}.");
            }
            command.Handler(ObjectReference.OrNull(target), values).OnFault(exception => command.Log.Write($"Console command {command.Name} threw: {exception}"));
        }
        catch (Exception ex)
        {
            command.Log.Write($"Console command {command.Name} threw: {ex}");
        }
    }
}

internal static unsafe partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_ConsoleCommand_SetHandler(delegate* unmanaged<long, nint, int, byte**, void> handler);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial long SF_ConsoleCommand_Add(string name, string? shortName, string help, [In] string[] parameterNames, int count);
}
