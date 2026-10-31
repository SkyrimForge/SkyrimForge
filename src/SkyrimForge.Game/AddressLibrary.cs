using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Resolves address library identifiers on the running game.</summary>
public static partial class AddressLibrary
{
    /// <summary>Whether the library contains the identifier the function states for the running edition.</summary>
    /// <remarks>
    /// The Skyrim VR library contains a subset of the Special Edition identifiers. Do not call a bound member whose identifier is missing, because CommonLib ends the process.
    /// </remarks>
    public static bool Contains(GameFunction function) => SF_AddressLibrary_Names(function.SpecialEdition, function.AnniversaryEdition, function.VR);

    /// <summary>Whether the library contains the identifier the variable states for the running edition.</summary>
    /// <remarks>The Skyrim VR library contains a subset of the Special Edition identifiers.</remarks>
    public static bool Contains(GameVariable variable) => SF_AddressLibrary_Names(variable.SpecialEdition, variable.AnniversaryEdition, variable.VR);

    /// <summary>The address of a function on the running edition.</summary>
    /// <exception cref="InvalidOperationException">The library does not contain the identifier the function states for the running edition.</exception>
    public static nint Resolve(GameFunction function)
    {
        if (Contains(function))
        {
            return SF_Hook_FunctionAddress(function.SpecialEdition, function.AnniversaryEdition, function.VR);
        }
        throw new InvalidOperationException($"The address library does not contain the identifier of {function} on this runtime.");
    }

    /// <summary>The address of the instruction at an offset into a function, using the offset stated for the running edition.</summary>
    /// <exception cref="InvalidOperationException">The library does not contain the identifier the function states for the running edition.</exception>
    public static nint Resolve(GameFunction function, GameOffset offset)
    {
        uint editionOffset = CurrentGameEdition.Value switch
        {
            GameEdition.AnniversaryEdition => offset.AnniversaryEdition,
            GameEdition.VR => offset.VR,
            _ => offset.SpecialEdition
        };
        return Resolve(function) + (nint)editionOffset;
    }

    /// <summary>The address of a variable on the running edition.</summary>
    /// <exception cref="InvalidOperationException">The library does not contain the identifier the variable states for the running edition.</exception>
    public static nint Resolve(GameVariable variable)
    {
        if (Contains(variable))
        {
            return SF_Hook_FunctionAddress(variable.SpecialEdition, variable.AnniversaryEdition, variable.VR);
        }
        throw new InvalidOperationException($"The address library does not contain the identifier of {variable} on this runtime.");
    }

    [LibraryImport(NativeRuntime.Library)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool SF_AddressLibrary_Names(ulong se, ulong ae, ulong vr);

    [LibraryImport(NativeRuntime.Library)]
    private static partial nint SF_Hook_FunctionAddress(ulong se, ulong ae, ulong vr);
}
