using System;

namespace SkyrimForge;

/// <summary>The modifier keys of a hotkey. The left and right keys of each pair are treated as the same key.</summary>
[Flags]
public enum ModifierKeys
{
    /// <summary>No modifier key.</summary>
    None = 0,

    /// <summary>A Shift key.</summary>
    Shift = 1,

    /// <summary>A Ctrl key.</summary>
    Control = 2,

    /// <summary>An Alt key.</summary>
    Alt = 4
}
