using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using SkyrimForge.Game.BSScript.Internal;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SkyrimForge.Papyrus;

internal sealed class PapyrusDispatcher(GameTarget gameTarget, ITaskQueue taskQueue, ILog log)
{
    private readonly GameTarget gameTarget = gameTarget;
    private readonly ITaskQueue taskQueue = taskQueue;
    private readonly ILog log = log;
    private static PapyrusDispatcher? instance;

    // Static because the generated Papyrus API is extension members, which have no constructor for injection.
    internal static PapyrusDispatcher Instance => instance ?? throw new InvalidOperationException("The Papyrus dispatcher is not installed. It is installed as the framework starts.");

    // Runs before any plugin loads, so the write needs no barrier.
    internal static void Install(PapyrusDispatcher dispatcher) => instance = dispatcher;

    internal Task<T?> DispatchStaticCallAsync<T>(string className, string functionName, PapyrusArguments? arguments = null)
    {
        return DispatchAsync(className, functionName, arguments, value => value.As<T>(), (virtualMachine, callback) => virtualMachine.DispatchStaticCall(className, functionName, callback));
    }

    internal Task DispatchStaticCallAsync(string className, string functionName, PapyrusArguments? arguments = null)
    {
        return DispatchAsync(className, functionName, arguments, _ => true, (virtualMachine, callback) => virtualMachine.DispatchStaticCall(className, functionName, callback));
    }

    internal Task<T?> DispatchFunctionCallAsync<T>(IForm self, string className, string functionName, PapyrusArguments? arguments = null)
    {
        return DispatchAsync(className, functionName, arguments, value => value.As<T>(), (virtualMachine, callback) => virtualMachine.DispatchFunctionCall(self, className, functionName, callback));
    }

    internal Task<T?> DispatchPropertyGetAsync<T>(IForm receiver, string className, string propertyName)
    {
        return DispatchAsync(className, propertyName, null, value => value.As<T>(), (_, callback) => GetPropertyIntoCallback(receiver, className, propertyName, callback));
    }

    // Returns no task because the Papyrus VM releases the callback before the setter runs.
    internal void SetProperty(IForm receiver, string className, string propertyName, Action<Variable> setPropertyValue)
    {
        VirtualMachineArgumentList virtualMachineArgumentList = VirtualMachineArgumentList.Allocate();
        Variable value = virtualMachineArgumentList.Append();

        // Built before the value is written, so the callback can free the argument list if the write throws.
        PapyrusCallback callback = PapyrusCallback.Create(log, virtualMachineArgumentList, _ => { }, () => { });
        try
        {
            setPropertyValue(value);
        }
        catch (Exception)
        {
            callback.DeleteCallback();
            throw;
        }

        // Outside the try. This call releases the callback if the Papyrus virtual machine returns false, so a catch around it would free the list twice.
        SetPropertyFromCallback(receiver, className, propertyName, value, callback);
    }

    internal Task DispatchFunctionCallAsync(IForm self, string className, string functionName, PapyrusArguments? arguments = null)
    {
        return DispatchAsync(className, functionName, arguments, _ => true, (virtualMachine, callback) => virtualMachine.DispatchFunctionCall(self, className, functionName, callback));
    }

    private async Task<T> DispatchAsync<T>(string className, string functionName, PapyrusArguments? arguments, Func<PapyrusValue, T> convertReturnValue, Action<VirtualMachine, PapyrusCallback> dispatch)
    {
        var taskCompletionSource = new GameTaskCompletionSource<T>(taskQueue);

        // Reading the Papyrus VM throws before the game builds it. IPlugin.Start can run that early.
        VirtualMachine virtualMachine;
        try
        {
            virtualMachine = gameTarget.PapyrusVirtualMachine;
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"No Papyrus virtual machine existed, so {className}.{functionName} was not dispatched.", ex);
        }

        // Built here, inside the task, so an argument the Papyrus VM did not allocate faults the task. Only the callback frees the list once it is built.
        // The handlers go to Create because the Papyrus VM can run and release the call as soon as it accepts it.
        PapyrusCallback callback = PapyrusCallback.Create(log, (arguments ?? new PapyrusArguments()).Build(), returnValue =>
        {
            // Converted here, because returnValue points into the Papyrus VM's stack, which is released once this delegate returns.
            try
            {
                T convertedValue = convertReturnValue(returnValue);
                taskCompletionSource.SetResult(convertedValue);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        },
        taskCompletionSource.SetCanceled);

        dispatch(virtualMachine, callback);
        return await taskCompletionSource.Task;
    }

    private static void DispatchPropertyThroughCallback(string className, string propertyName, PapyrusCallback callback, string readOrWrite, Func<int> dispatch)
    {
        int status = dispatch();
        // Statuses 1 and 2 return before the callback reaches a smart pointer. Status 3 returns after the repaired accessor's smart pointer has released it.
        if (status is 1 or 2)
        {
            callback.DeleteCallback();
        }
        if (status != 0)
        {
            throw new InvalidOperationException(status switch
            {
                1 => $"There is no Papyrus virtual machine, so the {readOrWrite} of {className}.{propertyName} did not happen.",
                2 => $"That form has no instance of {className} bound to it, so it has no {propertyName} to {readOrWrite}.",
                3 => $"The Papyrus virtual machine did not {readOrWrite} {className}.{propertyName}.",
                _ => $"The {readOrWrite} of {className}.{propertyName} returned the unrecognized status {status}."
            });
        }
    }

    private static void GetPropertyIntoCallback(IForm receiver, string className, string propertyName, PapyrusCallback callback)
    {
        DispatchPropertyThroughCallback(className, propertyName, callback, "read", () => NativePapyrus.SF_Papyrus_GetPropertyIntoCallback(receiver.Address, className, propertyName, callback.Functor.Address));
    }

    // The Papyrus VM copies the value during the call, then releases the callback and the value as the call returns.
    private static void SetPropertyFromCallback(IForm receiver, string className, string propertyName, Variable value, PapyrusCallback callback)
    {
        DispatchPropertyThroughCallback(className, propertyName, callback, "write", () => NativePapyrus.SF_Papyrus_SetPropertyIntoCallback(receiver.Address, className, propertyName, value.Address, callback.Functor.Address));
    }
}

internal static partial class NativePapyrus
{
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_GetPropertyIntoCallback(nint receiver, string className, string propertyName, nint callback);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_SetPropertyIntoCallback(nint receiver, string className, string propertyName, nint value, nint callback);
}
