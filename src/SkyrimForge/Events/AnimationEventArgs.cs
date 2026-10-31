using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>One annotation on an actor's animation, matching the game's BSAnimationGraphEvent.</summary>
/// <param name="Tag">The annotation's name: HitFrame, weaponSwing, attackStart, FootLeft.</param>
/// <param name="Payload">The annotation's argument, empty if it declares none.</param>
/// <param name="Actor">The actor playing the animation.</param>
public readonly record struct AnimationEventArgs(string Tag, string Payload, Actor Actor);
