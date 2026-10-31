using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

// The game frees these objects, so they must come from its heap.
internal static class GameHeap
{
    // The size is a multiple of eight.
    internal static nint AllocateZeroed(GameTarget gameTarget, int size, string type)
    {
        nint address = gameTarget.MemoryManager.Allocate((ulong)size, 0, false);
        if (address == 0)
        {
            throw new InvalidOperationException($"The game's heap returned no memory for a {type}.");
        }
        for (int offset = 0; offset < size; offset += sizeof(long))
        {
            Marshal.WriteInt64(address, offset, 0);
        }
        return address;
    }

    internal static nint AllocateObject(GameTarget gameTarget, int size, string type)
    {
        nint vtable = GetVTable(type);
        nint address = AllocateZeroed(gameTarget, size, type);
        Marshal.WriteIntPtr(address, vtable);
        return address;
    }

    internal static nint GetVTable(string type) =>
        NativeSupplements.SF_Hook_VTable(type) is var vtable and not 0 ? vtable : throw new InvalidOperationException($"The game has no virtual table for {type}.");
}

internal static partial class NativeSupplements
{
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SF_Hook_VTable(string name);
}
