using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SkyrimForge.Game;

internal static class Sequences
{
    internal delegate int Copy<TEntry>(ref TEntry first, int capacity) where TEntry : struct;

    internal static IReadOnlyList<TResult> Read<TEntry, TResult>(Copy<TEntry> copy, Func<TEntry, TResult?> selector) where TEntry : struct where TResult : struct
    {
        TEntry[] buffer = Gather(copy, out int total);
        return [.. buffer.Take(total).Select(selector).WhereNotNull()];
    }

    internal static IReadOnlyList<TResult?> ReadNullable<TEntry, TResult>(Copy<TEntry> copy, Func<TEntry, TResult?> selector) where TEntry : struct where TResult : struct
    {
        TEntry[] buffer = Gather(copy, out int total);
        return [.. buffer.Take(total).Select(selector)];
    }

    internal static IReadOnlyList<string> ReadStrings(Copy<nint> copy)
    {
        nint[] buffer = Gather(copy, out int total);
        return [.. buffer.Take(total).Select(entry => Marshal.PtrToStringUTF8(entry)).WhereNotNull()];
    }

    internal static string? ReadString(Copy<byte> copy)
    {
        string? text = ReadStringOrEmpty(copy);
        return !string.IsNullOrEmpty(text) ? text : null;
    }

    internal static string? ReadStringOrEmpty(Copy<byte> copy)
    {
        byte[] buffer = Gather(copy, out int total);
        if (total >= 0)
        {
            // Clamped because Gather's second copy call reports a fresh count, which can exceed the buffer sized for the first.
            return Encoding.UTF8.GetString(buffer, 0, Math.Min(total, buffer.Length));
        }
        return null;
    }

    private static TEntry[] Gather<TEntry>(Copy<TEntry> copy, out int total) where TEntry : struct
    {
        const int initialLength = 256;
        TEntry[] buffer = new TEntry[initialLength];
        total = copy(ref buffer[0], buffer.Length);
        if (total > buffer.Length)
        {
            buffer = new TEntry[total];
            total = copy(ref buffer[0], buffer.Length);
        }
        return buffer;
    }
}
