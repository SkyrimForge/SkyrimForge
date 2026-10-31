using System;

namespace SkyrimForge;

/// <summary>The registers of a thread that reached an <see cref="InstructionHook"/>, as they are before the hooked instruction runs.</summary>
/// <remarks>
/// The thread continues with every register as the handler leaves it.
/// The upper halves of the AVX registers are not saved, and the handler's compiled code may clear them.
/// Game code with a 256-bit value live across the hooked instruction can lose its upper half.
/// </remarks>
public readonly ref struct CpuContext
{
    private readonly ref CpuRegisters registers;
    private readonly ulong address;
    private readonly ulong trampoline;
    private readonly Func<ulong, string?> findInteriorOwner;

    internal CpuContext(ref CpuRegisters registers, nint address, Func<ulong, string?> findInteriorOwner)
    {
        this.registers = ref registers;
        this.address = (ulong)address;
        trampoline = registers.Rip;
        this.findInteriorOwner = findInteriorOwner;
    }

    /// <summary>rax.</summary>
    public ulong Rax { get => registers.Rax; set => registers.Rax = value; }

    /// <summary>rbx.</summary>
    public ulong Rbx { get => registers.Rbx; set => registers.Rbx = value; }

    /// <summary>rcx.</summary>
    public ulong Rcx { get => registers.Rcx; set => registers.Rcx = value; }

    /// <summary>rdx.</summary>
    public ulong Rdx { get => registers.Rdx; set => registers.Rdx = value; }

    /// <summary>rsi.</summary>
    public ulong Rsi { get => registers.Rsi; set => registers.Rsi = value; }

    /// <summary>rdi.</summary>
    public ulong Rdi { get => registers.Rdi; set => registers.Rdi = value; }

    /// <summary>rbp.</summary>
    public ulong Rbp { get => registers.Rbp; set => registers.Rbp = value; }

    /// <summary>r8.</summary>
    public ulong R8 { get => registers.R8; set => registers.R8 = value; }

    /// <summary>r9.</summary>
    public ulong R9 { get => registers.R9; set => registers.R9 = value; }

    /// <summary>r10.</summary>
    public ulong R10 { get => registers.R10; set => registers.R10 = value; }

    /// <summary>r11.</summary>
    public ulong R11 { get => registers.R11; set => registers.R11 = value; }

    /// <summary>r12.</summary>
    public ulong R12 { get => registers.R12; set => registers.R12 = value; }

    /// <summary>r13.</summary>
    public ulong R13 { get => registers.R13; set => registers.R13 = value; }

    /// <summary>r14.</summary>
    public ulong R14 { get => registers.R14; set => registers.R14 = value; }

    /// <summary>r15.</summary>
    public ulong R15 { get => registers.R15; set => registers.R15 = value; }

    /// <summary>The flags register.</summary>
    public ulong Rflags { get => registers.Rflags; set => registers.Rflags = value; }

    /// <summary>The stack pointer at the hooked instruction.</summary>
    public ulong Rsp => registers.Rsp;

    /// <summary>The address of the instruction the thread runs next. It reads as the hooked address until the handler sets it.</summary>
    /// <remarks>Setting it to another address skips the hooked instruction and continues there.</remarks>
    /// <exception cref="InvalidOperationException">The address lies inside a recorded patch or hook past its first byte.</exception>
    public ulong Rip
    {
        get => registers.Rip == trampoline ? address : registers.Rip;
        set
        {
            if (value == address)
            {
                registers.Rip = trampoline;
                return;
            }
            if (findInteriorOwner(value) is string owner)
            {
                throw new InvalidOperationException($"Rip cannot be set to 0x{value:X}, which lies inside code {owner} changed, past the first byte of that change.");
            }
            registers.Rip = value;
        }
    }

    /// <summary>xmm0.</summary>
    public ref Xmm Xmm0 => ref registers.Xmm[0];

    /// <summary>xmm1.</summary>
    public ref Xmm Xmm1 => ref registers.Xmm[1];

    /// <summary>xmm2.</summary>
    public ref Xmm Xmm2 => ref registers.Xmm[2];

    /// <summary>xmm3.</summary>
    public ref Xmm Xmm3 => ref registers.Xmm[3];

    /// <summary>xmm4.</summary>
    public ref Xmm Xmm4 => ref registers.Xmm[4];

    /// <summary>xmm5.</summary>
    public ref Xmm Xmm5 => ref registers.Xmm[5];

    /// <summary>xmm6.</summary>
    public ref Xmm Xmm6 => ref registers.Xmm[6];

    /// <summary>xmm7.</summary>
    public ref Xmm Xmm7 => ref registers.Xmm[7];

    /// <summary>xmm8.</summary>
    public ref Xmm Xmm8 => ref registers.Xmm[8];

    /// <summary>xmm9.</summary>
    public ref Xmm Xmm9 => ref registers.Xmm[9];

    /// <summary>xmm10.</summary>
    public ref Xmm Xmm10 => ref registers.Xmm[10];

    /// <summary>xmm11.</summary>
    public ref Xmm Xmm11 => ref registers.Xmm[11];

    /// <summary>xmm12.</summary>
    public ref Xmm Xmm12 => ref registers.Xmm[12];

    /// <summary>xmm13.</summary>
    public ref Xmm Xmm13 => ref registers.Xmm[13];

    /// <summary>xmm14.</summary>
    public ref Xmm Xmm14 => ref registers.Xmm[14];

    /// <summary>xmm15.</summary>
    public ref Xmm Xmm15 => ref registers.Xmm[15];
}
