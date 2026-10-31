using SkyrimForge.Game;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using unsafe ActivateFunction = delegate* unmanaged[Thiscall]<SkyrimForge.Game.ActorBase, SkyrimForge.Game.ObjectReference, SkyrimForge.Game.ObjectReference, byte, SkyrimForge.Game.TESBoundObject, int, byte>;
using unsafe ProcessMessageFunction = delegate* unmanaged[Thiscall]<SkyrimForge.Game.StatsMenu, SkyrimForge.Game.UIMessage, SkyrimForge.Game.UI_MESSAGE_RESULTS>;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Replaces the virtual table entries of StatsMenu::ProcessMessage and TESNPC::Activate, logs calls other than menu updates, and calls the original.</summary>
/// <remarks>The pointer parameters are declared as view types, which are one address each, so they match the game's own parameters.</remarks>
public sealed class VirtualFunctionPlugin(IFunctionHooks functionHooks, ILog log) : IPlugin
{
    private const int processMessageVTableIndex = 0x04;

    private const int activateVTableIndex = 0x37;

    private static ILog? replacementLog;
    private static nint originalProcessMessage;
    private static nint originalActivate;
    private static int updateCount;

    /// <inheritdoc/>
    public unsafe void Start()
    {
        replacementLog = log;
        ProcessMessageFunction processMessage = &OnProcessMessage;
        originalProcessMessage = functionHooks.ReplaceVirtualTableEntry("StatsMenu", processMessageVTableIndex, (nint)processMessage);
        ActivateFunction activate = &OnActivate;
        originalActivate = functionHooks.ReplaceVirtualTableEntry("TESNPC", activateVTableIndex, (nint)activate);
        log.Write("Replaced StatsMenu::ProcessMessage and TESNPC::Activate. Open the skills screen or talk to a character.");
    }

    // An exception must not escape into the game's code.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static unsafe UI_MESSAGE_RESULTS OnProcessMessage(StatsMenu self, UIMessage message)
    {
        try
        {
            if (!message.Exists)
            {
                replacementLog?.Write("StatsMenu::ProcessMessage ran for no message.");
            }
            else if (message.Type == UI_MESSAGE_TYPE.kUpdate)
            {
                updateCount++;
            }
            else if (message.Type == UI_MESSAGE_TYPE.kHide)
            {
                replacementLog?.Write($"StatsMenu::ProcessMessage ran for {UI_MESSAGE_TYPE.kHide}, after {updateCount} updates.");
                updateCount = 0;
            }
            else
            {
                replacementLog?.Write($"StatsMenu::ProcessMessage ran for {message.Type}.");
            }
        }
        catch (Exception)
        {
        }
        return ((ProcessMessageFunction)originalProcessMessage)(self, message);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvThiscall)])]
    private static unsafe byte OnActivate(ActorBase self, ObjectReference target, ObjectReference activator, byte arg3, TESBoundObject @object, int count)
    {
        byte result = ((ActivateFunction)originalActivate)(self, target, activator, arg3, @object, count);
        try
        {
            replacementLog?.Write($"TESNPC::Activate ran, {(@object.Exists ? "with an object" : "with no object")}, count {count}. It returned {result != 0}.");
        }
        catch (Exception)
        {
        }
        return result;
    }
}
