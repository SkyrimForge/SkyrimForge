namespace SkyrimForge.Game;

/// <summary>The low and the high end of an image-space range, matching the game's ImageSpaceModifierData::MinMax&lt;float&gt;.</summary>
public readonly record struct MinMaxFloat(MultAddFloat Min, MultAddFloat Max);
