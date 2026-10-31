using SkyrimForge.Game.BSResource;
using System.Runtime.InteropServices;

namespace SkyrimForge.Infrastructure;

internal static partial class NativeGlobals
{
    private const string library = NativeRuntime.Library;

    // The offsets are used only when an SKSE plugin loaded earlier replaced MainLoop's call to Main::Update.
    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static unsafe partial bool SF_Frame_Register(
        delegate* unmanaged<float, void> handler, nint mainLoop, nint mainUpdate, uint seOffset, uint aeOffset, uint vrOffset);

    [LibraryImport(library)]
    internal static unsafe partial void SF_Lifecycle_Register(delegate* unmanaged<SessionMessage, nint, int, void> handler);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void SF_ConsoleLog_Print(string message);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void SF_Messaging_SendModEvent(string eventName, string stringArgument, float numberArgument, nint sender);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Saving_ResolveFormId(uint storedFormId, out uint currentFormId);

    [LibraryImport(library)]
    internal static unsafe partial void SF_Saving_SetHandlers(delegate* unmanaged<void> save, delegate* unmanaged<uint, uint, nint, uint, void> load, delegate* unmanaged<void> revert);

    // SKSE accepts records only while the save handler runs.
    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Saving_WriteRecord(uint type, uint version, nint data, uint length);

    [LibraryImport(library)]
    internal static unsafe partial uint SF_Saving_SaveFolder(byte* buffer, uint capacity);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Task_Add(ulong token);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Task_AddUI(ulong token);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Task_IsAvailable();

    [LibraryImport(library)]
    internal static unsafe partial void SF_Task_SetHandler(delegate* unmanaged<ulong, void> handler);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SF_Hook_VTable(string name);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SF_Hook_WriteVirtual(nint vtable, uint index, nint replacement, string owner);

    [LibraryImport(library)]
    internal static partial nint SF_Hook_ConflictOwner();

    [LibraryImport(library)]
    internal static partial nint SF_Hook_FindCall(nint caller, nint callee, uint occurrence, uint searchLength);

    [LibraryImport(library)]
    internal static partial nint SF_Hook_FunctionAddress(ulong se, ulong ae, ulong vr);

    [LibraryImport(library)]
    internal static partial nint SF_Hook_CallAt(nint function, uint seOffset, uint aeOffset, uint vrOffset);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SF_Hook_WriteCall(nint site, nint replacement, string owner);

    [LibraryImport(library)]
    internal static unsafe partial uint SF_Hook_EntryPrologue(nint target, byte* destination, uint capacity);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SF_Hook_WriteEntry(nint target, nint replacement, string owner);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_GameFileSystem_Exists(string path);

    // Only kNone allocates a buffer, which SF_GameFileSystem_Release frees.
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial ErrorCode SF_GameFileSystem_ReadAll(string path, out nint buffer, out nint data, out ulong length);

    [LibraryImport(library)]
    internal static partial void SF_GameFileSystem_Release(nint buffer);

    [LibraryImport(library)]
    internal static unsafe partial void SF_Messaging_SetHandler(delegate* unmanaged<uint, nint, uint, nint, void> handler);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Messaging_Listen(string sender);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Messaging_Dispatch(uint messageType, nint data, uint length, string? receiver);
}
