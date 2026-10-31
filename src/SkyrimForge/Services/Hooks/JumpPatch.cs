using System;

namespace SkyrimForge;

/// <summary>Replaces whole instructions with a jump, so the code at <see cref="CodePatch.Address"/> continues at <see cref="Target"/>.</summary>
/// <remarks>
/// The jump occupies the first five expected bytes, and the rest become nops.
/// The expected bytes must end on an instruction, and no branch elsewhere in the function may land inside them.
/// The target must begin an instruction, and must not lie inside a recorded patch or hook past its first byte.
/// <para>
/// The entry point of a managed method is usually beyond a 32-bit displacement, so redirect game code to managed code through <see cref="IFunctionHooks"/>.
/// Memory from <see cref="ICodePatcher.AllocateCode"/> is within reach. Finish writing code there before applying the jump.
/// </para>
/// </remarks>
public sealed class JumpPatch : CodePatch
{
    /// <summary>Creates a patch replacing the expected bytes at <paramref name="address"/> with a jump to <paramref name="target"/>.</summary>
    /// <param name="address">The address of the first expected byte.</param>
    /// <param name="expected">The bytes the code at <paramref name="address"/> must match: the whole instructions replaced, at least five bytes.</param>
    /// <param name="target">The address the jump reaches.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="address"/> or <paramref name="target"/> is 0, <paramref name="expected"/> is shorter than five bytes,
    /// or <paramref name="target"/> lies farther from <paramref name="address"/> than a 32-bit displacement reaches.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> is <c>null</c>.</exception>
    public JumpPatch(nint address, BytePattern expected, nint target) : base(address, expected)
    {
        ThrowIfShorterThanBranch();
        ThrowIfOutOfReach(target);
        Target = target;
    }

    /// <summary>The address the jump reaches.</summary>
    public nint Target { get; }
}
