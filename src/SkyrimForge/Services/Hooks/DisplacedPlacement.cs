namespace SkyrimForge;

/// <summary>The order of an <see cref="InsertionPatch"/>'s code and the instructions it displaces.</summary>
public enum DisplacedPlacement
{
    /// <summary>The displaced instructions run, then the inserted code.</summary>
    BeforeCode,

    /// <summary>The inserted code runs, then the displaced instructions.</summary>
    AfterCode
}
