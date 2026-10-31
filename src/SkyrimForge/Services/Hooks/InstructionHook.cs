using System;

namespace SkyrimForge;

/// <summary>Runs a handler on each thread that reaches the instruction at <see cref="CodePatch.Address"/>, before that instruction runs.</summary>
/// <remarks>
/// The thread then runs the hooked instruction and continues, with every register as the handler left it.
/// The handler skips the hooked instruction, or continues anywhere else, by setting <see cref="CpuContext.Rip"/>. To run a handler after an instruction, hook the one following it.
/// <para>
/// The hook moves the whole instructions it overwrites to a trampoline, which runs them after the handler.
/// The expected bytes cover every moved byte: the instructions spanning the first five bytes,
/// or the first fourteen when no trampoline lies within a 32-bit displacement of the address.
/// <see cref="ICodePatcher.Apply"/> throws if a moved instruction is a call that another moved instruction follows, if a moved branch targets a moved byte,
/// or if a direct branch elsewhere in the function lands inside the moved bytes past their first byte.
/// A moved call runs from the trampoline, which has no unwind data, so an exception or a stack walk passing through that call is not unwound correctly.
/// </para>
/// <para>
/// The handler runs on the thread that reached the instruction, which can be any thread. An exception escaping it is written to the plugin's log, and the process then ends.
/// The process also ends if managed code reaches the instruction through a P/Invoke marked <c>SuppressGCTransition</c>.
/// A hook remains until the process exits. At most 1024 instruction hooks exist in a process, and <see cref="ICodePatcher.Apply"/> throws for a hook past that number.
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
