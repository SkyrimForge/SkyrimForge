using System;

namespace SkyrimForge;

/// <summary>Replaces bytes within the expected bytes, such as the constant of one instruction.</summary>
public sealed class BytePatch : CodePatch
{
    /// <summary>
    /// Creates a patch writing <paramref name="replacement"/> at <paramref name="offset"/> bytes past <paramref name="address"/>.
    /// The code at <paramref name="address"/> must match <paramref name="expected"/>, which contains the replaced bytes.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="address"/> is 0, <paramref name="offset"/> is negative, <paramref name="replacement"/> is empty,
    /// or the replaced bytes extend past the end of <paramref name="expected"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> is <c>null</c>.</exception>
    public BytePatch(nint address, BytePattern expected, int offset, ReadOnlySpan<byte> replacement) : base(address, expected)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (replacement.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(replacement), "The replacement is empty.");
        }
        if (offset > expected.Length - replacement.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(replacement),
                $"The {replacement.Length} replaced bytes at offset {offset} extend past the {expected.Length} expected bytes, which must contain every byte written.");
        }
        Offset = offset;
        Replacement = replacement.ToArray();
    }

    /// <summary>The position in <see cref="CodePatch.Expected"/> of the first replaced byte.</summary>
    public int Offset { get; }

    /// <summary>The bytes written.</summary>
    public ReadOnlyMemory<byte> Replacement { get; }
}
