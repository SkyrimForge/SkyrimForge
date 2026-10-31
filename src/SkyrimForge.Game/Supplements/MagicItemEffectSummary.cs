using System.Collections.Generic;

namespace SkyrimForge.Game;

/// <summary>The values the game derives from a magic item's effects.</summary>
/// <param name="CostliestEffect">The effect costing the most, or <c>null</c> if the item has none.</param>
/// <param name="MaxCost">The cost of <paramref name="CostliestEffect"/>, or -1 if that is <c>null</c>.</param>
/// <param name="LargestAreaEffect">The effect covering the largest area, or <c>null</c> if none does.</param>
/// <param name="HighestArea">The area of <paramref name="LargestAreaEffect"/>.</param>
/// <param name="Flags">
/// The flags of the game's MagicItemDataCollector, which do not describe the effects. An ingredient has kOnlyFirstEffect, and any other item has none.
/// </param>
/// <param name="SummonsExtraLargeCreature">Whether an effect of the item summons a creature the game classes as extra large.</param>
/// <param name="ProjectileEffects">The effects the game counts as projectiles.</param>
public readonly record struct MagicItemEffectSummary(Effect? CostliestEffect, int MaxCost, Effect? LargestAreaEffect, float HighestArea, uint Flags, bool SummonsExtraLargeCreature,
    IReadOnlyList<Effect> ProjectileEffects);
