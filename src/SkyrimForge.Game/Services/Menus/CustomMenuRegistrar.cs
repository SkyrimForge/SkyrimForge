using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Game;

internal sealed class CustomMenuRegistrar(ILog log) : ICustomMenuRegistrar
{
    // Static because the native side identifies a menu by token alone. Concurrent because OnMessage can run while another menu is being registered.
    private static readonly ConcurrentDictionary<int, HandlerRegistration<Func<UI_MESSAGE_TYPE, UI_MESSAGE_RESULTS>>> registrations = new();

    private static int nextToken;

    private readonly ILog log = log;

    public unsafe void Register(string name, Func<UI_MESSAGE_TYPE, UI_MESSAGE_RESULTS> handler, string? movieFile = null, UI_MENU_FLAGS flags = UI_MENU_FLAGS.kNone, sbyte depth = 3, UserEvents.INPUT_CONTEXT_IDS.INPUT_CONTEXT_ID context = UserEvents.INPUT_CONTEXT_IDS.INPUT_CONTEXT_ID.kMenuMode)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(handler);

        // Recorded before the native call, since the game can construct the menu at once.
        int token = Interlocked.Increment(ref nextToken);
        registrations[token] = new HandlerRegistration<Func<UI_MESSAGE_TYPE, UI_MESSAGE_RESULTS>>(handler, log);

        NativeSupplements.SF_ManagedMenu_SetHandler(&OnMessage);
        int status = NativeSupplements.SF_UI_RegisterManagedMenu(name, token, movieFile, (uint)flags, depth, (uint)context);
        if (status == 0)
        {
            return;
        }

        registrations.TryRemove(token, out _);
        if (status == 4)
        {
            throw new ArgumentException($"The game already registers a menu called \"{name}\". To change a built-in menu, replace its virtual table entries through {nameof(IFunctionHooks)}.{nameof(IFunctionHooks.ReplaceVirtualTableEntry)}.", nameof(name));
        }
        throw new InvalidOperationException(status switch
        {
            1 => "The game has not built its interface yet.",
            2 => "Every managed menu registration is in use.",
            _ => $"Registering \"{name}\" returned the unrecognized status {status}."
        });
    }

    [UnmanagedCallersOnly]
    private static int OnMessage(int token, uint messageType)
    {
        if (!registrations.TryGetValue(token, out var registration))
        {
            return (int)UI_MESSAGE_RESULTS.kPassOn;
        }
        try
        {
            return (int)registration.Handler((UI_MESSAGE_TYPE)messageType);
        }
        catch (Exception ex)
        {
            registration.Log.Write($"A menu handler threw, and the message was passed on: {ex}");
        }
        return (int)UI_MESSAGE_RESULTS.kPassOn;
    }
}

internal static unsafe partial class NativeSupplements
{
    [LibraryImport(library)]
    internal static partial void SF_ManagedMenu_SetHandler(delegate* unmanaged<int, uint, int> handler);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_UI_RegisterManagedMenu(string name, int token, string? movieFile, uint flags, sbyte depth, uint context);
}
