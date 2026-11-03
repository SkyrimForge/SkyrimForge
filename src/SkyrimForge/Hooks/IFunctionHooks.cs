namespace SkyrimForge.Hooks;

/// <summary>Hooks a game function at a virtual method table entry, a call instruction, or the function's entry.</summary>
/// <remarks>
/// <para>
/// A replacement is the address of a static method marked <c>UnmanagedCallersOnly</c> whose parameters match the game function's.
/// For a member function, mark it <c>[UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]</c>, take the object as the first parameter,
/// and call the displaced address as a <c>delegate* unmanaged[Thiscall]</c>. Declare a pointer to a game object as a view type, and a C++ <c>bool</c> as <c>byte</c>.
/// </para>
/// <para>
/// A replacement runs on the calling thread, which can be any thread, so queue work for the game's thread through <see cref="ITaskQueue"/>.
/// An exception that reaches game code ends the process. Install a hook while no thread runs the code it redirects, as during a plugin's Start for most functions.
/// </para>
/// <para>
/// A hook remains until the process exits. A hook is chained to an earlier hook at the same table entry, call, or function entry, to a call patch at a call,
/// and to a jump patch or an instruction hook at a function's entry: calling the displaced address runs the earlier one, so the hook installed last runs first.
/// Any other overlap with a recorded hook or patch throws.
/// SkyrimForge.Runtime.log records each hook and its owner. Once the game's data has loaded, it also records each hook another module has overwritten.
/// </para>
/// </remarks>
public interface IFunctionHooks
{
    /// <summary>Replaces one entry of a game type's virtual method table and returns the entry it displaced.</summary>
    /// <param name="typeName">The game type's CommonLib name, which a view's summary states after <c>RE::</c>: "TESNPC" for <c>ActorBase</c>.</param>
    /// <param name="index">The entry's index in the game's table.</param>
    /// <param name="replacement">The replacement's address, subject to the remarks on <see cref="IFunctionHooks"/>.</param>
    /// <remarks>
    /// Only calls through this type's table, on objects of exactly this type, reach the replacement.
    /// An index of the wrong entry replaces that entry without an error.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="typeName"/> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than 0x1000, or <paramref name="replacement"/> is 0.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// No table of the name has an address in the running game version,
    /// or the entry neither addresses code in a loaded module nor was replaced by an earlier hook, as when the index lies past the table's end.
    /// </exception>
    nint ReplaceVirtualTableEntry(string typeName, int index, nint replacement);

    /// <summary>
    /// Returns the address of a call from <paramref name="caller"/> to <paramref name="callee"/> in the first <paramref name="searchLength"/> bytes of the caller,
    /// where <paramref name="occurrence"/> is the zero-based index among those calls.
    /// </summary>
    /// <remarks>A call whose target is already replaced is not found. <see cref="FindCallSite(GameFunction, GameOffset)"/> finds it.</remarks>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <paramref name="occurrence"/> is negative, or <paramref name="searchLength"/> is less than 1 or greater than 0x2000.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The address library has no identifier for the caller or for the callee, or the searched bytes contain no such call.
    /// </exception>
    nint FindCallSite(GameFunction caller, GameFunction callee, int occurrence = 0, int searchLength = 0x200);

    /// <summary>Returns the address of the call instruction <paramref name="offset"/> bytes from the entry of <paramref name="caller"/>.</summary>
    /// <remarks>An offset is valid for one game build only. A call already replaced, through this service or through SKSE's trampoline, is found as well.</remarks>
    /// <exception cref="System.InvalidOperationException">
    /// The address library has no identifier for the caller,
    /// or the offset does not address a call to the game's code or to an absolute jump stub, as SKSE's trampoline and this service write.
    /// </exception>
    nint FindCallSite(GameFunction caller, GameOffset offset);

    /// <summary>Replaces the target of the call instruction at <paramref name="callSite"/> with <paramref name="replacement"/> and returns the target it displaced.</summary>
    /// <remarks>
    /// A call an earlier hook replaced, through this service, a call patch, or SKSE's trampoline in another SKSE plugin, can be replaced again.
    /// Calling the displaced target then runs that hook's replacement.
    /// </remarks>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="callSite"/> or <paramref name="replacement"/> is 0.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// The address is not a call instruction in the executable code of a module SkyrimForge patches, the call overlaps a recorded hook or patch at another address,
    /// no earlier hook replaced the call and its target is neither code in the same section nor an absolute jump stub, or no memory within reach of the call could be allocated.
    /// </exception>
    nint ReplaceCallTarget(nint callSite, nint replacement);

    /// <summary>
    /// Detours the entry of <paramref name="function"/>, so every call to it reaches <paramref name="replacement"/>,
    /// and returns the address of a trampoline, which the replacement calls to reach the game's own code.
    /// </summary>
    /// <remarks>
    /// The instructions the jump overwrites are moved to the trampoline.
    /// A jump an earlier hook wrote at the entry is moved the same way, so the trampoline then jumps to that hook.
    /// </remarks>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="replacement"/> is 0.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// The address library has no identifier for the function, its entry is not in the executable code of a module SkyrimForge patches,
    /// its opening instructions overlap a recorded hook or patch other than a jump at the entry or contain the target of a recorded jump or call past their first byte,
    /// or the detour could not be written, for a reason the message states.
    /// </exception>
    nint DetourFunction(GameFunction function, nint replacement);
}
