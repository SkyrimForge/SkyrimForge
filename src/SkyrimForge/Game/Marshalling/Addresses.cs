using System;

namespace SkyrimForge.Game;

internal static class Addresses
{
    // User mode on Win64, minus the first 64KB, which is never mapped.
    private static readonly nint lowestAddress = 0x10000;
    private static readonly nint highestAddress = unchecked((nint)0x7FFFFFFFFFFF);

    internal static bool CouldBeAddress(nint address) => address >= lowestAddress && address <= highestAddress;

    internal static nint Check(nint address)
    {
        if (CouldBeAddress(address))
        {
            return address;
        }

        throw new InvalidOperationException(address == 0
            ? "The address is 0. Such a view comes from default, from new, from an OrDefault call on a list of views, "
                + "or from the game as a parameter of a replaced function, which Exists tests."
            : $"The address 0x{address:X} is outside user-mode memory, so the game did not return it. Reading through it would end the process.");
    }
}
