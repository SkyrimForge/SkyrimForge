using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="IMagicItem"/>.</summary>
public static class MagicItemExtensions
{
    extension<TMagicItem>(TMagicItem magicItem) where TMagicItem : IMagicItem
    {
        /// <summary>Reads the values the game derives from this item's effects, as CommonLib's <c>MagicItem::CollectData</c> collects them.</summary>
        public MagicItemEffectSummary CollectData()
        {
            nint costliestEffect = 0;
            int maxCost = 0;
            nint largestAreaEffect = 0;
            float highestArea = 0f;
            uint flags = 0;
            bool summonsExtraLargeCreature = false;
            IReadOnlyList<Effect> projectileEffects = Sequences.Read((ref nint first, int capacity) =>
                NativeSupplements.SF_MagicItem_CollectData(magicItem.Address, out costliestEffect, out maxCost, out largestAreaEffect, out highestArea, out flags,
                    out summonsExtraLargeCreature, ref first, capacity),
                Effect.OrNull);
            return new MagicItemEffectSummary(Effect.OrNull(costliestEffect), maxCost, Effect.OrNull(largestAreaEffect), highestArea, flags, summonsExtraLargeCreature,
                projectileEffects);
        }
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial int SF_MagicItem_CollectData(nint self, out nint costliestEffect, out int maxCost, out nint largestAreaEffect, out float highestArea, out uint flags,
        [MarshalAs(UnmanagedType.U1)] out bool summonsExtraLargeCreature, ref nint buffer, int capacity);
}
