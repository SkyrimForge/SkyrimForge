using System;

namespace SkyrimForge;

/// <summary>Replaces whole instructions with a call to <see cref="Target"/>, after which the code following the expected bytes runs.</summary>
/// <remarks>
/// The call occupies the first five expected bytes, and the rest become nops.
/// The expected bytes must end on an instruction, and no branch elsewhere in the function may land inside them.
/// The target must begin an instruction, and must not lie inside a recorded patch or hook past its first byte.
/// <para>
/// The entry point of a managed method is usually beyond a 32-bit displacement, so redirect game code to managed code through <see cref="IFunctionHooks"/>.
/// Memory from <see cref="ICodePatcher.AllocateCode"/> is within reach. Finish writing code there before applying the call.
/// </para>
/// </remarks>
public sealed class CallPatch : CodePatch
{
    /// <summary>Creates a patch replacing the expected bytes at <paramref name="address"/> with a call to <paramref name="target"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="address"/> or <paramref name="target"/> is 0, <paramref name="expected"/> is shorter than five bytes,
    /// or <paramref name="target"/> lies farther from <paramref name="address"/> than a 32-bit displacement reaches.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> is <c>null</c>.</exception>
    public CallPatch(nint address, BytePattern expected, nint target) : base(address, expected)
    {
        ThrowIfShorterThanBranch();
        ThrowIfOutOfReach(target);
        Target = target;
    }

    /// <summary>The address the call reaches.</summary>
    public nint Target { get; }
}
