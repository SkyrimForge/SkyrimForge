namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="ITESActorBase"/>.</summary>
public static class TESActorBaseExtensions
{
    extension<TActorBase>(TActorBase actorBase) where TActorBase : ITESActorBase
    {
        /// <summary>Makes the actor base essential or not, as Papyrus's SetEssential does.</summary>
        /// <param name="essential">Whether the actor base is essential. True also clears the protected flag.</param>
        /// <remarks>Call on the game's thread.</remarks>
        public void SetEssential(bool essential = true)
        {
            TESActorBaseData data = actorBase.AsTESActorBaseData();
            if (essential)
            {
                data.SetFlagBit(ACTOR_BASE_DATA.Flag.kProtected, false, true);
            }
            data.SetFlagBit(ACTOR_BASE_DATA.Flag.kEssential, essential, true);
        }

        /// <summary>Makes the actor base protected or not, as Papyrus's SetProtected does.</summary>
        /// <param name="isProtected">Whether the actor base is protected. True also clears the essential flag.</param>
        /// <remarks>Call on the game's thread.</remarks>
        public void SetProtected(bool isProtected = true)
        {
            TESActorBaseData data = actorBase.AsTESActorBaseData();
            if (isProtected)
            {
                data.SetFlagBit(ACTOR_BASE_DATA.Flag.kEssential, false, true);
            }
            data.SetFlagBit(ACTOR_BASE_DATA.Flag.kProtected, isProtected, true);
        }

        /// <summary>Makes the actor base invulnerable or not, as Papyrus's SetInvulnerable does.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public void SetInvulnerable(bool invulnerable = true) => actorBase.AsTESActorBaseData().SetFlagBit(ACTOR_BASE_DATA.Flag.kInvulnerable, invulnerable, true);
    }
}
