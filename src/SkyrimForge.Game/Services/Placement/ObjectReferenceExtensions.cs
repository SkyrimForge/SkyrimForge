using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.World;

/// <summary>Provides extension members for <see cref="IObjectReference"/>.</summary>
public static class ObjectReferenceExtensions
{
    extension<TReference>(TReference reference) where TReference : IObjectReference
    {
        /// <summary>
        /// Disables the vegetation within <see cref="PackIn.DisabledVegetationRadius"/> of <paramref name="origin"/>,
        /// then lays each part of <paramref name="packIn"/> on the ground at its offset from <paramref name="origin"/>.
        /// </summary>
        /// <param name="tes">The game's TES, through which the ground and the vegetation are read.</param>
        /// <param name="dataHandler">The game's data handler.</param>
        /// <param name="utility">The service through which the task waits one second.</param>
        /// <param name="packIn">The parts to place.</param>
        /// <param name="origin">The position the parts' offsets are measured from, in this reference's worldspace or, in an interior, its cell.</param>
        /// <param name="cancellationToken">
        /// Cancels the one-second wait. The parts remain placed, and the task ends canceled without returning them. A new game or a save load also cancels the wait.
        /// </param>
        /// <returns>
        /// The reference placed for each part, which excludes a part that <see cref="PackInPart.MaximumTilt"/> leaves out.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="utility"/> or <paramref name="packIn"/> is <c>null</c>.</exception>
        /// <remarks>
        /// A part whose ground is not loaded is placed at the origin's height. The task waits a second for the parts' 3D to load, lays them on the ground again, and completes.
        /// <see cref="PackIn.FitsAt"/> tests whether every part is placed at an origin, and <see cref="PackIn.FindSite"/> searches for such an origin.
        /// Call on the game's thread.
        /// </remarks>
        public Task<IReadOnlyDictionary<PackInPart, ObjectReference>> PlacePackInAsync(TES tes, TESDataHandler dataHandler, IUtility utility, PackIn packIn, NiPoint3 origin,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utility);
            ArgumentNullException.ThrowIfNull(packIn);
            return Core();

            async Task<IReadOnlyDictionary<PackInPart, ObjectReference>> Core()
            {
                cancellationToken.ThrowIfCancellationRequested();
                tes.DisableVegetation(packIn, origin);
                Dictionary<PackInPart, ObjectReference> placedParts = [];
                List<GroundPlacement> laidParts = [];
                foreach (PackInPart part in packIn.Parts)
                {
                    NiPoint3 groundPosition = packIn.GetGroundPosition(tes, origin, part);
                    if (!packIn.IsGroundLevelEnough(tes, groundPosition, part))
                    {
                        continue;
                    }
                    float yaw = part.Yaw ?? Random.Shared.NextSingle() * 360f;
                    ObjectReference partReference = reference.PlaceOnGround(tes, dataHandler, part.BaseObjectId, groundPosition, yaw, part.Span);
                    placedParts.Add(part, partReference);
                    laidParts.Add(new(partReference, groundPosition, yaw, part.Span));
                }
                await tes.LayOnGroundAfterDelayAsync(laidParts, utility, cancellationToken);
                return placedParts;
            }
        }
    }
}
