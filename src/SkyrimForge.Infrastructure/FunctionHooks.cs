using SkyrimForge.Hooks;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Infrastructure;

internal sealed class FunctionHooks(string owner) : IFrameworkFunctionHooks
{
    internal static string? ConflictOwner => Marshal.PtrToStringUTF8(NativeGlobals.SF_Hook_ConflictOwner()) is { Length: > 0 } name ? name : null;

    /// <inheritdoc/>
    public nint ReplaceVirtualTableEntry(string typeName, int index, nint replacement)
    {
        ArgumentNullException.ThrowIfNull(typeName);
        ArgumentOutOfRangeException.ThrowIfZero(replacement);
        // The native side reads the entry before validating it, and an unmapped address faults.
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 0x1000);
        nint vtable = NativeRuntime.SF_Hook_VTable(typeName);
        if (vtable == 0)
        {
            throw new InvalidOperationException(
                $"No virtual method table address is known for {typeName}. CommonLib declares no table under that name, or the running game version has no address for it.");
        }
        nint displacedAddress = NativeGlobals.SF_Hook_WriteVirtual(vtable, (uint)index, replacement, owner);
        if (displacedAddress == 0)
        {
            throw new InvalidOperationException(NativeGlobals.SF_Hook_Fault() == PatchFault.Overlap
                ? $"Entry 0x{index:X} of {typeName} overlaps memory {ConflictOwner} already changed. Nothing was written."
                : $"Entry 0x{index:X} of {typeName} does not address code in a loaded module, and no earlier hook replaced it. The index may be past the table's end. Nothing was written.");
        }
        return displacedAddress;
    }

    /// <inheritdoc/>
    public nint FindCallSite(GameFunction caller, GameFunction callee, int occurrence = 0, int searchLength = 0x200)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(occurrence);

        // The native side reads every byte of the search length, and a byte past the caller's code can match a call in the next function.
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(searchLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(searchLength, 0x2000);
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
            throw new InvalidOperationException($"The offset into {caller} does not address a call to game code or to an absolute jump stub on this runtime.");
        }
        return callSite;
    }

    /// <inheritdoc/>
    public nint ReplaceCallTarget(nint callSite, nint replacement)
    {
        ArgumentOutOfRangeException.ThrowIfZero(callSite);
        ArgumentOutOfRangeException.ThrowIfZero(replacement);
        nint displacedAddress = NativeGlobals.SF_Hook_WriteCall(callSite, replacement, owner);
        if (displacedAddress == 0)
        {
            string reason = NativeGlobals.SF_Hook_Fault() switch
            {
                PatchFault.Overlap => $"overlaps code {ConflictOwner} already changed, other than a call replaced at the same address",
                PatchFault.NoImage or PatchFault.ExcludedImage or PatchFault.NotExecutable => "does not lie in the executable code of a module SkyrimForge patches",
                PatchFault.NoNearMemory => "lies farther than a 32-bit displacement from any memory that could be allocated for the stub the call reaches",
                _ => "does not contain a call to code in the same section or to an absolute jump stub"
            };
            throw new InvalidOperationException($"The address 0x{callSite:X} {reason}. Nothing was written.");
        }
        return displacedAddress;
    }

    /// <inheritdoc/>
    public nint DetourFunction(GameFunction function, nint replacement)
    {
        ArgumentOutOfRangeException.ThrowIfZero(replacement);
        nint entry = GetAddress(function);
        nint trampoline = NativeGlobals.SF_Hook_WriteEntry(entry, replacement, owner);
        if (trampoline == 0)
        {
            string reason = NativeGlobals.SF_Hook_Fault() switch
            {
                PatchFault.Overlap => $"Its opening instructions overlap code {ConflictOwner} already changed, other than a jump written at its entry.",
                PatchFault.ContainsTarget => $"Its opening instructions contain, past their first byte, the target of a jump or call {ConflictOwner} applied.",
                PatchFault.NoImage or PatchFault.ExcludedImage or PatchFault.NotExecutable => "Its opening instructions do not lie in the executable code of a module SkyrimForge patches.",
                PatchFault.NoNearMemory => "No memory within a 32-bit displacement of it could be allocated for the trampoline.",
                PatchFault.NotRelocatable => $"Its opening instructions cannot be moved to a trampoline. Its first 16 bytes are {DescribeCode(entry, 16)}.",
                PatchFault.MovedCallNotLast => "Its opening instructions contain a call followed by another instruction the jump overwrites.",
                PatchFault.BranchWithinMoved => "Its opening instructions contain a branch to one of the instructions the jump overwrites.",
                PatchFault.BranchIntoPatch => "A direct branch elsewhere in the function lands inside the instructions the jump overwrites, past their first byte.",
                _ => "The jump could not be written to its entry."
            };
            throw new InvalidOperationException($"The function at 0x{entry:X} was not detoured. {reason}");
        }
        return trampoline;
    }

    private static nint GetAddress(GameFunction function) => NativeRuntime.GetAddress(function) is var address and not 0
        ? address
        : throw new InvalidOperationException($"The address library does not contain the identifier of {function} on this runtime.");

    // Reads without a check: the native side confirmed the bytes lie in a module's code, all of them for CodePatcher and the first 5 of the 16 for a detour.
    internal static unsafe string DescribeCode(nint address, int length) =>
        string.Join(" ", new ReadOnlySpan<byte>((void*)address, length).ToArray().Select(value => value.ToString("X2")));
}
