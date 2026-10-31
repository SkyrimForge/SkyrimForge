using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>The edition running in this process.</summary>
public static partial class CurrentGameEdition
{
    /// <summary>The edition running in this process.</summary>
    public static readonly GameEdition Value = (GameEdition)SF_Module_Edition();

    /// <summary>Whether the running edition is one of these.</summary>
    public static bool IsAnyOf(GameEdition editions) => editions.HasFlag(Value);

    [LibraryImport(NativeRuntime.Library)]
    private static partial byte SF_Module_Edition();
}
