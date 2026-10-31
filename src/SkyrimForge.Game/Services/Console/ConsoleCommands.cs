using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

internal sealed class ConsoleCommands : IConsoleCommands
{
    public bool Exists(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return NativeSupplements.SF_SCRIPT_FUNCTION_LocateConsoleCommand(name) != 0;
    }

    public int? GetArgumentCount(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        int count = NativeSupplements.SF_SCRIPT_FUNCTION_NumParams(name);
        return count >= 0 ? count : null;
    }

    public int? GetRequiredArgumentCount(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        int count = NativeSupplements.SF_SCRIPT_FUNCTION_RequiredParams(name);
        return count >= 0 ? count : null;
    }

    public void Run(string name) => Execute(0, name);

    public void Run<TObjectReference>(TObjectReference reference, string name) where TObjectReference : IObjectReference => Execute(reference.Address, name);

    public double RunAndGetResult(string name) => Execute(0, name);

    public double RunAndGetResult<TObjectReference>(TObjectReference reference, string name) where TObjectReference : IObjectReference => Execute(reference.Address, name);

    public void RunLine(string commandLine) => RunLine(0, commandLine);

    public void RunLine<TObjectReference>(TObjectReference reference, string commandLine) where TObjectReference : IObjectReference => RunLine(reference.Address, commandLine);

    private static double Execute(nint reference, string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var status = (ExecuteStatus)NativeSupplements.SF_SCRIPT_FUNCTION_ExecuteFunction(name, reference, out double result);
        return status switch
        {
            ExecuteStatus.ReportedFailure => throw new InvalidOperationException($"The console command {name} ran and reported failure."),
            ExecuteStatus.NotFound => throw new ArgumentException($"The game has no console command named {name} that it can run.", nameof(name)),
            ExecuteStatus.RequiresArgument => throw new ArgumentException($"The console command {name} requires an argument, which this call cannot pass. RunLine passes arguments.", nameof(name)),
            ExecuteStatus.RequiresReference => throw new ArgumentException($"The console command {name} runs on a reference, so it needs the overload that takes one.", nameof(name)),
            _ => result
        };
    }

    private static void RunLine(nint reference, string commandLine)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        if (!NativeSupplements.SF_Console_RunCommandLine(commandLine, reference))
        {
            throw new InvalidOperationException("The game could not create the script that compiles a console command line, so the line did not run.");
        }
    }

    private enum ExecuteStatus
    {
        Succeeded = 0,

        ReportedFailure = 1,

        NotFound = -1,

        RequiresArgument = -2,

        RequiresReference = -3
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial long SF_SCRIPT_FUNCTION_LocateConsoleCommand(string name);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_SCRIPT_FUNCTION_ExecuteFunction(string name, nint reference, out double result);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_SCRIPT_FUNCTION_NumParams(string name);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_SCRIPT_FUNCTION_RequiredParams(string name);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Console_RunCommandLine(string commandLine, nint reference);
}
