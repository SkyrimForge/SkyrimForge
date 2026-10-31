namespace SkyrimForge.Game;

/// <summary>A projectile's launch angles, matching the game's Projectile::ProjectileRot: pitch about X and yaw about Z.</summary>
public readonly record struct ProjectileRot(float X, float Z);
