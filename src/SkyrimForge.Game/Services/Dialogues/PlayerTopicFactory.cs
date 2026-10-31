using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class PlayerTopicFactory
{
    private readonly GameTarget gameTarget;
    private readonly IUtility utility;
    private readonly IUnvoicedSpeech unvoicedSpeech;
    private readonly VoiceSilencer voiceSilencer;
    private readonly TopicHider topicHider;
    private readonly ILog log;

    private readonly List<PlayerTopic> allTopics = [];
    private readonly List<PlayerTopic> pendingTopics = [];

    // Read and written on the game's thread.
    private TaskCompletionSource? replyEndSource;

    public PlayerTopicFactory(GameTarget gameTarget, IGameEvents events, IUtility utility, IUnvoicedSpeech unvoicedSpeech, VoiceSilencer voiceSilencer, TopicHider topicHider, ILog log)
    {
        this.gameTarget = gameTarget;
        this.utility = utility;
        this.unvoicedSpeech = unvoicedSpeech;
        this.voiceSilencer = voiceSilencer;
        this.topicHider = topicHider;
        this.log = log;
        events.OnFrame.Subscribe(CreatePendingTopics);
        events.OnFrame.Subscribe(_ => EndReplyOutsideConversation());
        events.OnTopicInfo.Subscribe(OnTopicInfoAsync);
        events.OnNewGameOrSaveLoading.Subscribe(_ => HideAll());
    }

    internal PlayerTopic Create(byte priority, FormId<Quest>? questId) => Add(new PlayerTopic(questId, priority, voiceSilencer, topicHider, ReportFailure));

    private PlayerTopic Add(PlayerTopic topic)
    {
        allTopics.Add(topic);
        pendingTopics.Add(topic);
        return topic;
    }

    private void ReportFailure(Exception ex) => log.Write($"A subscriber to a player topic's OnSelected threw: {ex}");

    private bool ConversationOpen => gameTarget.MenuTopicManager is { MenuOpen: true };

    // The game reads a quest's branches when it builds a conversation, so topics are added only while none is open.
    private void CreatePendingTopics(FrameEventArgs args)
    {
        if (pendingTopics.Count == 0 || gameTarget.MenuTopicManager is not { MenuOpen: false })
        {
            return;
        }
        PlayerTopic[] topicsToAdd = [.. pendingTopics];
        pendingTopics.Clear();
        foreach (PlayerTopic topic in topicsToAdd)
        {
            try
            {
                if (!PlayerTopicRecords.Create(topic))
                {
                    log.Write($"The player topic \"{topic.Text}\" was not added because its quest's table of branches does not list the topic's branch.");
                }
            }
            catch (InvalidOperationException ex)
            {
                topic.OnException(ex.Message);
                log.Write($"The player topic \"{topic.Text}\" has no records: {ex.Message}");
            }
        }
    }

    // Form IDs are read at comparison time, since a created form receives a new ID when a save loads. A topic event arrives on a worker thread.
    private async Task OnTopicInfoAsync(TopicInfoEventArgs args)
    {
        bool begins = args.Type == TESTopicInfoEvent.TopicInfoEventType.kTopicBegin;
        if ((!begins && args.Type != TESTopicInfoEvent.TopicInfoEventType.kTopicEnd) || args.TopicInfoFormID is not { } topicInfoId)
        {
            return;
        }
        FormId<TopicInfo> id = (FormId<TopicInfo>)topicInfoId;
        ObjectReference? speaker = args.SpeakerReference;
        await utility.WaitForGameThreadAsync();
        // Copied, since a handler of the selection can create topics.
        foreach (PlayerTopic topic in allTopics.ToArray())
        {
            if (topic.GetText(id) is { } chosenText)
            {
                if (begins)
                {
                    topic.RaiseSelected(chosenText, Begin(speaker));
                }
                else
                {
                    replyEndSource?.TrySetResult();
                }
            }
        }
    }

    // The game can close a conversation without ending the reply.
    private void EndReplyOutsideConversation()
    {
        if (replyEndSource is { Task.IsCompleted: false } && !ConversationOpen)
        {
            replyEndSource.TrySetResult();
        }
    }

    private async Task Begin(ObjectReference? speaker)
    {
        replyEndSource?.TrySetResult();
        replyEndSource = null;
        if (speaker?.AsActor() is not { } speakerActor)
        {
            return;
        }
        unvoicedSpeech.Stop(speakerActor);
        replyEndSource = new TaskCompletionSource();
        await replyEndSource.Task;
    }

    private void HideAll()
    {
        foreach (PlayerTopic topic in allTopics)
        {
            topic.Hide();
            topic.ResetForLoad();
        }
    }
}
