using System;
using System.Runtime.InteropServices;

namespace SkyrimForge;

/// <summary>One 128-bit SSE register, as an <see cref="InstructionHandler"/> reads and changes it through <see cref="CpuContext"/>.</summary>
/// <remarks>A scalar <c>float</c> or <c>double</c> argument or result occupies the low lane, which <see cref="Single"/> and <see cref="Double"/> read and write.</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Xmm
{
    private ulong low;
    private ulong high;

    /// <summary>The low 32 bits, as a <c>float</c>.</summary>
    public float Single
    {
        readonly get => BitConverter.Int32BitsToSingle((int)low);
        set => low = (low & 0xFFFF_FFFF_0000_0000) | (uint)BitConverter.SingleToInt32Bits(value);
    }

    /// <summary>The low 64 bits, as a <c>double</c>.</summary>
    public double Double
    {
        readonly get => BitConverter.Int64BitsToDouble((long)low);
        set => low = (ulong)BitConverter.DoubleToInt64Bits(value);
    }

    /// <summary>All 128 bits.</summary>
    public UInt128 Bits
    {
        readonly get => new(high, low);
        set
        {
            low = (ulong)value;
            high = (ulong)(value >> 64);
        }
    }
}
