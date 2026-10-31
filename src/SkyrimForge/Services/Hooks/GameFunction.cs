namespace SkyrimForge;

/// <summary>A game function, by its Address Library ID on each runtime. An ID the address library lacks resolves to no address.</summary>
public readonly partial record struct GameFunction(ulong SpecialEdition, ulong AnniversaryEdition, ulong VR)
{
    /// <summary>Creates a function whose Special Edition ID also applies to VR.</summary>
    public GameFunction(ulong specialEditionAndVR, ulong anniversaryEdition) : this(specialEditionAndVR, anniversaryEdition, specialEditionAndVR) { }

    /// <summary>The game's main loop, which calls <see cref="Main__Update"/> once a frame.</summary>
    public static GameFunction MainLoop { get; } = new(35551, 36544);

    /// <summary>The game's own work for one frame, called from <see cref="MainLoop"/>.</summary>
    public static GameFunction Main__Update { get; } = new(35565, 36564);
}
