using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

internal sealed unsafe class StoryEvents : IStoryEvents
{
    // Replaced, never mutated, so a raise never enumerates a list being written.
    private static StoryEvents[] instances = [];

    private readonly GameTarget gameTarget;
    private readonly ILog log;
    private readonly GameEvent<StoryEventArgs> onStoryEvent;

    private IReadOnlyList<StoryEventKind>? kinds;
    private bool hooked;

    public StoryEvents(GameTarget gameTarget, ILog log)
    {
        this.gameTarget = gameTarget;
        this.log = log;
        onStoryEvent = new GameEvent<StoryEventArgs>(ex => Report(nameof(OnStoryEvent), ex));
        instances = [.. instances, this];
    }

    // Hooked on the first read, not in the constructor, so a plugin that never subscribes installs nothing and a test calls no native code.
    public IGameEvent<StoryEventArgs> OnStoryEvent
    {
        get
        {
            Hook();
            return onStoryEvent;
        }
    }

    private void Hook()
    {
        if (hooked)
        {
            return;
        }
        hooked = true;
        GameFunction addEvent = GameFunction.BGSStoryEventManager__AddEvent_Impl;
        if (NativeSupplements.SF_Story_Register(&OnStoryRaised, NativeSupplements.SF_Hook_FunctionAddress(addEvent.SpecialEdition, addEvent.AnniversaryEdition, addEvent.VR)))
        {
            log.Write("Story events hooked the manager.");
            return;
        }
        log.Write("Story events are unavailable. The story manager's AddEvent function did not resolve or could not be hooked.");
    }

    [UnmanagedCallersOnly]
    private static void OnStoryRaised(uint index, nint members, int count)
    {
        foreach (StoryEvents instance in instances)
        {
            try
            {
                instance.Raise(index, new ReadOnlySpan<ulong>((void*)members, count));
            }
            catch (Exception ex)
            {
                instance.Report(nameof(OnStoryRaised), ex);
            }
        }
    }

    private void Raise(uint index, ReadOnlySpan<ulong> members)
    {
        // An empty table is not retained, so a raise arriving before the game registers its kinds reads again on the next one.
        IReadOnlyList<StoryEventKind> describedKinds = kinds ?? Describe();
        if (describedKinds.Count == 0)
        {
            return;
        }
        kinds = describedKinds;
        if (index >= describedKinds.Count)
        {
            log.Write($"Story event index {index} is outside the {describedKinds.Count} kinds the manager describes.");
            return;
        }
        StoryEventKind storyEventKind = describedKinds[(int)index];

        var values = new StoryEventValue[storyEventKind.Values.Count];
        foreach (int i in Enumerable.Range(0, values.Length))
        {
            values[i] = Read(storyEventKind.Values[i], i < members.Length ? members[i] : 0);
        }
        onStoryEvent.Raise(new StoryEventArgs(storyEventKind.Signature, storyEventKind.Name, values));
    }

    // A handle is resolved here because the reference it identifies can be gone by the time a subscriber reads it.
    private static StoryEventValue Read(ValueDeclaration declaredValue, ulong member)
    {
        if (declaredValue.Kind == StoryEventValueKind.Reference)
        {
            ObjectReference? reference = ObjectReference.LookupByHandle((uint)member);
            return new StoryEventValue(declaredValue.Name, declaredValue.Kind, reference?.Address ?? 0, 0);
        }
        if (declaredValue.Kind == StoryEventValueKind.Number)
        {
            return new StoryEventValue(declaredValue.Name, declaredValue.Kind, 0, (long)member);
        }
        return new StoryEventValue(declaredValue.Name, declaredValue.Kind, (nint)member, 0);
    }

    private List<StoryEventKind> Describe()
    {
        var describedKinds = new List<StoryEventKind>();
        foreach (BGSRegisteredStoryEvent storyEvent in gameTarget.StoryEventManager.RegisteredEvents.Get())
        {
            var values = new List<ValueDeclaration>();
            foreach (BGSStoryEventMember member in storyEvent.Members.Get())
            {
                values.Add(new ValueDeclaration(member.Name, (StoryEventValueKind)member.Type));
            }
            describedKinds.Add(new StoryEventKind(storyEvent.UniqueID, storyEvent.Name, values));
        }
        return describedKinds;
    }

    private void Report(string operation, Exception ex)
    {
        log.Write($"{operation} threw: {ex}");
    }

    private sealed record StoryEventKind(string Signature, string Name, IReadOnlyList<ValueDeclaration> Values);

    private sealed record ValueDeclaration(string Name, StoryEventValueKind Kind);
}

internal static unsafe partial class NativeSupplements
{
    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Story_Register(delegate* unmanaged<uint, nint, int, void> handler, nint addEvent);

    [LibraryImport(library)]
    internal static partial nint SF_Hook_FunctionAddress(ulong se, ulong ae, ulong vr);
}
