using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Papyrus;

// Frees the argument list when the call ends. The Papyrus VM does not free it, and freeing it sooner frees memory the Papyrus VM has yet to copy.
internal sealed class PapyrusCallback
{
    private static readonly ConcurrentDictionary<int, PapyrusCallback> callbacksByToken = new();

    private static int nextToken;

    private readonly VirtualMachineArgumentList virtualMachineArgumentList;

    private readonly Action<PapyrusValue> onReturn;

    private readonly Action onDestroyedWithoutReturn;

    private readonly ILog log;

    // Assigned after construction because the bind can throw, and the callback must exist then to free the argument list.
    // Written on the dispatching thread and read on the Papyrus VM's thread.
    private volatile FormType returnObjectType;

    static unsafe PapyrusCallback()
    {
        NativePapyrus.SF_Papyrus_SetReturnOrDestructionCallback(&OnReturnOrDestruction);
    }

    internal static PapyrusCallback Create(ILog log, VirtualMachineArgumentList virtualMachineArgumentList, Action<PapyrusValue> onReturn, Action onDestroyedWithoutReturn)
    {
        int token = Interlocked.Increment(ref nextToken);
        if (IStackCallbackFunctor.OrNull(NativePapyrus.SF_Papyrus_BeginCallback(token, virtualMachineArgumentList.NativeArguments.Address)) is not { } functor)
        {
            virtualMachineArgumentList.Free();
            throw new InvalidOperationException("The Papyrus callback could not be allocated.");
        }
        var callback = new PapyrusCallback(log, virtualMachineArgumentList, onReturn, onDestroyedWithoutReturn, functor);
        callbacksByToken[token] = callback;
        return callback;
    }

    private PapyrusCallback(ILog log, VirtualMachineArgumentList virtualMachineArgumentList, Action<PapyrusValue> onReturn, Action onDestroyedWithoutReturn, IStackCallbackFunctor functor)
    {
        this.log = log;
        this.virtualMachineArgumentList = virtualMachineArgumentList;
        this.onReturn = onReturn;
        this.onDestroyedWithoutReturn = onDestroyedWithoutReturn;
        Functor = functor;
    }

    internal IStackCallbackFunctor Functor { get; }

    internal IFunctionArguments Arguments => virtualMachineArgumentList.NativeArguments;

    internal void BindArguments(string className, string functionName)
    {
        returnObjectType = virtualMachineArgumentList.Bind(className, functionName);
    }

    // A callback built but not dispatched otherwise leaks with its list, because the dispatch acquires the first reference.
    internal void DeleteCallback() => NativePapyrus.SF_Papyrus_DeleteCallback(Functor.Address);

    // Runs when the call returns and again, with no value, when the callback is destroyed. A function declaring no return value still reports a return, with None.
    [UnmanagedCallersOnly]
    private static void OnReturnOrDestruction(int token, nint returnedVariableAddress)
    {
        if (!callbacksByToken.TryRemove(token, out PapyrusCallback? callback))
        {
            return;
        }

        // No return value means the Papyrus VM rejected the call or released it without running it, or DeleteCallback released a callback that was not dispatched.
        if (Variable.OrNull(returnedVariableAddress) is not { } returnedVariable)
        {
            callback.InvokeWithTryCatch(callback.onDestroyedWithoutReturn);
            return;
        }

        callback.InvokeWithTryCatch(() => callback.onReturn(new PapyrusValue(returnedVariable, callback.returnObjectType)));
    }

    // An exception cannot propagate through the Papyrus VM's dispatch.
    private void InvokeWithTryCatch(Action work)
    {
        try
        {
            work();
        }
        catch (Exception ex)
        {
            log.Write($"A Papyrus callback threw and its value was dropped: {ex}");
        }
    }
}

internal static unsafe partial class NativePapyrus
{
    [LibraryImport(library)]
    internal static partial void SF_Papyrus_SetReturnOrDestructionCallback(delegate* unmanaged<int, nint, void> handler);

    [LibraryImport(library)]
    internal static partial nint SF_Papyrus_BeginCallback(int token, nint arguments);

    [LibraryImport(library)]
    internal static partial void SF_Papyrus_DeleteCallback(nint callback);
}
