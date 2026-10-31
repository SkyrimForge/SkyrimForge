using System;
using System.Runtime.CompilerServices;

namespace SkyrimForge.Game;

/// <summary>Reads and writes memory at an address, and converts between addresses and views.</summary>
/// <remarks>An address that maps no memory faults when it is read or written, and the process ends.</remarks>
public static unsafe class GameMemory
{
    /// <summary>A reference to the value of type <typeparamref name="T"/> at an address.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is 0 or outside user-mode memory.</exception>
    public static ref T AsRef<T>(nint address) where T : unmanaged => ref Unsafe.AsRef<T>((void*)Check(address));

    /// <summary>The bytes at an address, as a writable span.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is 0 or outside user-mode memory, or <paramref name="length"/> is negative.</exception>
    public static Span<byte> AsSpan(nint address, int length) => new((void*)Check(address), length);

    /// <summary>Whether the bytes at an address match a pattern.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is 0 or outside user-mode memory.</exception>
    public static bool Matches(nint address, BytePattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        return pattern.Matches(AsSpan(address, pattern.Length));
    }

    /// <summary>A view of type <typeparamref name="T"/> over an address. A view over 0 has no address, and its <c>Exists</c> is false.</summary>
    /// <remarks>The object at the address is not checked, so a view over an object of another type reads that object's memory as this type's.</remarks>
    public static T AsView<T>(nint address) where T : struct, IAddressable => Unsafe.As<nint, T>(ref address); // Every view stores only its address.

    /// <summary>The address a view is over.</summary>
    /// <exception cref="InvalidOperationException">The view has no address, or one outside user-mode memory.</exception>
    public static nint GetAddress<T>(T view) where T : struct, IAddressable => view.Address;

    private static nint Check(nint address)
    {
        if (Addresses.CouldBeAddress(address))
        {
            return address;
        }
        throw new ArgumentOutOfRangeException(nameof(address), $"The address 0x{address:X} is 0 or outside user-mode memory.");
    }
}
