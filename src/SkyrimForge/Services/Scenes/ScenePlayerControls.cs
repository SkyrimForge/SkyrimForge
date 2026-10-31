using System;

namespace SkyrimForge;

/// <summary>
/// The player controls a <see cref="QuestScene"/> disables while it plays. Each member is named for a parameter of Papyrus's Game.DisablePlayerControls.
/// </summary>
[Flags]
public enum ScenePlayerControls
{
    /// <summary>No control.</summary>
    None = 0,

    /// <summary>The controls abMovement disables.</summary>
    Movement = 1,

    /// <summary>The controls abFighting disables.</summary>
    Fighting = 2,

    /// <summary>The controls abCamSwitch disables.</summary>
    CameraSwitch = 4,

    /// <summary>The controls abLooking disables.</summary>
    Looking = 8,

    /// <summary>The controls abSneaking disables.</summary>
    Sneaking = 16,

    /// <summary>The controls abMenu disables.</summary>
    Menu = 32,

    /// <summary>The controls abActivate disables.</summary>
    Activate = 64,

    /// <summary>The controls abJournalTabs disables.</summary>
    JournalTabs = 128
}
