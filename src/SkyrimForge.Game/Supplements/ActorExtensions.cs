using System;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="IActor"/>.</summary>
public static class ActorExtensions
{
    extension<TActor>(TActor actor) where TActor : IActor
    {
        /// <summary>Adds <paramref name="amount"/> to the permanent modifier of <paramref name="actorValue"/>, as Papyrus's ModActorValue does.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public void ModActorValue(ActorValue actorValue, float amount) =>
            actor.AsActorValueOwner().ModActorValue(ACTOR_VALUE_MODIFIERS.ACTOR_VALUE_MODIFIER.kPermanent, actorValue, amount);

        /// <summary>The current value of <paramref name="actorValue"/>, as Papyrus's GetActorValue returns it.</summary>
        /// <remarks>A disabled actor returns its base record's value, as Papyrus does. Call on the game's thread.</remarks>
        public float GetActorValue(ActorValue actorValue) =>
            actor.IsDisabled() ? actor.GetActorBase()!.Value.GetActorValue(actorValue) : actor.AsActorValueOwner().GetActorValue(actorValue);

        /// <summary>Sets the value of <paramref name="actorValue"/>, as Papyrus's SetActorValue does.</summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="actorValue"/> is not a value the game describes, or it is an enumeration and <paramref name="value"/> is not one of its values.
        /// </exception>
        /// <remarks>Call on the game's thread.</remarks>
        public void SetActorValue(ActorValue actorValue, float value)
        {
            ActorValueInfo info = GetInfo(actorValue);
            if (info.Flags.HasFlag(ActorValueInfo.ActorValueFlag.kEnumeration) && !(value >= 0f && value == MathF.Truncate(value) && value < info.EnumValueCount))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"{actorValue} is an enumeration of {info.EnumValueCount} values, numbered from zero.");
            }
            actor.AsActorValueOwner().SetActorValue(actorValue, value);
        }

        /// <summary>
        /// Changes the permanent modifier of <paramref name="actorValue"/> so that its current value is <paramref name="value"/>, as Papyrus's ForceActorValue does.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="actorValue"/> is not a value the game describes, or the game forbids scripts to change it.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public void ForceActorValue(ActorValue actorValue, float value)
        {
            if (GetInfo(actorValue).Flags.HasFlag(ActorValueInfo.ActorValueFlag.kCannotBeAlteredByScripts))
            {
                throw new ArgumentOutOfRangeException(nameof(actorValue), actorValue, $"The game forbids scripts to change {actorValue}.");
            }
            ActorValueOwner owner = actor.AsActorValueOwner();
            owner.ModActorValue(ACTOR_VALUE_MODIFIERS.ACTOR_VALUE_MODIFIER.kPermanent, actorValue, value - owner.GetActorValue(actorValue));
        }

        /// <summary>The current value of <paramref name="actorValue"/> as a fraction of its permanent value, as Papyrus's GetActorValuePercentage returns it.</summary>
        /// <returns>The fraction, or 1 if the permanent value is zero.</returns>
        /// <remarks>Call on the game's thread.</remarks>
        public float GetActorValuePercentage(ActorValue actorValue)
        {
            ActorValueOwner owner = actor.AsActorValueOwner();
            float permanent = owner.GetPermanentActorValue(actorValue);
            return permanent == 0f ? 1f : owner.GetActorValue(actorValue) / permanent;
        }

        /// <summary>The base value of <paramref name="actorValue"/>, as Papyrus's GetBaseActorValue returns it.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public float GetBaseActorValue(ActorValue actorValue) => actor.AsActorValueOwner().GetBaseActorValue(actorValue);

        /// <summary>Restores up to <paramref name="amount"/> of the damage to <paramref name="actorValue"/>, as Papyrus's RestoreActorValue does.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="actorValue"/> is not a value the game describes, or the game forbids scripts to change it.</exception>
        /// <remarks>
        /// The sign of <paramref name="amount"/> is ignored. An actor value without damage is unchanged, except an inverted one, whose damage modifier is always lowered.
        /// Call on the game's thread.
        /// </remarks>
        public void RestoreDamage(ActorValue actorValue, float amount)
        {
            ActorValueInfo info = GetInfo(actorValue);
            if (info.Flags.HasFlag(ActorValueInfo.ActorValueFlag.kCannotBeAlteredByScripts))
            {
                throw new ArgumentOutOfRangeException(nameof(actorValue), actorValue, $"The game forbids scripts to change {actorValue}.");
            }
            const ACTOR_VALUE_MODIFIERS.ACTOR_VALUE_MODIFIER damage = ACTOR_VALUE_MODIFIERS.ACTOR_VALUE_MODIFIER.kDamage;
            bool inverted = info.Flags.HasFlag(ActorValueInfo.ActorValueFlag.kInverted);
            float signedAmount = inverted ? -MathF.Abs(amount) : MathF.Abs(amount);
            if ((signedAmount > 0f && actor.GetActorValueModifier(damage, actorValue) < 0f) || (inverted && signedAmount < 0f))
            {
                actor.AsActorValueOwner().ModActorValue(damage, actorValue, signedAmount);
            }
        }

        /// <summary>Damages <paramref name="actorValue"/> by <paramref name="amount"/>, as Papyrus's DamageActorValue does.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="actorValue"/> is not a value the game describes, or the game forbids scripts to change it.</exception>
        /// <remarks>
        /// The sign of <paramref name="amount"/> is ignored. An inverted actor value's damage modifier is raised, and any other's is lowered.
        /// Call on the game's thread.
        /// </remarks>
        public void DamageActorValue(ActorValue actorValue, float amount)
        {
            ActorValueInfo info = GetInfo(actorValue);
            if (info.Flags.HasFlag(ActorValueInfo.ActorValueFlag.kCannotBeAlteredByScripts))
            {
                throw new ArgumentOutOfRangeException(nameof(actorValue), actorValue, $"The game forbids scripts to change {actorValue}.");
            }
            float signedAmount = info.Flags.HasFlag(ActorValueInfo.ActorValueFlag.kInverted) ? MathF.Abs(amount) : -MathF.Abs(amount);
            if (actorValue == ActorValue.kHealth && actor.IsChild())
            {
                // The generated HandleHealthDamage requires an attacker, and Papyrus passes none.
                NativeBindings.SF_Actor_HandleHealthDamage(actor.Address, 0, signedAmount);
            }
            else
            {
                actor.AsActorValueOwner().ModActorValue(ACTOR_VALUE_MODIFIERS.ACTOR_VALUE_MODIFIER.kDamage, actorValue, signedAmount);
            }
        }

        /// <summary>Whether <paramref name="other"/> detects the actor, as Papyrus's IsDetectedBy returns it.</summary>
        /// <returns>False if either actor has no AI process.</returns>
        /// <remarks>Call on the game's thread.</remarks>
        public bool IsDetectedBy<TOther>(TOther other) where TOther : IActor =>
            actor.ActorRuntimeData.CurrentProcess != null && other.ActorRuntimeData.CurrentProcess != null
            && other.RequestDetectionLevel(actor, DETECTION_PRIORITY.kNormal) > 0;

        /// <summary>Gives the actor's base record <paramref name="outfit"/> as its default outfit and dresses the actor in it, as Papyrus's SetOutfit does.</summary>
        /// <remarks>
        /// An outfit the record already has changes nothing, and a disabled actor's record changes without the actor being dressed.
        /// Call on the game's thread.
        /// </remarks>
        public void SetOutfit(Outfit outfit) => actor.SetDefaultOutfit(outfit, true);

        /// <summary>Opens the actor's inventory in the container menu, as Papyrus's OpenInventory does.</summary>
        /// <param name="forceOpen">Whether the inventory opens for an actor that is not the player's teammate.</param>
        /// <remarks>Call on the game's thread.</remarks>
        public void OpenInventory(bool forceOpen = false)
        {
            if (actor.IsPlayerTeammate() || forceOpen)
            {
                ContainerMenu.OpenMenu(actor, ContainerMenu.ContainerMode.kNPCMode);
            }
        }

        /// <summary>The actor's rank in <paramref name="faction"/>, as Papyrus's GetFactionRank returns it.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public int GetFactionRank(Faction faction) => actor.GetFactionRank(faction, actor.IsPlayerRef());

        /// <summary>Whether <paramref name="effect"/> is active on the actor, as Papyrus's HasMagicEffect returns it.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public bool HasMagicEffect(MagicEffect effect) => actor.AsMagicTarget().HasMagicEffect(effect);

        /// <summary>Whether the actor has a line of sight to <paramref name="other"/>, as Papyrus's HasLOS returns it.</summary>
        /// <exception cref="ArgumentException">The actor is not the player and <paramref name="other"/> is no actor. Papyrus logs an error and returns false.</exception>
        /// <remarks>Call on the game's thread.</remarks>
        public bool HasLOS<TOther>(TOther other) where TOther : IObjectReference
        {
            if (!actor.IsPlayerRef() && other.GetFormType() != FormType.ActorCharacter)
            {
                throw new ArgumentException("Only the player has a line of sight to a reference that is no actor.", nameof(other));
            }
            bool ignored = false;
            return actor.HasLineOfSight(other, ref ignored);
        }

        /// <summary>Whether the actor's base record is the parent in a relationship with <paramref name="other"/>'s, as Papyrus's HasParentRelationship returns it.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public bool HasParentRelationship<TOther>(TOther other) where TOther : IActor
        {
            ActorBase actorBase = actor.GetActorBase()!.Value;
            return BGSRelationship.GetRelationship(actorBase, other.GetActorBase()!.Value)?.Npc1?.Address == actorBase.Address;
        }

        /// <summary>Evaluates the actor's package again and resets its AI, as Papyrus's ResetAI does.</summary>
        /// <remarks>A deleted or disabled actor is unchanged. Call on the game's thread.</remarks>
        public void ResetAI()
        {
            if (!actor.IsDeleted() && !actor.IsDisabled())
            {
                actor.EvaluatePackage(false, true);
            }
        }

        /// <summary>Raises a trespass alarm against <paramref name="criminal"/> on the actor's behalf, as Papyrus's SendTrespassAlarm does.</summary>
        /// <remarks>Call on the game's thread.</remarks>
        public void SendTrespassAlarm<TCriminal>(TCriminal criminal) where TCriminal : IActor => criminal.TrespassAlarm(actor, actor.GetBaseObject()!.Value, -1);

        /// <summary>Sets the actor's facial expression to <paramref name="mood"/> at <paramref name="strength"/>, as Papyrus's SetExpressionOverride does.</summary>
        /// <param name="mood">The mood, from 1 to 16.</param>
        /// <param name="strength">The strength in percent, from 0 to 100. A greater or negative value reads as 100, as it does in Papyrus.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mood"/> is outside 1 to 16.</exception>
        /// <remarks>An actor without face animation data is unchanged. Call on the game's thread.</remarks>
        public void SetExpressionOverride(int mood, int strength = 100)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(mood, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(mood, 16);
            if (actor.GetFaceGenAnimationData() is { } faceData)
            {
                faceData.ExprOverride = false;
                faceData.SetExpressionOverride((uint)mood, Math.Min((uint)strength, 100u) * 0.01f);
                faceData.ExprOverride = true;
            }
        }

        /// <summary>Launches <paramref name="spell"/> from the actor's aim.</summary>
        /// <returns>The projectile, or <c>null</c> if the game created none.</returns>
        /// <remarks>Call on the game's thread.</remarks>
        public Projectile? LaunchSpellFromAim<TSpell>(TSpell spell) where TSpell : ISpell
        {
            const float originHeight = 96f;
            NiPoint3 position = actor.GetPosition();
            var angles = new ProjectileRot(actor.GetAimAngle(), actor.GetAimHeading());
            return Projectile.LaunchSpell(actor, spell, position with { Z = position.Z + originHeight }, angles);
        }
    }

    private static ActorValueInfo GetInfo(ActorValue actorValue) =>
        ActorValueList.GetActorValueInfo(actorValue) ?? throw new ArgumentOutOfRangeException(nameof(actorValue), actorValue, $"The game describes no actor value {actorValue}.");
}
