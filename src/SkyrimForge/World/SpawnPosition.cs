using SkyrimForge.Game;

namespace SkyrimForge.World;

/// <summary>A position <see cref="IPlayerPositionHistory.FindSpawnPosition"/> finds.</summary>
public readonly record struct SpawnPosition(NiPoint3 Position, SpawnPositionSource Source);
