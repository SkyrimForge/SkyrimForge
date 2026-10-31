using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

// Call members on the game's thread.
internal sealed class TopicHider
{
    private readonly GameTarget gameTarget;

    private readonly List<(TopicInfo Info, PlayerTopic? Topic)> infos = [];

    // conditionsBySlot[slot][i] is the condition that slot placed in infos[i].
    private readonly List<List<TESConditionItem>> conditionsBySlot = [];
    private readonly List<Actor?> speakersBySlot = [];

    private readonly Dictionary<object, (int Slot, IReadOnlyCollection<PlayerTopic> Topics)> owners = [];
    private bool dataInfosAdded;

    public TopicHider(GameTarget gameTarget, IGameEvents events)
    {
        this.gameTarget = gameTarget;
        // A load frees the hidden actors' addresses.
        events.OnNewGameOrSaveLoading.Subscribe(_ => ReleaseAll());
    }

    // The player is never the speaker, so a condition on this address matches no speaker.
    private nint NobodyAddress => gameTarget.Player.Address;

    internal void AddInfo(TopicInfo info, PlayerTopic topic) => Add(info, topic);

    internal void Hide(object owner, Actor speaker, IReadOnlyCollection<PlayerTopic> ownTopics)
    {
        Release(owner);
        AddDataInfos();
        int slot = speakersBySlot.FindIndex(hidden => hidden?.FormId == speaker.FormId);
        if (slot < 0)
        {
            slot = speakersBySlot.FindIndex(hidden => hidden == null);
        }
        if (slot < 0)
        {
            slot = AddSlot();
        }
        speakersBySlot[slot] = speaker;
        owners[owner] = (slot, ownTopics);
        Apply(slot);
    }

    internal void Release(object owner)
    {
        if (!owners.Remove(owner, out (int Slot, IReadOnlyCollection<PlayerTopic> Topics) entry))
        {
            return;
        }
        if (!owners.Values.Any(other => other.Slot == entry.Slot))
        {
            speakersBySlot[entry.Slot] = null;
        }
        Apply(entry.Slot);
    }

    private void ReleaseAll()
    {
        owners.Clear();
        for (int slot = 0; slot < speakersBySlot.Count; slot++)
        {
            speakersBySlot[slot] = null;
            Apply(slot);
        }
    }

    private void Add(TopicInfo info, PlayerTopic? topic)
    {
        infos.Add((info, topic));
        for (int slot = 0; slot < conditionsBySlot.Count; slot++)
        {
            conditionsBySlot[slot].Add(Prepend(info, GetParameter(slot, topic)));
        }
    }

    // The menu lists only the starting topics of top-level branches, and the game opens a conversation with that of a blocking branch.
    private void AddDataInfos()
    {
        if (dataInfosAdded)
        {
            return;
        }
        dataInfosAdded = true;
        const BGSDialogueBranch.Flag listedBranches = BGSDialogueBranch.Flag.kTopLevel | BGSDialogueBranch.Flag.kBlocking;
        foreach (Topic topic in gameTarget.DataHandler.GetFormArray<Topic>())
        {
            if (topic.FormId.Value < 0xFF000000 && topic.Data.Type == DIALOGUE_TYPES.DIALOGUE_TYPE.kPlayerDialogue
                && topic.OwnerBranch is { } branch && (branch.Flags & listedBranches) != 0 && branch.StartingTopic?.FormId == topic.FormId)
            {
                foreach (TopicInfo info in topic.TopicInfos.Get())
                {
                    Add(info, null);
                }
            }
        }
    }

    private int AddSlot()
    {
        nint nobody = NobodyAddress;
        conditionsBySlot.Add([.. infos.Select(entry => Prepend(entry.Info, nobody))]);
        speakersBySlot.Add(null);
        return conditionsBySlot.Count - 1;
    }

    private void Apply(int slot)
    {
        List<TESConditionItem> conditions = conditionsBySlot[slot];
        for (int i = 0; i < infos.Count; i++)
        {
            conditions[i].Data.FunctionData.Params.Set(0, GetParameter(slot, infos[i].Topic));
        }
    }

    private nint GetParameter(int slot, PlayerTopic? topic) =>
        speakersBySlot[slot] is { } speaker && (topic == null || !owners.Values.Any(entry => entry.Slot == slot && entry.Topics.Contains(topic))) ? speaker.Address : NobodyAddress;

    // The condition is joined to the rest with "and," which the game evaluates after every "or," so it applies to the info as a whole.
    private static TESConditionItem Prepend(TopicInfo info, nint reference)
    {
        TESConditionItem condition = RuntimeConditions.Create(FUNCTION_DATA.FunctionID.kGetIsReference, reference, 0f);
        TESCondition conditions = info.ObjConditions;
        condition.Next = conditions.Head;
        conditions.Head = condition;
        return condition;
    }
}
