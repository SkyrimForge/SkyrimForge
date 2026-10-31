using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

/// <summary>A group of objects placed at offsets from one point, each laid on the ground, as a pack-in of the Creation Kit is.</summary>
/// <exception cref="ArgumentNullException"><paramref name="parts"/> is <c>null</c>.</exception>
public sealed class PackIn(IEnumerable<PackInPart> parts)
{
    /// <summary>The objects, placed in this order.</summary>
    public IReadOnlyList<PackInPart> Parts { get; } = [.. parts ?? throw new ArgumentNullException(nameof(parts))];

    /// <summary>The distance above each object's position at which the ground ray starts. 300 units by default.</summary>
    public float RayStartHeight { get; init; } = 300f;

    /// <summary>The distance below each object's position at which the ground ray ends. 600 units by default.</summary>
    public float RayEndDepth { get; init; } = 600f;

    /// <summary>The radius around the origin in which vegetation is disabled before the parts are placed. Zero disables none.</summary>
    /// <remarks>An object whose base is a part's is not disabled. The save records the objects as disabled.</remarks>
    public float DisabledVegetationRadius { get; init; }

    /// <summary>
    /// Returns whether each part with a <see cref="PackInPart.MaximumTilt"/> lies on ground within that limit when placed at <paramref name="origin"/>,
    /// a point on the ground.
    /// </summary>
    /// <remarks>The ground is measured only if its cell is loaded, and a part with a tilt limit over unmeasured ground does not fit. Call on the game's thread.</remarks>
    public bool FitsAt(TES tes, NiPoint3 origin)
    {
        return Parts.All(part => IsGroundLevelEnough(tes, GetGroundPosition(tes, origin, part), part));
    }

    /// <summary>
    /// Returns the nearest tested ground point within <paramref name="searchRadius"/> of <paramref name="position"/> at which <see cref="FitsAt"/> returns true,
    /// or null if none fits.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchRadius"/> is negative.</exception>
    /// <remarks>The ground is the highest collision surface, which can be a roof, a bridge, or an actor. Call on the game's thread.</remarks>
    public NiPoint3? FindSite(TES tes, NiPoint3 position, float searchRadius)
    {
        const float pointSpacing = 200f;
        ArgumentOutOfRangeException.ThrowIfNegative(searchRadius);
        for (float radius = 0f; radius <= searchRadius; radius += pointSpacing)
        {
            int pointCount = radius == 0f ? 1 : Math.Max(8, (int)MathF.Ceiling(2f * MathF.PI * radius / pointSpacing));
            foreach (int i in Enumerable.Range(0, pointCount))
            {
                float angle = i * 2f * MathF.PI / pointCount;
                NiPoint3 candidate = position with { X = position.X + radius * MathF.Cos(angle), Y = position.Y + radius * MathF.Sin(angle) };
                if (tes.GetPositionOnGround(candidate, RayStartHeight + radius, RayEndDepth + radius) is { } ground && FitsAt(tes, ground))
                {
                    return ground;
                }
            }
        }
        return null;
    }

    internal NiPoint3 GetGroundPosition(TES tes, NiPoint3 origin, PackInPart part)
    {
        NiPoint3 offset = new(origin.X + part.EastOffset, origin.Y + part.NorthOffset, origin.Z);
        return tes.GetPositionOnGround(offset, RayStartHeight, RayEndDepth) ?? offset;
    }

    internal bool IsGroundLevelEnough(TES tes, NiPoint3 groundPosition, PackInPart part) =>
        part.MaximumTilt is not { } maximumTilt || tes.GetSurfaceTilt(groundPosition, part.Span, RayStartHeight, RayEndDepth) <= maximumTilt;
}
