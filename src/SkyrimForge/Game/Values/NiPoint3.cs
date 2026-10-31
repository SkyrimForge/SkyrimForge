using System;

namespace SkyrimForge.Game;

/// <summary>A three-float position or vector, matching the game's NiPoint3.</summary>
public readonly record struct NiPoint3(float X, float Y, float Z) : IFormattable
{
    // C's FLT_EPSILON, as in CommonLib's Unitize. float.Epsilon is a different, far smaller value.
    private const float unitizeEpsilon = 1.1920929E-07f;

    /// <summary>The vector with every component zero.</summary>
    public static NiPoint3 Zero => default;

    /// <summary>The length of the vector.</summary>
    public float Length => MathF.Sqrt(SquaredLength);

    /// <summary>The square of the length of the vector.</summary>
    public float SquaredLength => (X * X) + (Y * Y) + (Z * Z);

    /// <summary>Adds each component of <paramref name="right"/> to the same component of <paramref name="left"/>.</summary>
    public static NiPoint3 operator +(NiPoint3 left, NiPoint3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>Subtracts each component of <paramref name="right"/> from the same component of <paramref name="left"/>.</summary>
    public static NiPoint3 operator -(NiPoint3 left, NiPoint3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>Multiplies each component of <paramref name="left"/> by the same component of <paramref name="right"/>.</summary>
    public static NiPoint3 operator *(NiPoint3 left, NiPoint3 right) => new(left.X * right.X, left.Y * right.Y, left.Z * right.Z);

    /// <summary>Divides each component of <paramref name="left"/> by the same component of <paramref name="right"/>.</summary>
    public static NiPoint3 operator /(NiPoint3 left, NiPoint3 right) => new(left.X / right.X, left.Y / right.Y, left.Z / right.Z);

    /// <summary>Adds <paramref name="scalar"/> to each component of <paramref name="point"/>.</summary>
    public static NiPoint3 operator +(NiPoint3 point, float scalar) => new(point.X + scalar, point.Y + scalar, point.Z + scalar);

    /// <summary>Subtracts <paramref name="scalar"/> from each component of <paramref name="point"/>.</summary>
    public static NiPoint3 operator -(NiPoint3 point, float scalar) => new(point.X - scalar, point.Y - scalar, point.Z - scalar);

    /// <summary>Multiplies each component of <paramref name="point"/> by <paramref name="scalar"/>.</summary>
    public static NiPoint3 operator *(NiPoint3 point, float scalar) => new(point.X * scalar, point.Y * scalar, point.Z * scalar);

    /// <summary>Multiplies each component of <paramref name="point"/> by the reciprocal of <paramref name="scalar"/>.</summary>
    public static NiPoint3 operator /(NiPoint3 point, float scalar) => point * (1f / scalar);

    /// <summary>Adds each component of <paramref name="point"/> to <paramref name="scalar"/>.</summary>
    public static NiPoint3 operator +(float scalar, NiPoint3 point) => new(scalar + point.X, scalar + point.Y, scalar + point.Z);

    /// <summary>Subtracts each component of <paramref name="point"/> from <paramref name="scalar"/>.</summary>
    public static NiPoint3 operator -(float scalar, NiPoint3 point) => new(scalar - point.X, scalar - point.Y, scalar - point.Z);

    /// <summary>Multiplies <paramref name="scalar"/> by each component of <paramref name="point"/>.</summary>
    public static NiPoint3 operator *(float scalar, NiPoint3 point) => new(scalar * point.X, scalar * point.Y, scalar * point.Z);

    /// <summary>Divides <paramref name="scalar"/> by each component of <paramref name="point"/>.</summary>
    public static NiPoint3 operator /(float scalar, NiPoint3 point) => new(scalar / point.X, scalar / point.Y, scalar / point.Z);

    /// <summary>Negates each component of <paramref name="point"/>.</summary>
    public static NiPoint3 operator -(NiPoint3 point) => new(-point.X, -point.Y, -point.Z);

    /// <summary>Returns the cross product of this vector and <paramref name="other"/>.</summary>
    public NiPoint3 Cross(NiPoint3 other) => new((Y * other.Z) - (Z * other.Y), (Z * other.X) - (X * other.Z), (X * other.Y) - (Y * other.X));

    /// <summary>Returns the dot product of this vector and <paramref name="other"/>.</summary>
    public float Dot(NiPoint3 other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);

    /// <summary>Returns the cross product of this vector and <paramref name="other"/>, scaled to a length of one.</summary>
    /// <inheritdoc cref="Unitize" path="/remarks"/>
    public NiPoint3 UnitCross(NiPoint3 other) => Cross(other).Unitize();

    /// <summary>Returns this vector scaled to a length of one.</summary>
    /// <remarks>A vector whose length is at most C's FLT_EPSILON becomes the zero vector.</remarks>
    public NiPoint3 Unitize()
    {
        float length = Length;
        if (length == 1f)
        {
            return this;
        }
        return length > unitizeEpsilon ? this / length : Zero;
    }

    /// <summary>Gets the square of the distance to <paramref name="other"/>.</summary>
    public float GetSquaredDistance(NiPoint3 other) => (other - this).SquaredLength;

    /// <summary>Gets the distance to <paramref name="other"/>, in game units.</summary>
    public float GetDistance(NiPoint3 other)
    {
        float dx = other.X - X, dy = other.Y - Y, dz = other.Z - Z;
        return MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    /// <summary>Gets the distance to <paramref name="other"/> across the X and Y axes, in game units.</summary>
    public float GetHorizontalDistance(NiPoint3 other)
    {
        float dx = other.X - X, dy = other.Y - Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>
    /// Gets the distance across the X and Y axes to the nearest point of the segment from <paramref name="start"/> to <paramref name="end"/>, in game units.
    /// </summary>
    public float GetHorizontalDistanceToSegment(NiPoint3 start, NiPoint3 end)
    {
        float dx = end.X - start.X, dy = end.Y - start.Y;
        float squaredLength = (dx * dx) + (dy * dy);
        float fraction = squaredLength == 0f ? 0f : Math.Clamp((((X - start.X) * dx) + ((Y - start.Y) * dy)) / squaredLength, 0f, 1f);
        return GetHorizontalDistance(new NiPoint3(start.X + (fraction * dx), start.Y + (fraction * dy), 0f));
    }

    /// <summary>
    /// Gets the compass direction nearest the bearing from this point to <paramref name="other"/>, with +Y as north and +X as east.
    /// A point directly above or below this one is north.
    /// </summary>
    public CompassDirection GetCompassDirection(NiPoint3 other)
    {
        double degrees = ((Math.Atan2(other.X - X, other.Y - Y) * 180d / Math.PI) + 360d) % 360d;
        return (CompassDirection)((int)Math.Round(degrees / 45d) % 8);
    }

    /// <summary>Returns the components in the current culture's format: <c>NiPoint3 { X = 1.5, Y = 2, Z = 3 }</c>.</summary>
    public override string ToString() => ToString(null, null);

    /// <summary>Returns the components, each formatted with <paramref name="format"/>: <c>NiPoint3 { X = 1.5, Y = 2.0, Z = 3.0 }</c> for <c>F1</c>.</summary>
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        $"NiPoint3 {{ X = {X.ToString(format, formatProvider)}, Y = {Y.ToString(format, formatProvider)}, Z = {Z.ToString(format, formatProvider)} }}";
}
