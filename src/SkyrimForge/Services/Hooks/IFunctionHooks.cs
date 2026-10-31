namespace SkyrimForge;

/// <summary>Hooks a game function at a virtual method table entry, a call instruction, or the function's entry.</summary>
/// <remarks>
/// To run a handler at one instruction inside a function, use an <see cref="InstructionHook"/>.
/// <para>
/// A replacement is the address of a static method marked <c>UnmanagedCallersOnly</c>, whose parameters and calling convention match the game function's.
/// A parameter of a view type addresses nothing if the game passes <c>null</c>, and <c>Exists</c> tests for that.
/// </para>
/// <para>
/// A member function takes its object as the first parameter. Mark the replacement <c>[UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]</c>,
/// declare a view type for each pointer to a game object, and declare a C++ <c>bool</c> as <c>byte</c>.
/// To call the game's function, cast the displaced address to a <c>delegate* unmanaged[Thiscall]</c> of the same parameters. The VirtualFunctionPlugin example does both.
/// </para>
/// <para>
/// A replacement runs on the calling thread, which can be any thread, so queue work that calls a member documented for the game's thread through <see cref="ITaskQueue"/>.
/// An exception that reaches game code ends the process, and the framework's fault log does not record it.
/// The process also ends if managed code reaches the hooked function through a P/Invoke marked <c>SuppressGCTransition</c>.
/// </para>
/// <para>
/// Call the displaced address at most once on any path, a catch block included.
/// Install a hook while no thread executes the code it redirects. A plugin's Start is such a moment for most functions.
/// </para>
/// <para>
/// A hook remains installed until the process exits. A second hook on one table entry, call instruction, or function entry throws,
/// as does a hook overlapping one already recorded or a patch <see cref="ICodePatcher"/> applied.
/// SkyrimForge.Runtime.log lists each hook's owner, and records each hook another module has changed by the time the game's data has loaded.
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
    /// or the entry does not address game code, as when the index lies past the table's end or the entry is already replaced.
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
    /// <remarks>An offset is valid for one game build only. A call already replaced through SKSE's trampoline is found as well.</remarks>
    /// <exception cref="System.InvalidOperationException">
    /// The address library has no identifier for the caller, or the offset does not address a call to the game's code or to an SKSE trampoline stub.
    /// </exception>
    nint FindCallSite(GameFunction caller, GameOffset offset);

    /// <summary>Replaces the target of the call instruction at <paramref name="callSite"/> with <paramref name="replacement"/> and returns the target it displaced.</summary>
    /// <remarks>
    /// A call another SKSE plugin already replaced through SKSE's trampoline can be replaced again. Calling the displaced target then runs that plugin's replacement.
    /// </remarks>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="callSite"/> or <paramref name="replacement"/> is 0.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// The address is not a call instruction in game code, the call overlaps a recorded hook, or its target is neither game code nor an SKSE trampoline stub.
    /// </exception>
    nint ReplaceCallTarget(nint callSite, nint replacement);

    /// <summary>
    /// Detours the entry of <paramref name="function"/>, so every call to it reaches <paramref name="replacement"/>,
    /// and returns the address of a trampoline, which the replacement calls to reach the game's own code.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="replacement"/> is 0.</exception>
    /// <exception cref="System.InvalidOperationException">
    /// The address library has no identifier for the function, its entry is not game code, its opening instructions cannot be moved, overlap a recorded hook,
    /// or contain the target of a recorded jump or call past their first byte, it is already detoured, or the trampoline has no room.
    /// </exception>
    nint DetourFunction(GameFunction function, nint replacement);
}
