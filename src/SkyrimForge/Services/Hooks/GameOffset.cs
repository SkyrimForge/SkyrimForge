namespace SkyrimForge;

/// <summary>The offset of an instruction from a function's entry, in bytes, on each runtime.</summary>
/// <remarks>
/// Each offset is valid only for the build it was read from, and a game update can move the instruction.
/// <see cref="IFunctionHooks.FindCallSite(GameFunction, GameFunction, int, int)"/> finds a call site by address library IDs, which are mapped per build.
/// </remarks>
public readonly record struct GameOffset(uint SpecialEdition, uint AnniversaryEdition, uint VR)
{
    /// <summary>Creates an offset whose Special Edition value also applies to VR.</summary>
    public GameOffset(uint specialEditionAndVR, uint anniversaryEdition) : this(specialEditionAndVR, anniversaryEdition, specialEditionAndVR) { }
}
