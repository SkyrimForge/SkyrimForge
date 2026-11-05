using SkyrimForge.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

/// <remarks>Call members on the game's thread.</remarks>
internal sealed class PlayerTopic(FormId<Quest>? questId, byte priority, VoiceSilencer voiceSilencer, TopicHider topicHider, Action<Exception> reportFailure)
{
    private static readonly FormId<Quest> dialogueGeneric = new(0x00013EB3);
    private static readonly FormId<TopicInfo> offerServicesInfo = new(0x0007F6BC);

    private readonly GameEvent<PlayerTopicSelectedEventArgs> onSelected = new(reportFailure);

    // One info per text, so the game marks each text said separately.
    private readonly Dictionary<string, TopicInfo> textsToInfos = new(StringComparer.Ordinal);

    // Tracked, since TopicHider adds conditions of the same function to the same list.
    private readonly Dictionary<string, TESConditionItem> textsToSpeakerConditions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Reply> textsToReplies = new(StringComparer.Ordinal);
    private string text = "";

    private nint speakerAddress;

    private Topic topic;
    private TopicInfo record;
    private GlobalVariable global;
    private bool visible;
    private bool said;
    private string? exceptionMessage;

    private bool IsReady => topic.Exists;

    public string Text
    {
        get => text;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ThrowIfFailed();
            if (value != text)
            {
                if (IsReady)
                {
                    ApplyText(value);
                }
                text = value;
            }
        }
    }

    public IGameEvent<PlayerTopicSelectedEventArgs> OnSelected => onSelected;

    public bool IsSaid
    {
        get => textsToInfos.TryGetValue(text, out TopicInfo info) && info.SaidOnce;
        set
        {
            said = value;
            if (textsToInfos.TryGetValue(text, out TopicInfo info))
            {
                info.SaidOnce = value;
            }
        }
    }

    public void Show<TSpeaker>(TSpeaker speaker) where TSpeaker : IObjectReference
    {
        ThrowIfFailed();
        speakerAddress = speaker.Address;
        foreach ((string infoText, TopicInfo info) in textsToInfos)
        {
            SetSpeakerCondition(infoText, info);
        }
        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    // A load frees the speaker's address.
    internal void ResetForLoad() => speakerAddress = 0;

    // The priority is set before the quest lists the topic, in case the game orders a quest's branches as they are added.
    internal bool CreateRecords()
    {
        Quest owner = (questId ?? dialogueGeneric).Form;
        record = offerServicesInfo.Form;
        topic = RuntimeForms.Create(FormType.Dialogue).AsTopic()!.Value;
        global = RuntimeConditions.CreateGlobal();
        DIALOGUE_DATA data = topic.Data;
        data.Type = DIALOGUE_TYPES.DIALOGUE_TYPE.kPlayerDialogue;
        data.SetSubtype(DIALOGUE_DATA.Subtype.kCustom);
        topic.PriorityAndJournalIndex = ((uint)priority << 24) | (topic.PriorityAndJournalIndex & 0x00FFFFFFu);
        ApplyText(text);
        SetVisible(visible);
        return owner.AddPlayerTopic(topic);
    }

    private void SetVisible(bool value)
    {
        visible = value;
        if (IsReady)
        {
            global.Value = value ? 1f : 0f;
        }
    }

    internal void OnException(string exceptionMessage) => this.exceptionMessage = exceptionMessage;

    private void ThrowIfFailed()
    {
        if (exceptionMessage != null)
        {
            throw new InvalidOperationException(exceptionMessage);
        }
    }

    internal string? GetText(FormId<TopicInfo> infoId) => textsToInfos.FirstOrDefault(entry => entry.Value.FormId == infoId).Key;

    internal void SetReply(Reply reply)
    {
        textsToReplies[text] = reply;
        if (textsToInfos.TryGetValue(text, out TopicInfo info))
        {
            ApplyReply(info, reply);
        }
    }

    internal void RaiseSelected(string chosenText, Task replyEndTask) => onSelected.Raise(new PlayerTopicSelectedEventArgs(this, chosenText, replyEndTask));

    private void ApplyReply(TopicInfo info, Reply reply)
    {
        // CommonLib declares flag bit 0 as kStartSceneOnEnd. The game ends the conversation after an info with it, as the Creation Kit's Goodbye flag does.
        const TOPIC_INFO_DATA.TOPIC_INFO_FLAGS goodbyeFlag = TOPIC_INFO_DATA.TOPIC_INFO_FLAGS.kStartSceneOnEnd;
        voiceSilencer.UpdateResponses(info, reply.Responses, true);
        TOPIC_INFO_DATA data = info.Data;
        data.Flags = reply.IsGoodbye ? data.Flags | goodbyeFlag : data.Flags & ~goodbyeFlag;
    }

    private void ApplyText(string value)
    {
        TopicInfo info = textsToInfos.TryGetValue(value, out TopicInfo knownInfo) ? knownInfo : CreateInfo(value);
        info.SaidOnce = said;
        topic.AsTESFullName().SetFullName(value);
        topic.TopicInfos.Replace([info]);
    }

    private TopicInfo CreateInfo(string text)
    {
        TopicInfo info = RuntimeForms.Create(FormType.Info).AsTopicInfo()!.Value;
        info.ParentTopic = topic;
        info.DataInfo = record;
        TESCondition conditions = info.ObjConditions;
        conditions.Head = RuntimeConditions.Create(FUNCTION_DATA.FunctionID.kGetGlobalValue, global.Address);
        if (speakerAddress != 0)
        {
            SetSpeakerCondition(text, info);
        }
        textsToInfos[text] = info;
        ApplyReply(info, textsToReplies.TryGetValue(text, out Reply? reply) ? reply : new Reply([], false));
        topicHider.AddInfo(info, this);
        return info;
    }

    private void SetSpeakerCondition(string infoText, TopicInfo info)
    {
        if (textsToSpeakerConditions.TryGetValue(infoText, out TESConditionItem existing))
        {
            existing.Data.FunctionData.Params.Set(0, speakerAddress);
            return;
        }
        textsToSpeakerConditions[infoText] = RuntimeConditions.PrependIsReference(info, speakerAddress, 1f);
    }
}
