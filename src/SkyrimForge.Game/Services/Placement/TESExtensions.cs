using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.World;

/// <summary>Provides extension members for <see cref="TES"/>.</summary>
public static class TESExtensions
{
    extension(TES tes)
    {
        /// <summary>
        /// Disables the vegetation within <see cref="PackIn.DisabledVegetationRadius"/> of <paramref name="origin"/>, except the bases of <paramref name="packIn"/>'s parts.
        /// </summary>
        /// <returns>The references disabled.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="packIn"/> is <c>null</c>.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public IReadOnlyList<ObjectReference> DisableVegetation(PackIn packIn, NiPoint3 origin)
        {
            ArgumentNullException.ThrowIfNull(packIn);
            return packIn.DisabledVegetationRadius > 0f ? tes.DisableVegetation(origin, packIn.DisabledVegetationRadius, [.. packIn.Parts.Select(part => part.BaseObjectId)]) : [];
        }
    }
}
