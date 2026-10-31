namespace SkyrimForge.Game;

/// <summary>A location's map marker. <see cref="LocationId"/> is null for a marker whose reference states no location.</summary>
public readonly record struct MapMarker(FormId<Location>? LocationId, FormId<ObjectReference> ReferenceId, FormId<WorldSpace> WorldspaceId, NiPoint3 Position, string Name, MARKER_TYPE Kind, bool IsVisible, bool CanTravelTo);
