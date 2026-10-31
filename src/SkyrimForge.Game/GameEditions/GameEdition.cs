using System;

namespace SkyrimForge.Game;

/// <summary>An edition of Skyrim. The values match CommonLib's <c>REL::Module::Runtime</c>.</summary>
[Flags]
public enum GameEdition
{
    /// <summary>None of the three.</summary>
    None = 0,

    /// <summary>Skyrim Special Edition 1.6.0 and later.</summary>
    AnniversaryEdition = 1,

    /// <summary>Special Edition below 1.6.0.</summary>
    SpecialEdition = 2,

    /// <summary>Skyrim VR.</summary>
    VR = 4
}
