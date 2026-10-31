using System;

namespace SkyrimForge;

/// <summary>
/// Inserts machine code at an address. The expected bytes become a jump to allocated memory, which runs the code and the displaced instructions, then jumps back.
/// </summary>
/// <remarks>
/// The expected bytes past the displaced ones are not run.
/// The expected bytes and the displaced bytes each end on an instruction, and no branch elsewhere in the function lands inside the expected bytes.
/// <para>
/// No RIP-relative operand or relative branch in the code refers outside it.
/// The code preserves every register, flag, and stack location the game reads afterward, and execution leaves it past its last byte, not through a return.
/// </para>
/// <para>
/// <see cref="ICodePatcher.Apply"/> throws for a displaced instruction with an operand relative to its own address, except displaced bytes that form a single five-byte call.
/// Allocated memory has no unwind data, so an exception or a stack walk passing through a frame there is not unwound correctly.
/// </para>
/// </remarks>
public sealed class InsertionPatch : CodePatch
{
    /// <summary>Creates a patch inserting <paramref name="code"/> at <paramref name="address"/>.</summary>
    /// <param name="address">The address of the first expected byte.</param>
    /// <param name="expected">The bytes the code at <paramref name="address"/> must match: the whole instructions replaced, at least five bytes.</param>
    /// <param name="code">The machine code inserted.</param>
    /// <param name="displacedLength">How many of the expected bytes, from the first, run beside the code: 0 for none.</param>
    /// <param name="placement">Whether the displaced instructions run before or after the code.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="address"/> is 0, <paramref name="expected"/> is shorter than five bytes, <paramref name="code"/> is empty,
    /// <paramref name="displacedLength"/> is negative or greater than the length of <paramref name="expected"/>, or <paramref name="placement"/> is not a defined value.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> is <c>null</c>.</exception>
    public InsertionPatch(nint address, BytePattern expected, ReadOnlySpan<byte> code, int displacedLength, DisplacedPlacement placement) : base(address, expected)
    {
        ThrowIfShorterThanBranch();
        if (code.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(code), "The code is empty.");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(displacedLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(displacedLength, expected.Length);
        if (!Enum.IsDefined(placement))
        {
            throw new ArgumentOutOfRangeException(nameof(placement), $"{placement} is not a DisplacedPlacement value.");
        }
        Code = code.ToArray();
        DisplacedLength = displacedLength;
        Placement = placement;
    }

    /// <summary>The machine code inserted.</summary>
    public ReadOnlyMemory<byte> Code { get; }

    /// <summary>How many of the expected bytes, from the first, run beside the code.</summary>
    public int DisplacedLength { get; }

    /// <summary>Whether the displaced instructions run before or after the code.</summary>
    public DisplacedPlacement Placement { get; }
}
