using SkyrimForge.Hooks;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Infrastructure;

internal sealed unsafe partial class CodePatcher(string owner, ILog log) : ICodePatcher
{
    private const int instructionHookCapacity = 1024;

    private static readonly Lock instructionHooksLock = new();

    // A slot is filled with instructionHooksLock held before its hook is written. Dispatch reads it without the lock.
    // A slot's entries are linked newest first, and the entry of a hook sharing the slot is linked in once Apply has written its group.
    private static readonly InstructionHookEntry?[] slots = new InstructionHookEntry?[instructionHookCapacity];

    // Set with instructionHooksLock held for a slot whose hook was written, so a later hook at its address can share it.
    // A slot whose hook was removed after a later hook in its group failed is retained but not set.
    private static readonly bool[] writtenSlots = new bool[instructionHookCapacity];

    private static readonly Func<ulong, string?> findInteriorOwner = address => Marshal.PtrToStringUTF8(SF_Code_InteriorOwner((nint)address));

    private static bool dispatchSet;

    private readonly string owner = owner;
    private readonly ILog log = log;

    /// <summary>Mirrors PatchKind in CodePatches.cpp.</summary>
    private enum PatchKind
    {
        Bytes,
        Jump,
        Call,
        Insertion,
        Instruction
    }

    /// <inheritdoc/>
    public void Apply(params ReadOnlySpan<CodePatch> patches)
    {
        CodePatch[] patchArray = [.. patches];
        for (int i = 0; i < patches.Length; i++)
        {
            CodePatch patch = patchArray[i] ?? throw new ArgumentNullException(nameof(patches), $"Patch {i} is null.");
            int overlappingIndex = Array.FindIndex(patchArray, 0, i, earlier => Overlap(earlier, patch));
            if (overlappingIndex >= 0)
            {
                throw new ArgumentException($"The expected bytes of patches {overlappingIndex} and {i} overlap.", nameof(patches));
            }
        }
        for (int i = 0; i < patchArray.Length; i++)
        {
            if (GetTarget(patchArray[i]) is nint target &&
                Array.FindIndex(patchArray, patch => patch.Address < target && target < patch.Address + patch.Expected.Length) is int containingIndex and >= 0)
            {
                throw new ArgumentException($"The target 0x{target:X} of patch {i} lies inside the expected bytes of patch {containingIndex} past their first byte.",
                    nameof(patches));
            }
        }
        if (patchArray.Length == 0)
        {
            return;
        }

        (uint[] patchSlots, bool[] sharing) = ReserveSlots(patchArray);
        NativeCodePatch[] nativePatches = new NativeCodePatch[patchArray.Length];
        byte[] data = new byte[patchArray.Sum(patch => 2 * patch.Expected.Length + GetPatchData(patch).Length)];
        PatchFault fault;
        uint failedIndex = 0;
        fixed (byte* firstByte = data)
        {
            byte* nextByte = firstByte;
            for (int i = 0; i < patchArray.Length; i++)
            {
                CodePatch patch = patchArray[i];
                int expectedLength = patch.Expected.Length;
                ReadOnlySpan<byte> patchData = GetPatchData(patch).Span;
                patch.Expected.CopyTo(new Span<byte>(nextByte, expectedLength), new Span<byte>(nextByte + expectedLength, expectedLength));
                patchData.CopyTo(new Span<byte>(nextByte + 2 * expectedLength, patchData.Length));
                nativePatches[i] = CreateNativePatch(patch, nextByte, nextByte + expectedLength, nextByte + 2 * expectedLength);
                nativePatches[i].Slot = patchSlots[i];
                nextByte += 2 * expectedLength + patchData.Length;
            }
            fixed (NativeCodePatch* firstPatch = nativePatches)
            {
                fault = SF_Code_WritePatches(firstPatch, (uint)nativePatches.Length, owner, &failedIndex);
            }
        }
        if (fault != PatchFault.None)
        {
            // A hook that was written and then removed retains its slot, because a thread inside it may still call its handler.
            if (fault is not (PatchFault.HookNotWritten or PatchFault.HookNotRemoved))
            {
                ReleaseSlots(patchArray, patchSlots, sharing);
            }
            throw new InvalidOperationException(GetFaultMessage(fault, patchArray[failedIndex]));
        }
        CompleteSlots(patchArray, patchSlots, sharing);
    }

    /// <inheritdoc/>
    public nint AllocateCode(nint near, int length)
    {
        ArgumentOutOfRangeException.ThrowIfZero(near);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        nint address = SF_Code_Allocate(near, (uint)length, owner);
        return address != 0 ? address : throw new InvalidOperationException($"No free memory for {length} bytes lies within a 32-bit displacement of 0x{near:X}.");
    }

    /// <summary>
    /// Assigns each hook at the address of a written hook that hook's slot, which SF_Code_WritePatches confirms against its record, and reserves a free slot for each other hook.
    /// </summary>
    private (uint[] PatchSlots, bool[] Sharing) ReserveSlots(CodePatch[] patches)
    {
        uint[] patchSlots = new uint[patches.Length];
        bool[] sharing = new bool[patches.Length];
        if (!patches.Any(patch => patch is InstructionHook))
        {
            return (patchSlots, sharing);
        }
        lock (instructionHooksLock)
        {
            // A written slot is never released, so it is never null.
            int[] sharedSlots = [.. patches.Select(patch => patch is InstructionHook hook
                ? Enumerable.Range(0, instructionHookCapacity).FirstOrDefault(slot => writtenSlots[slot] && slots[slot]!.Address == hook.Address, -1)
                : -1)];
            int newHookCount = patches.Where((patch, i) => patch is InstructionHook && sharedSlots[i] < 0).Count();
            int[] freeSlots = [.. Enumerable.Range(0, instructionHookCapacity).Where(slot => slots[slot] == null).Take(newHookCount)];
            if (freeSlots.Length < newHookCount)
            {
                throw new InvalidOperationException(
                    $"The {newHookCount} instruction hooks would exceed the process limit of {instructionHookCapacity} hooked addresses, of which {instructionHookCapacity - freeSlots.Length} are in use. Nothing was written.");
            }
            if (!dispatchSet)
            {
                SF_Hook_SetInstructionHandler(&Dispatch);
                dispatchSet = true;
            }
            int nextFreeSlot = 0;
            for (int i = 0; i < patches.Length; i++)
            {
                if (patches[i] is not InstructionHook hook)
                {
                    continue;
                }
                if (sharedSlots[i] >= 0)
                {
                    patchSlots[i] = (uint)sharedSlots[i];
                    sharing[i] = true;
                    continue;
                }
                int slot = freeSlots[nextFreeSlot++];
                Volatile.Write(ref slots[slot], new InstructionHookEntry(hook.Handler, hook.Address, log, null));
                patchSlots[i] = (uint)slot;
            }
        }
        return (patchSlots, sharing);
    }

    private static void ReleaseSlots(CodePatch[] patches, uint[] patchSlots, bool[] sharing)
    {
        lock (instructionHooksLock)
        {
            for (int i = 0; i < patches.Length; i++)
            {
                if (patches[i] is InstructionHook && !sharing[i])
                {
                    slots[patchSlots[i]] = null;
                }
            }
        }
    }

    /// <summary>Marks each new hook's slot as written, and links the entry of each sharing hook ahead of the entries already in its slot.</summary>
    private void CompleteSlots(CodePatch[] patches, uint[] patchSlots, bool[] sharing)
    {
        lock (instructionHooksLock)
        {
            for (int i = 0; i < patches.Length; i++)
            {
                if (patches[i] is not InstructionHook hook)
                {
                    continue;
                }
                uint slot = patchSlots[i];
                if (sharing[i])
                {
                    Volatile.Write(ref slots[slot], new InstructionHookEntry(hook.Handler, hook.Address, log, slots[slot]));
                }
                writtenSlots[slot] = true;
            }
        }
    }

    /// <summary>Runs a slot's handlers newest first, until one sets rip to another address, as a detour that does not call its displaced function ends a chain.</summary>
    [UnmanagedCallersOnly]
    private static void Dispatch(uint slot, CpuRegisters* registers)
    {
        ulong trampoline = registers->Rip;
        for (InstructionHookEntry? entry = Volatile.Read(ref slots[slot]); entry != null && registers->Rip == trampoline; entry = entry.Earlier)
        {
            try
            {
                entry.Handler(new CpuContext(ref *registers, entry.Address, findInteriorOwner));
            }
            catch (Exception exception)
            {
                string message = $"The handler of the instruction hook at 0x{entry.Address:X} threw an exception. The process ends.";
                entry.Log.Write($"{message} {exception}");
                Environment.FailFast(message, exception);
            }
        }
    }

    private static bool Overlap(CodePatch first, CodePatch second) =>
        first.Address < second.Address + second.Expected.Length && second.Address < first.Address + first.Expected.Length;

    private static nint? GetTarget(CodePatch patch) => patch switch
    {
        JumpPatch jump => jump.Target,
        CallPatch call => call.Target,
        _ => null
    };

    private static ReadOnlyMemory<byte> GetPatchData(CodePatch patch) => patch switch
    {
        BytePatch bytePatch => bytePatch.Replacement,
        InsertionPatch insertion => insertion.Code,
        _ => ReadOnlyMemory<byte>.Empty
    };

    private static NativeCodePatch CreateNativePatch(CodePatch patch, byte* expected, byte* mask, byte* data)
    {
        NativeCodePatch nativePatch = new()
        {
            Address = patch.Address,
            Expected = expected,
            Mask = mask,
            ExpectedLength = (uint)patch.Expected.Length
        };
        switch (patch)
        {
            case BytePatch bytePatch:
                nativePatch.Kind = PatchKind.Bytes;
                nativePatch.Replacement = data;
                nativePatch.Offset = (uint)bytePatch.Offset;
                nativePatch.ReplacementLength = (uint)bytePatch.Replacement.Length;
                break;
            case JumpPatch jump:
                nativePatch.Kind = PatchKind.Jump;
                nativePatch.Target = jump.Target;
                break;
            case CallPatch call:
                nativePatch.Kind = PatchKind.Call;
                nativePatch.Target = call.Target;
                break;
            case InsertionPatch insertion:
                nativePatch.Kind = PatchKind.Insertion;
                nativePatch.Code = data;
                nativePatch.CodeLength = (uint)insertion.Code.Length;
                nativePatch.DisplacedLength = (uint)insertion.DisplacedLength;
                nativePatch.DisplacedAfter = insertion.Placement == DisplacedPlacement.AfterCode ? 1 : 0;
                break;
            case InstructionHook:
                nativePatch.Kind = PatchKind.Instruction;
                break;
        }
        return nativePatch;
    }

    private static string GetFaultMessage(PatchFault fault, CodePatch patch)
    {
        string reason = fault switch
        {
            PatchFault.NoImage => "lie in no loaded module",
            PatchFault.ExcludedImage => "lie in a module SkyrimForge does not patch: a Windows module, a .NET runtime module, a managed assembly, or SkyrimForge.Runtime.dll",
            PatchFault.NotExecutable => "do not lie in one executable section of their module",
            PatchFault.Overlap => $"overlap code {FunctionHooks.ConflictOwner} already changed",
            PatchFault.NotInstructionStart => "do not begin on an instruction of the function containing them",
            PatchFault.Mismatch => $"read {FunctionHooks.DescribeCode(patch.Address, patch.Expected.Length)}, which does not match the expected {patch.Expected}",
            PatchFault.ProtectionUnchanged => "lie on a page whose protection could not be changed to allow writing",
            PatchFault.NotInstructionEnd => patch is InsertionPatch insertion
                ? $"or the first {insertion.DisplacedLength} of them, which the insertion displaces, do not end on an instruction"
                : "do not end on an instruction",
            PatchFault.BranchIntoPatch => "contain the target of a direct branch elsewhere in their function past their first byte",
            PatchFault.DisplacedRelative => "displace an instruction with an operand relative to its own address, which would refer to a different address once moved",
            PatchFault.TargetNotInstructionStart => $"would become {DescribeBranch(patch)}, which does not begin an instruction of the function containing it",
            PatchFault.TargetInsideWrite => $"would become {DescribeBranch(patch)}, which lies inside code {FunctionHooks.ConflictOwner} already changed",
            PatchFault.NoNearMemory => "lie farther than a 32-bit displacement from any memory that could be allocated",
            PatchFault.ContainsTarget => $"contain, past their first byte, the target of a jump or call {FunctionHooks.ConflictOwner} applied",
            PatchFault.NotRelocatable => "begin with instructions SafetyHook cannot move to a trampoline",
            PatchFault.MovedPastExpected => "do not cover every byte of the instructions the hook's jump overwrites",
            PatchFault.MovedCallNotLast => "contain a call followed by another instruction the hook's jump overwrites",
            PatchFault.BranchWithinMoved => "contain a branch to one of the instructions the hook's jump overwrites",
            _ => "could not be overwritten with the hook's jump"
        };
        string outcome = fault == PatchFault.HookNotRemoved
            ? ". An earlier instruction hook in the group could not be removed and remains written, as SkyrimForge.Runtime.log records. Nothing else was written."
            : ". Nothing was written.";
        return $"The {patch.Expected.Length} bytes at 0x{patch.Address:X} {reason}{outcome}";
    }

    private static string DescribeBranch(CodePatch patch) => $"{(patch is CallPatch ? "a call" : "a jump")} to 0x{GetTarget(patch):X}";

    [LibraryImport(NativeRuntime.Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial PatchFault SF_Code_WritePatches(NativeCodePatch* patches, uint count, string owner, uint* failedIndex);

    [LibraryImport(NativeRuntime.Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SF_Code_Allocate(nint address, uint length, string owner);

    [LibraryImport(NativeRuntime.Library)]
    private static partial void SF_Hook_SetInstructionHandler(delegate* unmanaged<uint, CpuRegisters*, void> handler);

    [LibraryImport(NativeRuntime.Library)]
    private static partial nint SF_Code_InteriorOwner(nint address);

    private sealed record InstructionHookEntry(InstructionHandler Handler, nint Address, ILog Log, InstructionHookEntry? Earlier);

    /// <summary>Mirrors CodePatch in CodePatches.cpp.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCodePatch
    {
        public PatchKind Kind;
        public nint Address;
        public byte* Expected;
        public byte* Mask;
        public uint ExpectedLength;
        public byte* Replacement;
        public uint Offset;
        public uint ReplacementLength;
        public nint Target;
        public byte* Code;
        public uint CodeLength;
        public uint DisplacedLength;
        public int DisplacedAfter;
        public uint Slot;
    }
}
