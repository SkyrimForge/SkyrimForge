namespace SkyrimForge.Game;

/// <summary>A four-float color with alpha, matching the game's NiColorA. Each channel runs 0 to 1.</summary>
public readonly record struct NiColorA(float Red, float Green, float Blue, float Alpha);
