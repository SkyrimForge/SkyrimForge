using SkyrimForge.Hooks;
using System.Runtime.InteropServices;

namespace SkyrimForge;

internal static partial class NativeRuntime
{
    internal const string Library = "SkyrimForge.Runtime.dll";

    internal static nint GetAddress(GameFunction function) => SF_Hook_FunctionAddress(function.SpecialEdition, function.AnniversaryEdition, function.VR);

    [LibraryImport(Library)]
    internal static partial nint SF_Hook_FunctionAddress(ulong se, ulong ae, ulong vr);

    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SF_Hook_VTable(string name);
}
