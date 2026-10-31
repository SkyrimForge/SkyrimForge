using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="MapMarker"/>, a sequence of them, and <see cref="GameTarget"/>.</summary>
public static class MapMarkerExtensions
{
    // Form IDs, because a reference view is invalid after a save loads.
    private static FormId<ObjectReference>[]? markerReferences;

    extension(GameTarget gameTarget)
    {
        /// <summary>Reads every named map marker of every worldspace.</summary>
        /// <remarks>The first call enumerates each worldspace's persistent cell, so a marker created after it is not included.</remarks>
        public IReadOnlyList<MapMarker> GetMapMarkers()
        {
            if (markerReferences != null)
            {
                return [.. markerReferences.Select(id => id.FormOrNull).WhereNotNull().Select(FromReference).WhereNotNull()];
            }
            MapMarker[] markers =
            [
                .. gameTarget.DataHandler.GetFormArray<WorldSpace>()
                    .Select(worldspace => worldspace.PersistentCell)
                    .WhereNotNull()
                    .SelectMany(cell => cell.GetReferences())
                    .Select(FromReference)
                    .WhereNotNull()
            ];
            // An empty enumeration means the game has loaded no data yet.
            if (markers.Length > 0)
            {
                markerReferences = [.. markers.Select(marker => marker.ReferenceId)];
            }
            return markers;
        }
    }

    extension(IEnumerable<MapMarker> markers)
    {
        /// <summary>The worldspace of the marker whose location contains <paramref name="location"/>, or <c>null</c> if no marker's location contains it.</summary>
        public FormId<WorldSpace>? GetWorldspaceId(Location location) =>
            markers.Where(marker => marker.LocationId != null && location.IsWithin(marker.LocationId.Value))
                .Select(marker => marker.WorldspaceId).FirstOrNull();

        /// <summary>
        /// Returns the marker in <paramref name="worldspaceId"/> nearest <paramref name="position"/> in the horizontal plane, among those that pass <paramref name="predicate"/>.
        /// A null <paramref name="worldspaceId"/> matches no marker, and a null <paramref name="predicate"/> accepts every marker.
        /// </summary>
        public MapMarker? GetNearest(FormId<WorldSpace>? worldspaceId, NiPoint3 position, Func<MapMarker, bool>? predicate = null) =>
            markers.Where(marker => marker.WorldspaceId == worldspaceId && (predicate == null || predicate(marker)))
                .OrderBy(marker => marker.Position.GetHorizontalDistance(position))
                .FirstOrNull();
    }

    extension(MapMarker)
    {
        /// <summary>Finds the map marker whose reference has the form ID <paramref name="referenceId"/>, or null if that reference is no named map marker.</summary>
        public static MapMarker? FindByReferenceId(FormId<ObjectReference> referenceId) => referenceId.FormOrNull is { } reference ? FromReference(reference) : null;
    }

    private static MapMarker? FromReference(ObjectReference reference)
    {
        if (reference.ExtraList.GetByType(ExtraDataType.kMapMarker)?.AsExtraMapMarker()?.MapData is not { } mapData
            || reference.GetWorldspace()?.FormId is not { } worldspace
            || mapData.LocationName.GetFullName() is not { Length: > 0 } name)
        {
            return null;
        }
        return new MapMarker(reference.GetCurrentLocation()?.FormId, reference.FormId, worldspace, reference.GetPosition(), name, mapData.Type,
            mapData.Flags.HasFlag(MapMarkerData.Flag.kVisible), mapData.Flags.HasFlag(MapMarkerData.Flag.kCanTravelTo));
    }
}
