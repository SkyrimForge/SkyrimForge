using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

internal static class BindingUtility
{
    private const string nullStringMessage = "The game returned a null pointer for a string expected to be non-null.";

    internal static nint[] GetAddresses<T>(IEnumerable<T> items) where T : IAddressable
    {
        return [.. items.Select(item => item.Address)];
    }

    internal static nint[] GetAddresses<T>(IEnumerable<T> items, Func<T, nint> address)
    {
        return [.. items.Select(address)];
    }

    internal static string? CopyString(nint address, int length)
    {
        return address != 0 ? Marshal.PtrToStringUTF8(address, length) : null;
    }

    internal static string CopyFixedString(nint address, int width)
    {
        if (address == 0)
        {
            throw new InvalidOperationException("The game returned a zero address for a fixed-width string.");
        }
        int length = Enumerable.Range(0, width).TakeWhile(offset => Marshal.ReadByte(address, offset) != 0).Count();
        return Marshal.PtrToStringUTF8(address, length);
    }

    internal static string CopyNonNullString(nint address)
    {
        return Marshal.PtrToStringUTF8(address) ?? throw new InvalidOperationException(nullStringMessage);
    }

    internal static string CopyNonNullString(nint address, int length)
    {
        return address != 0 ? Marshal.PtrToStringUTF8(address, length) : throw new InvalidOperationException(nullStringMessage);
    }

    // The all-ones value is the member declared as -1. CommonLib's comparison narrows that member to the storage type, and a cast to the wider mirror does not undo the narrowing.
    internal static int WidenEnum(byte stored) => stored == byte.MaxValue ? -1 : stored;

    internal static int WidenEnum(ushort stored) => stored == ushort.MaxValue ? -1 : stored;

    // Callers rely on C# evaluating arguments left to right. The view passed in reads an address the preceding argument assigns.
    internal static TReturned WriteBack<TReturned, T>(TReturned returnValue, T view, out T target)
    {
        target = view;
        return returnValue;
    }
}
