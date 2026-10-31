namespace SkyrimForge.Game;

/// <summary>One object of a <see cref="PackIn"/>.</summary>
public sealed class PackInPart(FormId<TESBoundObject> baseObjectId, float eastOffset, float northOffset, float span)
{
    /// <summary>The object's base.</summary>
    public FormId<TESBoundObject> BaseObjectId { get; } = baseObjectId;

    /// <summary>The object's offset east of the origin, in units.</summary>
    public float EastOffset { get; } = eastOffset;

    /// <summary>The object's offset north of the origin, in units.</summary>
    public float NorthOffset { get; } = northOffset;

    /// <summary>The width across which the object is tilted to the terrain.</summary>
    public float Span { get; } = span;

    /// <summary>The object's heading in degrees, 0 by default. Null selects a random heading at each placement.</summary>
    public float? Yaw { get; init; } = 0f;

    /// <summary>The steepest ground tilt, in degrees across <see cref="Span"/>, on which the object is placed. Null places it on any ground.</summary>
    /// <remarks>
    /// The tilt is measured on the collision surface, which can be a rock, a bridge, or a floor over the terrain.
    /// The object is not placed if the tilt cannot be measured.
    /// </remarks>
    public float? MaximumTilt { get; init; }
}
