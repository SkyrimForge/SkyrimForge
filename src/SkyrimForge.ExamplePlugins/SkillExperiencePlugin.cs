using SkyrimForge.Game;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Logs each skill experience gain the player makes, from an instruction hook inside PlayerCharacter::AddSkillExperience.</summary>
/// <remarks>
/// The hook is after the function's test that the amount is positive, so it logs only applied gains, and it leaves the function's entry free for other mods' detours.
/// At the hooked instruction, edx is the skill and xmm2 the experience.
/// </remarks>
public sealed class SkillExperiencePlugin(ICodePatcher codePatcher, ILog log) : IPlugin
{
    private static readonly GameFunction addSkillExperience = new(39413, 40488);

    // mov eax, [rsp+70h] and mov rcx, [rcx+disp32]. The displacement differs by edition.
    private static readonly BytePattern expected = new("8B 44 24 70 48 8B 89 ?? ?? ?? ??");

    /// <inheritdoc/>
    public void Start()
    {
        nint site = AddressLibrary.Resolve(addSkillExperience, new GameOffset(0xC, 0xC));
        codePatcher.Apply(new InstructionHook(site, expected, context => log.Write($"Player gained {context.Xmm2.Single} experience in {(ActorValue)(int)context.Rdx}.")));
    }
}
