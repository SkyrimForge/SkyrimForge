using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Infrastructure;

internal sealed unsafe partial class CodePatcher(string owner, ILog log) : ICodePatcher
{
    private const int instructionHookCapacity = 1024;

    private static readonly Lock slotsLock = new();

    // A slot is filled with slotsLock held before its hook is written. Dispatch reads it without the lock.
    private static readonly InstructionHookSlot?[] slots = new InstructionHookSlot?[instructionHookCapacity];

    private static readonly Func<ulong, string?> findInteriorOwner = address => Marshal.PtrToStringUTF8(SF_Code_InteriorOwner((nint)address));

    private static bool dispatchSet;

    private readonly string owner = owner;
    private readonly ILog log = log;

    /// <summary>Mirrors PatchFault in CodePatches.cpp.</summary>
    private enum PatchFault
    {
        None,
        NoImage,
        ExcludedImage,
        NotExecutable,
        Overlap,
        NotInstructionStart,
        Mismatch,
        ProtectionUnchanged,
        NotInstructionEnd,
        BranchIntoPatch,
        DisplacedRelative,
        TargetNotInstructionStart,
        TargetInsideWrite,
        NoNearMemory,
        ContainsTarget,
        NotRelocatable,
        MovedPastExpected,
        MovedCallNotLast,
        BranchWithinMoved,
        HookNotWritten,
        HookNotRemoved
    }

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

        uint[] patchSlots = ReserveSlots(patchArray);
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
                ReleaseSlots(patchArray, patchSlots);
            }
            throw new InvalidOperationException(GetFaultMessage(fault, patchArray[failedIndex]));
        }
    }

    /// <inheritdoc/>
    public nint AllocateCode(nint near, int length)
    {
        if (near == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(near), "The address is 0.");
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        nint address = SF_Code_Allocate(near, (uint)length, owner);
        return address != 0 ? address : throw new InvalidOperationException($"No free memory for {length} bytes lies within a 32-bit displacement of 0x{near:X}.");
    }

    private uint[] ReserveSlots(CodePatch[] patches)
    {
        uint[] patchSlots = new uint[patches.Length];
        int hookCount = patches.Count(patch => patch is InstructionHook);
        if (hookCount == 0)
        {
            return patchSlots;
        }
        lock (slotsLock)
        {
            int[] freeSlots = [.. Enumerable.Range(0, instructionHookCapacity).Where(slot => slots[slot] == null).Take(hookCount)];
            if (freeSlots.Length < hookCount)
            {
                throw new InvalidOperationException(
                    $"The {hookCount} instruction hooks would exceed the process limit of {instructionHookCapacity}, of which {instructionHookCapacity - freeSlots.Length} are in use. Nothing was written.");
            }
            if (!dispatchSet)
            {
                SF_Hook_SetInstructionHandler(&Dispatch);
                dispatchSet = true;
            }
            int nextFreeSlot = 0;
            for (int i = 0; i < patches.Length; i++)
            {
                if (patches[i] is InstructionHook hook)
                {
                    int slot = freeSlots[nextFreeSlot++];
                    Volatile.Write(ref slots[slot], new InstructionHookSlot(hook.Handler, hook.Address, log));
                    patchSlots[i] = (uint)slot;
                }
            }
        }
        return patchSlots;
    }

    private static void ReleaseSlots(CodePatch[] patches, uint[] patchSlots)
    {
        lock (slotsLock)
        {
            for (int i = 0; i < patches.Length; i++)
            {
                if (patches[i] is InstructionHook)
                {
                    slots[patchSlots[i]] = null;
                }
            }
        }
    }

    [UnmanagedCallersOnly]
    private static void Dispatch(uint slot, CpuRegisters* registers)
    {
        InstructionHookSlot hook = Volatile.Read(ref slots[slot])!;
        try
        {
            hook.Handler(new CpuContext(ref *registers, hook.Address, findInteriorOwner));
        }
        catch (Exception exception)
        {
            string message = $"The handler of the instruction hook at 0x{hook.Address:X} threw an exception. The process ends.";
            hook.Log.Write($"{message} {exception}");
            Environment.FailFast(message, exception);
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
            PatchFault.Mismatch => $"read {DescribeCode(patch)}, which does not match the expected {patch.Expected}",
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

    // Called after the native side confirms the bytes are in a module's code, so reading them cannot fault.
    private static string DescribeCode(CodePatch patch) =>
        string.Join(" ", new ReadOnlySpan<byte>((void*)patch.Address, patch.Expected.Length).ToArray().Select(value => value.ToString("X2")));

    [LibraryImport(NativeRuntime.Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial PatchFault SF_Code_WritePatches(NativeCodePatch* patches, uint count, string owner, uint* failedIndex);

    [LibraryImport(NativeRuntime.Library, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SF_Code_Allocate(nint address, uint length, string owner);

    [LibraryImport(NativeRuntime.Library)]
    private static partial void SF_Hook_SetInstructionHandler(delegate* unmanaged<uint, CpuRegisters*, void> handler);

    [LibraryImport(NativeRuntime.Library)]
    private static partial nint SF_Code_InteriorOwner(nint address);

    private sealed record InstructionHookSlot(InstructionHandler Handler, nint Address, ILog Log);

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
