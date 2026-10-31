namespace SkyrimForge;

/// <summary>Runs on a thread that reaches an <see cref="InstructionHook"/>, before the hooked instruction, and may change the registers in <paramref name="context"/>.</summary>
public delegate void InstructionHandler(CpuContext context);
