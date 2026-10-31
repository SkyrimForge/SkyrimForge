using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>The running game's executable, as loaded in this process.</summary>
public static unsafe partial class GameModule
{
    private const uint codeSegment = 0; // REL::Segment::textx
    private const int newHeaderOffset = 0x3C; // IMAGE_DOS_HEADER.e_lfanew
    private const int imageSizeOffset = 0x50; // IMAGE_NT_HEADERS64.OptionalHeader.SizeOfImage

    /// <summary>The address the executable is loaded at.</summary>
    public static nint Base { get; } = SF_Module_Base();

    /// <summary>The executable's size in memory, in bytes.</summary>
    public static int Size { get; } = GameMemory.AsRef<int>(Base + GameMemory.AsRef<int>(Base + newHeaderOffset) + imageSizeOffset);

    /// <summary>The executable's version, such as 1.6.1170.0.</summary>
    public static Version Version { get; } = ReadVersion();

    /// <summary>Every address in the executable's code at which a pattern matches, in ascending order.</summary>
    /// <remarks>Each call reads the whole code segment.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <c>null</c>.</exception>
    public static IReadOnlyList<nint> Find(BytePattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        nint begin;
        nuint length;
        SF_Module_Segment(codeSegment, &begin, &length);
        ReadOnlySpan<byte> code = new((void*)begin, checked((int)length));
        List<nint> matches = [];
        int start = 0;
        int index;
        while ((index = pattern.IndexOf(code[start..])) >= 0)
        {
            matches.Add(begin + start + index);
            start += index + 1;
        }
        return matches;
    }

    private static Version ReadVersion()
    {
        ushort* parts = stackalloc ushort[4];
        SF_Module_Version(parts);
        return new Version(parts[0], parts[1], parts[2], parts[3]);
    }

    [LibraryImport(NativeRuntime.Library)]
    private static partial nint SF_Module_Base();

    [LibraryImport(NativeRuntime.Library)]
    private static partial void SF_Module_Version(ushort* parts);

    [LibraryImport(NativeRuntime.Library)]
    private static partial void SF_Module_Segment(uint name, nint* address, nuint* size);
}
