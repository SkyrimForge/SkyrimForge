namespace SkyrimForge.Game;

/// <summary>A multiplier and an addend an image-space modifier applies to one value, matching the game's ImageSpaceModifierData::MultAdd&lt;float&gt;.</summary>
public readonly record struct MultAddFloat(float Mult, float Add);
