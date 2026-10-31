using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Papyrus;

internal sealed class PapyrusFunctionRegistrar(ILog log) : IPapyrusFunctionRegistrar
{
    // Replaced rather than mutated, because calls on a Papyrus VM thread read it without a lock.
    private static volatile Dictionary<int, Func<Variable, StackFrame, Variable, uint, bool>> implementations = [];

    private static bool installed;

    private static int nextToken;

    private readonly ILog log = log;

    public void RegisterStatic(string className, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Action<PapyrusCall> body)
    {
        ArgumentException.ThrowIfNullOrEmpty(className);
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(body);
        Bind(className, returnType, functionName, parameterTypes, AdaptImmediate(body), isStatic: true, isLatent: false, FormType.None);
    }

    public void RegisterInstance(string selfClassName, FormType selfType, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Action<PapyrusCall> body)
    {
        ArgumentException.ThrowIfNullOrEmpty(selfClassName);
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(body);
        Bind(selfClassName, returnType, functionName, parameterTypes, AdaptImmediate(body), isStatic: false, isLatent: false, selfType);
    }

    public void RegisterLatentStatic(string className, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Func<PapyrusCall, PapyrusLatentCall, bool> body)
    {
        ArgumentException.ThrowIfNullOrEmpty(className);
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(body);
        ThrowIfArray(returnType);
        Bind(className, returnType, functionName, parameterTypes, AdaptLatent(body, returnType.ObjectType), isStatic: true, isLatent: true, FormType.None);
    }

    public void RegisterLatentInstance(string selfClassName, FormType selfType, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Func<PapyrusCall, PapyrusLatentCall, bool> body)
    {
        ArgumentException.ThrowIfNullOrEmpty(selfClassName);
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        ArgumentNullException.ThrowIfNull(parameterTypes);
        ArgumentNullException.ThrowIfNull(body);
        ThrowIfArray(returnType);
        Bind(selfClassName, returnType, functionName, parameterTypes, AdaptLatent(body, returnType.ObjectType), isStatic: false, isLatent: true, selfType);
    }

    private static void ThrowIfArray(PapyrusType returnType)
    {
        if (returnType.IsArray)
        {
            throw new ArgumentException("A latent function cannot return an array.", nameof(returnType));
        }
    }

    private static Func<PapyrusCall, uint, bool> AdaptImmediate(Action<PapyrusCall> body)
    {
        return (call, _) =>
        {
            body(call);
            return true;
        };
    }

    private static Func<PapyrusCall, uint, bool> AdaptLatent(Func<PapyrusCall, PapyrusLatentCall, bool> body, FormType returnObjectType)
    {
        return (call, stackId) => body(call, new PapyrusLatentCall(stackId, returnObjectType));
    }

    private unsafe void Bind(string className, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Func<PapyrusCall, uint, bool> body, bool isStatic, bool isLatent, FormType selfObjectType)
    {
        TypeInfo.RawType[] rawTypes = [.. parameterTypes.Select(parameter => parameter.Raw)];
        FormType[] objectTypes = [.. parameterTypes.Select(parameter => parameter.ObjectType)];

        bool InvokeBodyWithTryCatch(Variable returnValue, StackFrame frame, Variable self, uint stackId)
        {
            try
            {
                // Inside the try because reading the frame and defaulting the variable can throw.
                returnValue.SetNone();
                return body(new PapyrusCall(frame, returnValue, objectTypes, returnType.ObjectType, returnType.Raw, self, selfObjectType, isLatent), stackId);
            }
            catch (Exception ex)
            {
                // For a latent function, false means the work did not start, so the script continues with None.
                log.Write($"{className}.{functionName} threw and did not run: {ex}");
                return false;
            }
        }

        int token;
        if (!installed)
        {
            NativePapyrus.SF_Papyrus_SetCallback(&OnCall);
            installed = true;
        }
        token = nextToken++;
        implementations = new Dictionary<int, Func<Variable, StackFrame, Variable, uint, bool>>(implementations)
        {
            [token] = InvokeBodyWithTryCatch
        };

        int status = NativePapyrus.SF_Papyrus_BindFunction(className, functionName, rawTypes, objectTypes, rawTypes.Length, returnType.Raw, returnType.ObjectType, token, isStatic, isLatent);
        string? failure = status switch
        {
            0 => DescribeUnbound(className, functionName, token),
            1 => $"The Papyrus virtual machine does not exist yet, so {className}.{functionName} was not registered. Register from IGameEvents.OnDataLoaded.",
            2 => $"{className}.{functionName} declares a name or a parameter list the native side cannot bind. A function declares at most eleven parameters.",
            3 => DescribeUnbound(className, functionName, token)
                ?? $"The Papyrus virtual machine returned false for {className}.{functionName}, as it does where the registered types disagree with the script's declaration.",
            4 => $"{className}.{functionName} is already registered, by this plugin or another. Papyrus calls the earlier registration.",
            _ => $"Registering {className}.{functionName} returned the unrecognized status {status}."
        };
        if (failure == null)
        {
            return;
        }

        var remaining = new Dictionary<int, Func<Variable, StackFrame, Variable, uint, bool>>(implementations);
        remaining.Remove(token);
        implementations = remaining;
        throw new InvalidOperationException(failure);
    }

    // Returns null if the script's declaration is bound to the function, or the script has not linked. Loads the script if the game has not, which links it.
    private static unsafe string? DescribeUnbound(string className, string functionName, int token)
    {
        const int bufferSize = 512;
        byte* scriptDeclaration = stackalloc byte[bufferSize];
        byte* registration = stackalloc byte[bufferSize];
        registration[0] = 0;
        return NativePapyrus.SF_Papyrus_GetBinding(className, functionName, token, scriptDeclaration, registration, bufferSize) switch
        {
            2 => $"{className}.{functionName} cannot be called, because the game found no compiled {className}.pex in its Scripts folder.",
            3 => $"{className}.{functionName} cannot be called, because {className} declares no function named {functionName}.",
            4 => $"{className}.{functionName} is unbound, so a call returns None without running. The script declares "
                + $"\"{Marshal.PtrToStringUTF8((nint)scriptDeclaration)}\", and the registration declares \"{Marshal.PtrToStringUTF8((nint)registration)}\".",
            _ => null
        };
    }

    // Exceptions are caught because none may propagate into the Papyrus VM's dispatch.
    [UnmanagedCallersOnly]
    private static byte OnCall(int token, nint returnValueAddress, nint frameAddress, nint selfAddress, uint stackId)
    {
        try
        {
            // The Papyrus VM passes a receiver Variable for every function, None for a static one, so a zero address is a fault.
            if (!implementations.TryGetValue(token, out Func<Variable, StackFrame, Variable, uint, bool>? implementation)
                || Variable.OrNull(returnValueAddress) is not { } returnValue
                || StackFrame.OrNull(frameAddress) is not { } frame
                || Variable.OrNull(selfAddress) is not { } self)
            {
                return 0;
            }
            return implementation(returnValue, frame, self, stackId) ? (byte)1 : (byte)0;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}

internal static unsafe partial class NativePapyrus
{
    [LibraryImport(library)]
    internal static partial void SF_Papyrus_SetCallback(delegate* unmanaged<int, nint, nint, nint, uint, byte> handler);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_GetBinding(string className, string functionName, int token, byte* scriptDeclaration, byte* registration, int bufferSize);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_BindFunction(string className, string functionName, [In] TypeInfo.RawType[] parameterRawTypes, [In] FormType[] parameterObjectTypes, int parameterCount, TypeInfo.RawType returnRawType, FormType returnObjectType, int token, [MarshalAs(UnmanagedType.U1)] bool isStatic, [MarshalAs(UnmanagedType.U1)] bool isLatent);
}
