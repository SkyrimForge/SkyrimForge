using System;

namespace SkyrimForge;

/// <summary>Changes the machine code of the game, or of a native SKSE plugin, if it matches the bytes each patch expects.</summary>
/// <remarks>
/// <see cref="IFunctionHooks"/> redirects whole functions. In SkyrimForge.Game, <c>AddressLibrary.Resolve</c> and <c>GameModule.Find</c> return addresses in the game's code.
/// <para>
/// The code of Windows's modules, the .NET runtime, managed assemblies, and SkyrimForge.Runtime.dll is not changed.
/// <see cref="Apply"/> does not detect a branch into a patch through a jump table or a register.
/// </para>
/// <para>
/// A patch remains until the process exits. A patch overlapping a patch or hook recorded by this service or by <see cref="IFunctionHooks"/> throws,
/// and so does a jump or a call whose target lies inside one past its first byte.
/// SkyrimForge.Runtime.log lists each patch's owner.
/// </para>
/// <para>
/// Apply a patch while no thread executes the bytes it overwrites or is inside a call made from them. A plugin's Start is such a moment for most code.
/// </para>
/// <para>
/// Most SKSE plugins write code without comparing it first, so a plugin later in SKSE's order can overwrite a patch without failing.
/// To compare with the code those plugins write while loading, apply a patch from <see cref="IGameEvents.OnDataLoaded"/>.
/// SkyrimForge.Runtime.log records each patch and hook another module has changed by the time the game's data has loaded.
/// </para>
/// </remarks>
public interface ICodePatcher
{
    /// <summary>Applies every patch, or none if one cannot be applied.</summary>
    /// <exception cref="ArgumentNullException">A patch is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// The expected bytes of two patches overlap.
    /// The target of a <see cref="JumpPatch"/> or a <see cref="CallPatch"/> lies inside the expected bytes of a patch past their first byte.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A patch's expected bytes lie outside one executable section of a loaded module, lie in an excluded module, do not begin an instruction, do not match the code,
    /// overlap a recorded patch or hook, or contain the target of a recorded jump or call past their first byte. The code's memory protection could not be changed.
    /// A patch's expected bytes or its target break one of the rules in the remarks on its type, no memory within reach could be allocated,
    /// or an instruction hook could not be written. The message states the reason, and states any earlier instruction hook of the group that could not be removed.
    /// </exception>
    void Apply(params ReadOnlySpan<CodePatch> patches);

    /// <summary>
    /// Allocates <paramref name="length"/> bytes of executable memory, all within a 32-bit displacement of <paramref name="near"/>,
    /// for machine code a <see cref="JumpPatch"/> or a <see cref="CallPatch"/> reaches, and returns the address of the first byte.
    /// </summary>
    /// <remarks>
    /// The memory is never released. Code placed in it has no unwind data, so an exception or a stack walk passing through a frame there is not unwound correctly.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="near"/> is 0, or <paramref name="length"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">No free memory lies near enough to <paramref name="near"/>.</exception>
    nint AllocateCode(nint near, int length);
}
