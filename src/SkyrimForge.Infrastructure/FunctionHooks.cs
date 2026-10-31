using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Infrastructure;

internal sealed class FunctionHooks(string owner) : IFrameworkFunctionHooks
{
    private readonly string owner = owner;

    internal static string? ConflictOwner => Marshal.PtrToStringUTF8(NativeGlobals.SF_Hook_ConflictOwner()) is { Length: > 0 } name ? name : null;

    /// <inheritdoc/>
    public nint ReplaceVirtualTableEntry(string typeName, int index, nint replacement)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        ThrowIfZero(replacement);
        // The native side reads the entry before validating it, and an unmapped address faults.
        if (index < 0 || index > 0x1000)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The index is outside the range of a virtual method table.");
        }
        nint vtable = NativeGlobals.SF_Hook_VTable(typeName);
        if (vtable == 0)
        {
            throw new InvalidOperationException(
                $"No virtual method table address is known for {typeName}. CommonLib declares no table under that name, or the running game version has no address for it.");
        }
        nint displacedAddress = NativeGlobals.SF_Hook_WriteVirtual(vtable, (uint)index, replacement, owner);
        if (displacedAddress == 0)
        {
            throw new InvalidOperationException(ConflictOwner is string conflictOwner
                ? $"Entry 0x{index:X} of {typeName} is already replaced by {conflictOwner}. Nothing was written."
                : $"Entry 0x{index:X} of {typeName} does not contain game code. The index may be past the table's end, or the entry may already be replaced. Nothing was written.");
        }
        return displacedAddress;
    }

    /// <inheritdoc/>
    public nint FindCallSite(GameFunction caller, GameFunction callee, int occurrence = 0, int searchLength = 0x200)
    {
        if (occurrence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(occurrence), occurrence, "The occurrence must be 0 or greater.");
        }
        // The native side reads every byte of the search length, and a byte past the caller's code can match a call in the next function.
        if (searchLength <= 0 || searchLength > 0x2000)
        {
            throw new ArgumentOutOfRangeException(nameof(searchLength), searchLength, "The search length must be between 1 and 0x2000 bytes.");
        }
        nint callSite = NativeGlobals.SF_Hook_FindCall(GetAddress(caller), GetAddress(callee), (uint)occurrence, (uint)searchLength);
        if (callSite == 0)
        {
            string calls = occurrence == 0 ? "no call" : $"fewer than {occurrence + 1} calls";
            throw new InvalidOperationException($"The first 0x{searchLength:X} bytes of {caller} contain {calls} to {callee}. "
                + "A call whose target is already replaced is not matched, and FindCallSite with the call's offset finds it.");
        }
        return callSite;
    }

    /// <inheritdoc/>
    public nint FindCallSite(GameFunction caller, GameOffset offset)
    {
        nint callSite = NativeGlobals.SF_Hook_CallAt(GetAddress(caller), offset.SpecialEdition, offset.AnniversaryEdition, offset.VR);
        if (callSite == 0)
        {
            throw new InvalidOperationException($"The offset into {caller} does not address a call to game code or to an SKSE trampoline stub on this runtime.");
        }
        return callSite;
    }

    /// <inheritdoc/>
    public nint ReplaceCallTarget(nint callSite, nint replacement)
    {
        if (callSite == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(callSite), "The call site is 0.");
        }
        ThrowIfZero(replacement);
        nint displacedAddress = NativeGlobals.SF_Hook_WriteCall(callSite, replacement, owner);
        if (displacedAddress == 0)
        {
            throw new InvalidOperationException(ConflictOwner is string conflictOwner
                ? $"The call at 0x{callSite:X} overlaps code {conflictOwner} already replaced. Nothing was written."
                : $"The address 0x{callSite:X} does not contain a call to game code or to an SKSE trampoline stub. Nothing was written.");
        }
        return displacedAddress;
    }

    /// <inheritdoc/>
    public nint DetourFunction(GameFunction function, nint replacement)
    {
        ThrowIfZero(replacement);
        nint entry = GetAddress(function);
        nint trampoline = NativeGlobals.SF_Hook_WriteEntry(entry, replacement, owner);
        if (trampoline == 0)
        {
            string reason = ConflictOwner is string conflictOwner
                ? $"Its opening instructions overlap code {conflictOwner} already replaced, or contain, past their first byte, the target of a jump or call {conflictOwner} applied."
                : GetDetourFailureReason(entry);
            throw new InvalidOperationException($"The function at 0x{entry:X} was not detoured. {reason}");
        }
        return trampoline;
    }

    private static void ThrowIfZero(nint replacement)
    {
        if (replacement == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(replacement), "The replacement address is 0.");
        }
    }

    private static nint GetAddress(GameFunction function)
    {
        nint address = NativeGlobals.SF_Hook_FunctionAddress(function.SpecialEdition, function.AnniversaryEdition, function.VR);
        if (address == 0)
        {
            throw new InvalidOperationException($"The address library does not contain the identifier of {function} on this runtime.");
        }
        return address;
    }

    private static unsafe string GetDetourFailureReason(nint entry)
    {
        const int reportedByteCount = 16;
        const byte relativeJump = 0xE9;
        byte[] openingBytes = new byte[reportedByteCount];
        uint movableLength;
        fixed (byte* destination = openingBytes)
        {
            movableLength = NativeGlobals.SF_Hook_EntryPrologue(entry, destination, reportedByteCount);
        }
        string bytes = string.Join(" ", openingBytes.Select(value => value.ToString("X2")));
        return movableLength != 0 ? $"Its first {movableLength} bytes can be relocated, but no trampoline memory is available for them."
            : openingBytes[0] == relativeJump
                ? $"It starts with a relative jump, which the framework does not relocate. It is already detoured, or it is a thunk. Its first {reportedByteCount} bytes are {bytes}."
            : $"It is outside the game's code, or it starts with an instruction the framework does not relocate. Its first {reportedByteCount} bytes are {bytes}.";
    }
}
