using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Game;

internal sealed class ScaleformFunctionRegistrar(ILog log) : IScaleformFunctionRegistrar
{
    // Static because the native side identifies a function by token alone. Concurrent because Call can run while another function is being registered.
    private static readonly ConcurrentDictionary<int, HandlerRegistration<Action<GFxFunctionHandler.Params>>> registrations = new();

    private static int nextToken;

    // Static because SKSE calls one callback for every plugin.
    private static readonly ConcurrentQueue<GameEvent<ScaleformMovieCreatedEventArgs>> movieEvents = new();

    private readonly ILog log = log;
    private GameEvent<ScaleformMovieCreatedEventArgs>? onMovieCreated;

    public unsafe void Register(IGFxMovie movie, GFxValue target, string name, Action<GFxFunctionHandler.Params> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(handler);

        // Recorded before the handler exists, since the movie can call the function as soon as it is a member.
        int token = Interlocked.Increment(ref nextToken);
        registrations[token] = new HandlerRegistration<Action<GFxFunctionHandler.Params>>(handler, log);

        NativeSupplements.SF_Scaleform_SetCallHandler(&Call);

        nint handlerAddress = NativeSupplements.SF_Scaleform_CreateFunctionHandler(token);
        if (GFxFunctionHandler.OrNull(handlerAddress) is not { } functionHandler)
        {
            registrations.TryRemove(token, out _);
            throw new InvalidOperationException("The Scaleform function handler could not be allocated.");
        }

        // SetMember copies the reference, so the function outlives this value.
        using (ScaleformValue function = ScaleformValue.Create())
        {
            movie.CreateFunction(function.Value, functionHandler);
            if (target.SetMember(name, function.Value))
            {
                return;
            }
        }

        registrations.TryRemove(token, out _);
        throw new InvalidOperationException($"The Scaleform object rejected a member named \"{name}\".");
    }

    public unsafe IGameEvent<ScaleformMovieCreatedEventArgs> OnMovieCreated
    {
        get
        {
            if (onMovieCreated == null)
            {
                if (!NativeSupplements.SF_Scaleform_SetMovieHandler(&RaiseMovieCreated))
                {
                    throw new InvalidOperationException("The running SKSE has no Scaleform interface.");
                }
                onMovieCreated = new GameEvent<ScaleformMovieCreatedEventArgs>(ex => log.Write($"A movie handler threw and was removed: {ex}"));
                movieEvents.Enqueue(onMovieCreated);
            }
            return onMovieCreated;
        }
    }

    [UnmanagedCallersOnly]
    private static void Call(int token, nint parameters)
    {
        if (!registrations.TryGetValue(token, out var registration))
        {
            return;
        }
        try
        {
            if (GFxFunctionHandler.Params.OrNull(parameters) is { } callParams)
            {
                registration.Handler(callParams);
            }
        }
        catch (Exception ex)
        {
            registration.Log.Write($"A Scaleform function threw, and its return value was left unchanged: {ex}");
        }
    }

    [UnmanagedCallersOnly]
    private static void RaiseMovieCreated(nint view, nint root)
    {
        if (GFxMovieView.OrNull(view) is not { } movie || GFxValue.OrNull(root) is not { } rootValue)
        {
            return;
        }
        // GameEvent.Raise catches a subscriber's exception, so none escapes this method.
        var args = new ScaleformMovieCreatedEventArgs(movie, rootValue);
        foreach (GameEvent<ScaleformMovieCreatedEventArgs> movieEvent in movieEvents)
        {
            movieEvent.Raise(args);
        }
    }
}

internal static unsafe partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_Scaleform_SetCallHandler(delegate* unmanaged<int, nint, void> handler);

    [LibraryImport(library)]
    internal static partial nint SF_Scaleform_CreateFunctionHandler(int token);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Scaleform_SetMovieHandler(delegate* unmanaged<nint, nint, void> handler);
}
