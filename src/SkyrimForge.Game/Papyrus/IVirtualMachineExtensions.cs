using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Papyrus;

internal static class IVirtualMachineExtensions
{
    private static void ThrowIfNotDispatched(bool dispatched, string className, string functionName)
    {
        if (!dispatched)
        {
            throw new InvalidOperationException($"The Papyrus virtual machine rejected the well-formed call to {className}.{functionName}.");
        }
    }

    // A dispatch with no bound object ends the process. Binding here would make a function call create an instance of the script.
    private static void ThrowIfNotBound(ulong receiverHandle, string className, string functionName)
    {
        int status = NativePapyrus.SF_Papyrus_HasBoundObject(receiverHandle, className);
        if (status != 0)
        {
            throw new InvalidOperationException(status switch
            {
                1 => $"There is no Papyrus virtual machine, so {className}.{functionName} was not dispatched.",
                3 => $"That form has no instance of {className} bound to it, and dispatching "
                    + $"{className}.{functionName} on it would end the process. Bind one first with {nameof(PapyrusScriptDispatcher)}.{nameof(PapyrusScriptDispatcher.Bind)}.",
                _ => $"Testing whether {className} is bound to that form returned the unrecognized status {status}."
            });
        }
    }

    private static ulong GetHandle<T>(T virtualMachine, IForm receiver) where T : IVirtualMachine_
    {
        if (virtualMachine.GetObjectHandlePolicy() is not { } policy)
        {
            throw new InvalidOperationException("The Papyrus virtual machine has no object handle policy, so a function has no receiver.");
        }

        ulong receiverHandle = policy.GetHandleForObject(receiver.GetFormType(), receiver);
        return receiverHandle != policy.EmptyHandle() ? receiverHandle : throw new InvalidOperationException("The Papyrus virtual machine has no handle for that form, so that form cannot be a receiver.");
    }

    // Dispatching a name the Papyrus VM cannot resolve ends the process, and binding the arguments detects that first.
    extension<T>(T self) where T : IVirtualMachine_
    {
        public void DispatchStaticCall(string className, string functionName, PapyrusCallback callback)
        {
            try
            {
                callback.BindArguments(className, functionName);
            }
            catch (Exception)
            {
                callback.DeleteCallback();
                throw;
            }
            // Outside the try. The shim's smart pointer releases the callback if the Papyrus VM returns false, so deleting it here would free it twice.
            bool dispatched = self.DispatchStaticCall(className, functionName, callback.Arguments, callback.Functor);
            ThrowIfNotDispatched(dispatched, className, functionName);
        }

        public void DispatchFunctionCall(IForm receiver, string className, string functionName, PapyrusCallback callback)
        {
            ulong receiverHandle;
            try
            {
                receiverHandle = GetHandle(self, receiver);
                callback.BindArguments(className, functionName);
                ThrowIfNotBound(receiverHandle, className, functionName);
            }
            catch (Exception)
            {
                callback.DeleteCallback();
                throw;
            }
            // Outside the try. The shim's smart pointer releases the callback if the Papyrus VM returns false, so deleting it here would free it twice.
            bool dispatched = self.DispatchMethodCall(receiverHandle, className, functionName, callback.Arguments, callback.Functor);
            ThrowIfNotDispatched(dispatched, className, functionName);
        }
    }
}

internal static partial class NativePapyrus
{
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_HasBoundObject(ulong receiverHandle, string className);
}
