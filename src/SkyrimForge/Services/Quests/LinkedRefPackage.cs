using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>A package record whose location or target is the linked reference of the actor running it, with the keyword of that link.</summary>
/// <param name="PackageId">The package record.</param>
/// <param name="KeywordId">The keyword of the link the package's data specifies. A null value selects the default link.</param>
public sealed record LinkedRefPackage(FormId<Package> PackageId, FormId<Keyword>? KeywordId)
{
    /// <summary>Skyrim.esm's DefaultSandboxLinkCustom02512, which sandboxes within 512 units of the link with the keyword LinkCustom02.</summary>
    public static LinkedRefPackage Sandbox { get; } = new(new FormId<Package>(0x000DD837), new FormId<Keyword>(0x0005D5E7));

    /// <summary>Skyrim.esm's dunKatariahSceneSailorsSit, which sits on furniture within 64 units of the link with the keyword 0005D5FE.</summary>
    public static LinkedRefPackage Sit { get; } = new(new FormId<Package>(0x001094BE), new FormId<Keyword>(0x0005D5FE));

    /// <summary>Skyrim.esm's defaultHoldPositionLinkedRef512WithTravel, whose location is within 512 units of the default link.</summary>
    public static LinkedRefPackage HoldPosition { get; } = new(new FormId<Package>(0x00101C01), null);

    /// <summary>Skyrim.esm's defaultTravelToLinkCustom01, whose location is the link with the keyword LinkCustom01. The actor walks.</summary>
    /// <remarks>As a scene's package action it completes once the actor arrives. An AI-driven player performs it.</remarks>
    public static LinkedRefPackage Travel { get; } = new(new FormId<Package>(0x000235E0), new FormId<Keyword>(0x0005D5E6));

    /// <summary>Skyrim.esm's DefaultMoveToCustom01Run, whose location is within 64 units of the link with the keyword LinkCustom01. The actor runs.</summary>
    /// <remarks>As a scene's package action it completes once the actor arrives. An actor that runs to the player with it runs into the player.</remarks>
    public static LinkedRefPackage TravelAtRun { get; } = new(new FormId<Package>(0x00095FF2), new FormId<Keyword>(0x0005D5E6));

    /// <summary>Skyrim.esm's TestPackagesFollowwLeaderChicken, whose target is the link with the keyword Testchic, followed at between 128 and 256 units.</summary>
    public static LinkedRefPackage Follow { get; } = new(new FormId<Package>(0x000B2848), new FormId<Keyword>(0x000B2847));

    /// <summary>Skyrim.esm's UseIdleMarkerLinkCustom01, whose marker is the link with the keyword LinkCustom01.</summary>
    /// <remarks>The actor sometimes runs the package without moving to the marker.</remarks>
    public static LinkedRefPackage UseIdleMarker { get; } = new(new FormId<Package>(0x000978E3), new FormId<Keyword>(0x0005D5E6));

    /// <summary>
    /// Skyrim.esm's dunKnifepointRidgeSleepPackage, whose location is within 500 units of the link with the keyword LinkCustom01,
    /// and which runs only during its schedule, from 22:00 for 12 hours.
    /// </summary>
    /// <remarks>
    /// The actor sleeps in a linked bed that it owns, itself or through a faction, and sits in furniture near a linked bed of another owner.
    /// As a scene's package action it completes three seconds after the actor falls asleep, which ends its phase and wakes the actor.
    /// A later <see cref="SceneAction.EndPhaseIndex"/> leaves the actor asleep until that phase ends.
    /// </remarks>
    public static LinkedRefPackage Sleep { get; } = new(new FormId<Package>(0x000BBC68), new FormId<Keyword>(0x0005D5E6));
}
