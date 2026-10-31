namespace SkyrimForge;

/// <summary>Handles a game event. The game waits for the handler to return.</summary>
/// <remarks>A thrown exception is logged, and the handler's subscription is removed.</remarks>
public delegate void GameEventHandler<TArgs>(TArgs args) where TArgs : struct;
