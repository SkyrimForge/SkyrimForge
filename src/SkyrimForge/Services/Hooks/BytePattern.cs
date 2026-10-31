using System;
using System.Globalization;
using System.Linq;

namespace SkyrimForge;

/// <summary>A byte sequence in which some positions match any byte, written as hexadecimal pairs separated by spaces, such as "48 8D 0D ?? ?? ?? ??".</summary>
public sealed class BytePattern
{
    private readonly byte?[] bytes;

    /// <summary>Parses a pattern. Each position is two hexadecimal digits, or "?" or "??" for a position that matches any byte.</summary>
    /// <exception cref="FormatException"><paramref name="pattern"/> has no positions, or a position is neither two hexadecimal digits nor a wildcard.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <c>null</c>.</exception>
    public BytePattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        string[] tokens = pattern.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            throw new FormatException("The pattern has no positions.");
        }
        bytes = [.. tokens.Select(ParseToken)];
    }

    /// <summary>The number of positions.</summary>
    public int Length => bytes.Length;

    /// <summary>Whether a span begins with the pattern. A span shorter than the pattern does not match.</summary>
    public bool Matches(ReadOnlySpan<byte> span)
    {
        if (span.Length < bytes.Length)
        {
            return false;
        }
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] is byte value && span[i] != value)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>The index of the first position in a span at which the pattern matches, or -1 if it matches nowhere.</summary>
    public int IndexOf(ReadOnlySpan<byte> span)
    {
        int anchor = Array.FindIndex(bytes, value => value.HasValue);
        if (anchor < 0)
        {
            return span.Length >= bytes.Length ? 0 : -1;
        }
        byte anchorValue = bytes[anchor].GetValueOrDefault();
        int start = 0;
        while (start + bytes.Length <= span.Length)
        {
            int found = span[(start + anchor)..].IndexOf(anchorValue);
            if (found < 0)
            {
                return -1;
            }
            int candidate = start + found;
            if (Matches(span[candidate..]))
            {
                return candidate;
            }
            start = candidate + 1;
        }
        return -1;
    }

    internal void CopyTo(Span<byte> values, Span<byte> mask)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            values[i] = bytes[i].GetValueOrDefault();
            mask[i] = bytes[i].HasValue ? (byte)1 : (byte)0;
        }
    }

    /// <summary>The pattern with each position as two uppercase hexadecimal digits or "??".</summary>
    public override string ToString() => string.Join(" ", bytes.Select(value => value is byte fixedValue ? fixedValue.ToString("X2", CultureInfo.InvariantCulture) : "??"));

    private static byte? ParseToken(string token)
    {
        if (token is "?" or "??")
        {
            return null;
        }
        if (token.Length == 2 && byte.TryParse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte value))
        {
            return value;
        }
        throw new FormatException($"The position \"{token}\" is neither two hexadecimal digits nor a wildcard.");
    }
}
