using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class PlayerTopic : IPlayerTopic
{
    private readonly GameEvent<PlayerTopicSelectedEventArgs> onSelected;
    private readonly VoiceSilencer voiceSilencer;
    private readonly TopicHider topicHider;

    // One info per text, so the game marks each text said separately.
    private readonly Dictionary<string, TopicInfo> textsToInfos = new(StringComparer.Ordinal);

    // Tracked, since TopicHider adds conditions of the same function to the same list.
    private readonly Dictionary<string, TESConditionItem> textsToSpeakerConditions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Reply> textsToReplies = new(StringComparer.Ordinal);
    private string text = "";

    private nint speakerAddress;

    private TopicInfo record;
    private GlobalVariable global;
    private bool visible;
    private bool said;
    private string? exceptionMessage;

    internal PlayerTopic(FormId<Quest>? questId, byte priority, VoiceSilencer voiceSilencer, TopicHider topicHider, Action<Exception> reportFailure)
    {
        QuestId = questId;
        Priority = priority;
        onSelected = new GameEvent<PlayerTopicSelectedEventArgs>(reportFailure);
        this.voiceSilencer = voiceSilencer;
        this.topicHider = topicHider;
    }

    internal FormId<Quest>? QuestId { get; }

    internal byte Priority { get; }

    internal Topic Topic { get; private set; }

    private bool IsReady => Topic.Exists;

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
        visible = true;
        if (IsReady)
        {
            GlobalVariable shownGlobal = global;
            shownGlobal.Value = 1f;
        }
    }

    public void Hide()
    {
        visible = false;
        if (IsReady)
        {
            GlobalVariable shownGlobal = global;
            shownGlobal.Value = 0f;
        }
    }

    // A load frees the speaker's address.
    internal void ResetForLoad() => speakerAddress = 0;

    internal void Attach(Topic topic, TopicInfo responseRecord, GlobalVariable shownGlobal)
    {
        Topic = topic;
        record = responseRecord;
        global = shownGlobal;
        ApplyText(text);
        GlobalVariable value = global;
        value.Value = visible ? 1f : 0f;
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
        Topic.AsTESFullName().SetFullName(value);
        Topic.TopicInfos.Replace([info]);
    }

    private TopicInfo CreateInfo(string text)
    {
        TopicInfo info = RuntimeForms.Create(FormType.Info).AsTopicInfo()!.Value;
        info.ParentTopic = Topic;
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
        TESConditionItem condition = RuntimeConditions.Create(FUNCTION_DATA.FunctionID.kGetIsReference, speakerAddress);
        TESCondition conditions = info.ObjConditions;
        condition.Next = conditions.Head;
        conditions.Head = condition;
        textsToSpeakerConditions[infoText] = condition;
    }
}
