using System;

namespace SkyrimForge.Hooks;

/// <summary>Changes the machine code of the game, or of a native SKSE plugin, if it matches the bytes each patch expects.</summary>
/// <remarks>
/// A patch remains until the process exits.
/// Apply a patch while no thread runs the bytes it overwrites or is inside a call made from them, as during a plugin's Start for most code.
/// A patch overlapping a recorded patch or hook throws. An <see cref="InstructionHook"/> at the address of an earlier instruction hook shares that hook's jump instead.
/// A branch into a patch through a jump table or a register is not detected.
/// SkyrimForge.Runtime.log records each patch and its owner. Once the game's data has loaded, it also records each patch another module has overwritten.
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
    /// A patch's expected bytes lie outside one executable section of a loaded module,
    /// lie in a module of Windows or the .NET runtime, in a managed assembly, or in SkyrimForge.Runtime.dll,
    /// do not begin an instruction, do not match the code, overlap a recorded patch or hook, or contain the target of a recorded jump or call past their first byte.
    /// The code's memory protection could not be changed.
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
