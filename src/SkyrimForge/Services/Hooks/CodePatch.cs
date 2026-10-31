using System;

namespace SkyrimForge;

/// <summary>A patch <see cref="ICodePatcher"/> applies to code once the bytes at its address match the expected ones.</summary>
public abstract class CodePatch
{
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is 0.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> is <c>null</c>.</exception>
    private protected CodePatch(nint address, BytePattern expected)
    {
        if (address == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(address), "The address is 0.");
        }
        ArgumentNullException.ThrowIfNull(expected);
        Address = address;
        Expected = expected;
    }

    /// <summary>The address of the first expected byte.</summary>
    public nint Address { get; }

    /// <summary>The bytes the code at <see cref="Address"/> must match. The patch covers them, so another patch overlapping them throws.</summary>
    public BytePattern Expected { get; }

    private protected const int BranchLength = 5;

    /// <exception cref="ArgumentOutOfRangeException">The expected bytes are fewer than a jump.</exception>
    private protected void ThrowIfShorterThanBranch()
    {
        if (Expected.Length < BranchLength)
        {
            throw new ArgumentOutOfRangeException("expected", $"The {Expected.Length} expected bytes are fewer than the {BranchLength} a jump occupies.");
        }
    }

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="target"/> is 0 or lies farther from <see cref="Address"/> than a 32-bit displacement reaches.</exception>
    private protected void ThrowIfOutOfReach(nint target)
    {
        if (target == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(target), "The target is 0.");
        }
        Int128 displacement = (Int128)target - ((Int128)Address + BranchLength);
        if (displacement < int.MinValue || displacement > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(target),
                $"The target 0x{target:X} lies farther from 0x{Address:X} than a 32-bit displacement reaches. Allocate the target with ICodePatcher.AllocateCode near 0x{Address:X}.");
        }
    }
}
