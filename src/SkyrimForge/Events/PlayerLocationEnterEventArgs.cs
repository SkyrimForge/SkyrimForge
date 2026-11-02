using SkyrimForge.Game;

namespace SkyrimForge.Events;

/// <summary>Arguments for <see cref="IPlayerLocationEvents.OnEnter"/>.</summary>
public readonly record struct PlayerLocationEnterEventArgs(Location Location);
