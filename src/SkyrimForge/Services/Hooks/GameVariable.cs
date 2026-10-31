namespace SkyrimForge;

/// <summary>A game variable, by its Address Library ID on each runtime.</summary>
public readonly record struct GameVariable(ulong SpecialEdition, ulong AnniversaryEdition, ulong VR)
{
    /// <summary>Creates a variable whose Special Edition ID also applies to VR.</summary>
    public GameVariable(ulong specialEditionAndVR, ulong anniversaryEdition) : this(specialEditionAndVR, anniversaryEdition, specialEditionAndVR) { }
}
