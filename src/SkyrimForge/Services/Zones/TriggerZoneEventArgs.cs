using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Arguments for <see cref="ITriggerZone.OnEnter"/> and <see cref="ITriggerZone.OnLeave"/>.</summary>
/// <param name="CenterId">The reference the zone's center function returned at the check. It is null if the function returned none.</param>
public readonly record struct TriggerZoneEventArgs(FormId<ObjectReference>? CenterId);
