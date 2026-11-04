using SkyrimForge.Hooks;
using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

// The game frees these objects, so they must come from its heap.
internal static class GameHeap
{
    internal static nint AllocateZeroed(GameTarget gameTarget, int size)
    {
        nint address = gameTarget.MemoryManager.Allocate((ulong)size, 0, false);
        GameMemory.AsSpan(address, size).Clear();
        return address;
    }

    internal static nint AllocateObject(GameTarget gameTarget, int size, string type)
    {
        nint vtable = GetVTable(type);
        nint address = AllocateZeroed(gameTarget, size);
        Marshal.WriteIntPtr(address, vtable);
        return address;
    }

    internal static nint GetVTable(string type) =>
        NativeRuntime.SF_Hook_VTable(type) is var vtable and not 0 ? vtable : throw new InvalidOperationException($"The game has no virtual table for {type}.");
}
