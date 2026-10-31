using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkyrimForge.Game;

internal static unsafe class QuestDescriptions
{
    private static readonly Lock descriptionsLock = new();
    private static Dictionary<nint, Description> questAddressesToDescriptions = [];
    private static delegate* unmanaged<nint, nint, uint, void> originalGetJournalText;
    private static bool hooked;

    // Installed at data load, while no journal is open to call the function.
    internal static void Hook(IFunctionHooks functionHooks, ILog log)
    {
        lock (descriptionsLock)
        {
            if (hooked)
            {
                return;
            }
            hooked = true;
            delegate* unmanaged<nint, nint, uint, void> replacement = &OnJournalText;
            try
            {
                originalGetJournalText = (delegate* unmanaged<nint, nint, uint, void>)functionHooks.DetourFunction(GameFunction.TESQuest__GetJournalTextForInstance, (nint)replacement);
            }
            catch (InvalidOperationException ex)
            {
                log.Write($"Runtime quest descriptions are unavailable: {ex.Message}");
            }
        }
    }

    internal static void Set(Quest quest, string description, string questName, ILog log)
    {
        // Replaced, never mutated, because the journal reads it without the lock.
        lock (descriptionsLock)
        {
            questAddressesToDescriptions = new Dictionary<nint, Description>(questAddressesToDescriptions)
            {
                [quest.Address] = new Description(description, questName, log)
            };
        }
    }

    [UnmanagedCallersOnly]
    private static void OnJournalText(nint quest, nint text, uint instanceId)
    {
        if (questAddressesToDescriptions.TryGetValue(quest, out Description? description))
        {
            try
            {
                NativeSupplements.SF_Quest_WriteJournalText(text, description.Text);
                return;
            }
            catch (Exception ex)
            {
                description.Log.Write($"Quest \"{description.QuestName}\" description \"{description.Text}\" could not be written: {ex}");
            }
        }
        if (originalGetJournalText != null)
        {
            originalGetJournalText(quest, text, instanceId);
        }
    }

    private sealed record Description(string Text, string QuestName, ILog Log);
}

internal static partial class NativeSupplements
{
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial void SF_Quest_WriteJournalText(nint text, string description);
}
