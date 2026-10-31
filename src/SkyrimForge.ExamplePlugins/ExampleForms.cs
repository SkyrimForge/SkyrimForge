using SkyrimForge.Game;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Forms from Skyrim.esm that the examples use as subjects, each named by its editor ID.</summary>
internal static class ExampleForms
{
    internal static readonly FormId<Weapon> IronSword = new(0x00012EB7);

    internal static readonly FormId<Weapon> HuntingBow = new(0x00013985);

    internal static readonly FormId<Spell> Flames = new(0x00012FCD);

    internal static readonly FormId<Ammo> IronArrow = new(0x0001397D);

    internal static readonly FormId<ActorBase> EncHunterNordF = new(0x000BFB48);

    internal static readonly FormId<Idle> IdleApplaud2 = new(0x000D8730);

    internal static readonly FormId<Sound> QSTDwemerGong01 = new(0x000F5382);

    internal static readonly FormId<TESBoundObject> FoodSweetroll = new(0x00064B3D);
}
