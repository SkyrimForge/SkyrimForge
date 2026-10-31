namespace SkyrimForge.Game;

/// <summary>The point a ray strikes and the normal of the face it strikes there.</summary>
/// <param name="Position">The point the ray strikes.</param>
/// <param name="Normal">The normal of the face at that point.</param>
public readonly record struct SurfaceHit(NiPoint3 Position, NiPoint3 Normal);
