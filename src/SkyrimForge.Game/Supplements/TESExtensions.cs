using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="TES"/>.</summary>
public static class TESExtensions
{
    extension(TES tes)
    {
        /// <summary>Returns <paramref name="position"/> with its Z at the height of the terrain, which lies beneath roads, bridges, and ruin floors.</summary>
        /// <exception cref="InvalidOperationException">The land at <paramref name="position"/> is not loaded.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public NiPoint3 GetPositionOnLand(NiPoint3 position)
        {
            if (tes.GetLandHeight(position, out float height))
            {
                return position with { Z = height };
            }
            throw new InvalidOperationException($"The land at {position} is not loaded, so {nameof(TESBindings.GetLandHeight)} returned false.");
        }

        /// <summary>
        /// Returns <paramref name="position"/> with its Z at the higher of the collision surface and the terrain beneath it.
        /// The result is null if neither is loaded.
        /// </summary>
        /// <remarks>The ray strikes actors, as <see cref="GetSurfaceUnder"/> describes. Call on the game's thread.</remarks>
        public NiPoint3? GetPositionOnGround(NiPoint3 position, float rayStartHeight, float rayEndDepth)
        {
            float? hitHeight = tes.GetSurfaceUnder(position, rayStartHeight, rayEndDepth) is ({ } hit, _) ? hit.Z : null;
            float? landHeight = tes.GetLandHeight(position, out float height) ? height : null;
            float? groundHeight = hitHeight != null && landHeight != null ? Math.Max(hitHeight.Value, landHeight.Value) : hitHeight ?? landHeight;
            return groundHeight != null ? position with { Z = groundHeight.Value } : null;
        }

        /// <summary>
        /// Casts a ray down through <paramref name="position"/> and returns the point it strikes and that face's normal.
        /// The result is null if the ray strikes nothing or the position is in no loaded cell.
        /// </summary>
        /// <remarks>The ray strikes actors on every layer tested, so a ray through the player's position returns a point on the player. Call on the game's thread.</remarks>
        public SurfaceHit? GetSurfaceUnder(NiPoint3 position, float rayStartHeight, float rayEndDepth, COL_LAYER layer = COL_LAYER.kLOS)
        {
            if (tes.GetCell(position)?.GetbhkWorld() is not { } world)
            {
                return null;
            }
            float scale = bhkWorld.GetWorldScale();
            NiPoint3 rayStart = position with { Z = position.Z + rayStartHeight };
            NiPoint3 rayEnd = position with { Z = position.Z - rayEndDepth };
            bhkPickData pick = bhkPickData.New();
            try
            {
                // The view addresses the field, so writing through the copy writes the pick data.
                CFilter filter = pick.RayInput.FilterInfo;
                filter.Filter = (uint)layer;
                Write(pick.RayInput.From, rayStart, scale);
                Write(pick.RayInput.To, rayEnd, scale);
                if (!world.PickObject(pick) || !pick.RayOutput.HasHit())
                {
                    return null;
                }
                float fraction = pick.RayOutput.HitFraction;
                hkVector4 normal = pick.RayOutput.Normal;
                return new SurfaceHit(new NiPoint3(rayStart.X, rayStart.Y, rayStart.Z + ((rayEnd.Z - rayStart.Z) * fraction)),
                    new NiPoint3(normal.QuadX, normal.QuadY, normal.QuadZ));
            }
            finally
            {
                pick.Delete();
            }
        }

        /// <summary>
        /// Returns the angles in radians that lay an object of width <paramref name="span"/> flush on the surface under <paramref name="position"/>,
        /// with the heading <paramref name="yawDegrees"/>. The result is null if a ray at any of the four edges strikes nothing.
        /// </summary>
        /// <remarks>The rays strike actors, as <see cref="GetSurfaceUnder"/> describes. Call on the game's thread.</remarks>
        public NiPoint3? GetSurfaceAngle(NiPoint3 position, float yawDegrees, float span, float rayStartHeight, float rayEndDepth, COL_LAYER layer = COL_LAYER.kLOS)
        {
            float halfSpan = span / 2f;
            if (tes.GetSurfaceUnder(position with { Y = position.Y + halfSpan }, rayStartHeight, rayEndDepth, layer) is not ({ } north, _)
                || tes.GetSurfaceUnder(position with { Y = position.Y - halfSpan }, rayStartHeight, rayEndDepth, layer) is not ({ } south, _)
                || tes.GetSurfaceUnder(position with { X = position.X + halfSpan }, rayStartHeight, rayEndDepth, layer) is not ({ } east, _)
                || tes.GetSurfaceUnder(position with { X = position.X - halfSpan }, rayStartHeight, rayEndDepth, layer) is not ({ } west, _))
            {
                return null;
            }
            return new NiPoint3(-GetSlope(north.Z - south.Z, span), GetSlope(east.Z - west.Z, span), (float)(yawDegrees * Math.PI / 180.0));
        }

        /// <summary>
        /// Returns the steeper of the north and east tilts of the surface under <paramref name="position"/>, in degrees.
        /// The result is null if a ray at any of the four edges strikes nothing.
        /// </summary>
        /// <remarks>The rays strike actors, as <see cref="GetSurfaceUnder"/> describes. Call on the game's thread.</remarks>
        public float? GetSurfaceTilt(NiPoint3 position, float span, float rayStartHeight, float rayEndDepth)
        {
            if (tes.GetSurfaceAngle(position, 0f, span, rayStartHeight, rayEndDepth) is not { } surfaceAngle)
            {
                return null;
            }
            return (float)(Math.Max(Math.Abs(surfaceAngle.X), Math.Abs(surfaceAngle.Y)) * 180.0 / Math.PI);
        }

        /// <summary>
        /// Returns the angles in radians that lay an object of width <paramref name="span"/> flush on the terrain under <paramref name="position"/>,
        /// with the heading <paramref name="yawDegrees"/>. The result is null if the land at any of the four edges is not loaded.
        /// </summary>
        /// <remarks>Call on the game's thread.</remarks>
        public NiPoint3? GetLandAngle(NiPoint3 position, float yawDegrees, float span)
        {
            float halfSpan = span / 2f;
            if (!tes.GetLandHeight(position with { Y = position.Y + halfSpan }, out float north)
                || !tes.GetLandHeight(position with { Y = position.Y - halfSpan }, out float south)
                || !tes.GetLandHeight(position with { X = position.X + halfSpan }, out float east)
                || !tes.GetLandHeight(position with { X = position.X - halfSpan }, out float west))
            {
                return null;
            }
            return new NiPoint3(-GetSlope(north - south, span), GetSlope(east - west, span), (float)(yawDegrees * Math.PI / 180.0));
        }

        /// <summary>Returns the loaded exterior cells within <paramref name="radius"/> of <paramref name="center"/>, or the interior cell the player is in.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<Cell> GetCellsInRange(NiPoint3 center, float radius)
        {
            if (tes.InteriorCell is { } interiorCell)
            {
                return [interiorCell];
            }
            if (tes.GridCells is not { } gridCells)
            {
                return [];
            }
            int sideLength = (int)gridCells.Length;
            return [.. Enumerable.Range(0, sideLength)
                .SelectMany(x => Enumerable.Range(0, sideLength).Select(y => gridCells.GetCell((uint)x, (uint)y)))
                .WhereNotNull()
                .Where(cell => cell.IsAttached() && Reaches(cell, center, radius))];
        }

        /// <summary>Returns every reference within <paramref name="radius"/> of <paramref name="center"/>, across every loaded cell in range.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<ObjectReference> GetReferencesInRange<TCenter>(TCenter center, float radius) where TCenter : IObjectReference =>
            tes.GetReferencesInRange(center.GetPosition(), radius);

        /// <summary>Returns every reference within <paramref name="radius"/> of <paramref name="center"/>, across every loaded cell in range.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<ObjectReference> GetReferencesInRange(NiPoint3 center, float radius) =>
            [.. tes.GetCellsInRange(center, radius).SelectMany(cell => cell.GetReferencesInRange(center, radius))];

        /// <summary>
        /// Disables every reference within <paramref name="radius"/> of <paramref name="center"/> whose base object is vegetation,
        /// as <c>IsVegetation</c> determines,
        /// except actors and the references whose base object is in <paramref name="excludedBaseObjectIds"/>.
        /// </summary>
        /// <returns>The references disabled.</returns>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<ObjectReference> DisableVegetation(NiPoint3 center, float radius, IReadOnlyCollection<FormId<TESBoundObject>>? excludedBaseObjectIds = null)
        {
            IReadOnlyList<ObjectReference> clearedReferences =
            [
                .. tes.GetReferencesInRange(center, radius)
                    .Where(reference =>
                        reference.AsActor() == null
                        && reference.GetBaseObject() is { } baseObject
                        && baseObject.IsVegetation
                        && (excludedBaseObjectIds == null || !excludedBaseObjectIds.Contains(baseObject.FormId)))
            ];
            foreach (ObjectReference reference in clearedReferences)
            {
                reference.Disable();
            }
            return clearedReferences;
        }

        /// <summary>
        /// Disables the vegetation within <see cref="PackIn.DisabledVegetationRadius"/> of <paramref name="origin"/>, except the bases of <paramref name="packIn"/>'s parts.
        /// </summary>
        /// <returns>The references disabled.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="packIn"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<ObjectReference> DisableVegetation(PackIn packIn, NiPoint3 origin)
        {
            ArgumentNullException.ThrowIfNull(packIn);
            return packIn.DisabledVegetationRadius > 0f ? tes.DisableVegetation(origin, packIn.DisabledVegetationRadius, [.. packIn.Parts.Select(part => part.BaseObjectId)]) : [];
        }

        internal async Task LayOnGroundAfterDelayAsync(IEnumerable<GroundPlacement> placements, IUtility utility, CancellationToken cancellationToken = default)
        {
            const float settleSeconds = 1f;
            ArgumentNullException.ThrowIfNull(placements);
            ArgumentNullException.ThrowIfNull(utility);
            await utility.WaitSecondsAsync(settleSeconds, cancellationToken);
            foreach ((ObjectReference reference, NiPoint3 position, float yaw, float span) in placements)
            {
                reference.LayOnGround(tes, position, yaw, span);
            }
        }
    }

    private static bool Reaches(Cell cell, NiPoint3 center, float radius)
    {
        const float cellSpan = 4096f;
        return cell.GetCoordinates() is { } corner
            && corner.WorldX - radius < center.X && center.X < corner.WorldX + cellSpan + radius
            && corner.WorldY - radius < center.Y && center.Y < corner.WorldY + cellSpan + radius;
    }

    private static void Write(hkVector4 vector, NiPoint3 point, float scale)
    {
        vector.QuadX = point.X * scale;
        vector.QuadY = point.Y * scale;
        vector.QuadZ = point.Z * scale;
        vector.QuadW = 0f;
    }

    private static float GetSlope(float rise, float run) => (float)Math.Atan2(rise, run);

}
