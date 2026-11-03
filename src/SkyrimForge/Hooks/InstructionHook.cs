using System;

namespace SkyrimForge.Hooks;

/// <summary>Runs a handler on each thread that reaches the instruction at <see cref="CodePatch.Address"/>, before that instruction runs.</summary>
/// <remarks>
/// <para>
/// The thread then runs the instruction with every register as the handler left it. A handler that sets <see cref="CpuContext.Rip"/> continues there instead.
/// The handler runs on any thread that reaches the instruction. An exception escaping it is written to the plugin's log, and the process then ends.
/// </para>
/// <para>
/// The hook moves the instructions spanning its first five bytes to a trampoline, or its first fourteen when no trampoline lies within a 32-bit displacement.
/// The expected bytes must cover them. <see cref="ICodePatcher.Apply"/> throws if a moved call is followed by another moved instruction,
/// if a moved branch targets a moved byte, or if a direct branch elsewhere in the function lands inside the moved bytes past their first byte.
/// A moved call runs without unwind data, so an exception passing through it is not unwound correctly.
/// </para>
/// <para>
/// A hook remains until the process exits. Instruction hooks exist at no more than 1024 addresses, and <see cref="ICodePatcher.Apply"/> throws past that number.
/// Hooks at one address share one jump, and their handlers run newest first. A handler that sets <see cref="CpuContext.Rip"/> to another address stops the earlier ones.
/// A later hook's expected bytes are compared with the code as it was before the first hook.
/// A hook overlapping a hook or patch at another address throws, as does one where <see cref="IFunctionHooks.DetourFunction"/> detoured a function after the first hook there.
/// </para>
/// </remarks>
public sealed class InstructionHook : CodePatch
{
    /// <summary>Creates a hook running <paramref name="handler"/> before the instruction at <paramref name="address"/>.</summary>
    /// <param name="address">The address of the hooked instruction.</param>
    /// <param name="expected">The bytes the code at <paramref name="address"/> must match: at least the whole instructions spanning the first five bytes.</param>
    /// <param name="handler">The handler run before the instruction.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is 0, or <paramref name="expected"/> is shorter than five bytes.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="expected"/> or <paramref name="handler"/> is <c>null</c>.</exception>
    public InstructionHook(nint address, BytePattern expected, InstructionHandler handler) : base(address, expected)
    {
        ThrowIfShorterThanBranch();
        ArgumentNullException.ThrowIfNull(handler);
        Handler = handler;
    }

    /// <summary>The handler run before the instruction.</summary>
    public InstructionHandler Handler { get; }
}
