using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class DialogueBranch : IDialogueBranch
{
    private readonly ISavedStateStringSet saidTexts;
    private readonly DialogueTopic startingTopic;
    private readonly Func<Actor, IEnumerable<TopicResponse>>? computeGreeting;
    private readonly Func<Actor, IEnumerable<TopicResponse>>? computeGoodbye;
    private readonly bool resumes;
    private readonly PlayerTopic[] otherTopics;
    private readonly PlayerTopic[] goodbyeTopics;
    private readonly PluginVoices voices;
    private readonly VoiceSilencer voiceSilencer;
    private readonly TopicHider topicHider;
    private readonly IGameSession session;
    private readonly GameTarget gameTarget;
    private readonly GameEvent<ConversationEndEventArgs> onConversationEnd;

    private readonly List<(PlayerTopic PlayerTopic, DialogueTopic Topic)> shownTopics = [];

    // The text is part of the key, since the player can choose from a menu that the latest change of a created topic's text has not reached.
    private readonly Dictionary<(IPlayerTopic PlayerTopic, string Text), DialogueTopic> choices = [];

    private readonly List<DialogueTopic> chosenTopics = [];

    private FormId<Actor>? speakerId;

    private FormId<Actor>? hiddenSpeakerId;

    private Func<Actor?>? getSpeaker;
    private Func<Actor, bool>? speakerFilter;

    private FormId<Actor>? crosshairActorId;

    // Recorded as the conversation opens, since the speaker can change before it closes.
    private FormId<Actor>? conversationSpeakerId;
    private bool shownInConversation;
    private bool goodbyeChosen;

    private (FormId<Actor> SpeakerId, DialogueTopic Topic)? resumePoint;

    internal DialogueBranch(ISavedStateStringSet saidTexts, DialogueTopic startingTopic, Func<Actor, IEnumerable<TopicResponse>>? computeGreeting, Func<Actor, IEnumerable<TopicResponse>>? computeGoodbye, bool resumes, PlayerTopic[] otherTopics, PlayerTopic[] goodbyeTopics, PluginVoices voices, VoiceSilencer voiceSilencer, TopicHider topicHider, IGameEvents events, IGameSession session, GameTarget gameTarget, ILog log)
    {
        this.saidTexts = saidTexts;
        this.startingTopic = startingTopic;
        this.computeGreeting = computeGreeting;
        this.computeGoodbye = computeGoodbye;
        this.resumes = resumes;
        this.otherTopics = otherTopics;
        this.goodbyeTopics = goodbyeTopics;
        this.voices = voices;
        this.voiceSilencer = voiceSilencer;
        this.topicHider = topicHider;
        this.session = session;
        this.gameTarget = gameTarget;
        onConversationEnd = new GameEvent<ConversationEndEventArgs>(ex => log.Write($"A subscriber to a dialogue branch's OnConversationEnd threw: {ex}"));
        foreach (PlayerTopic topic in otherTopics.Concat(goodbyeTopics))
        {
            topic.OnSelected.Subscribe(ChooseAsync);
        }
        events.OnFrame.Subscribe(_ => Update());
        events.OnCrosshairReference.Subscribe(OnCrosshairReference);
        // Frames run during a load, and a speaker found then can be a reference the load replaces.
        events.OnNewGameOrSaveLoading.Subscribe(_ => Reset());
        events.OnSaveLoaded.Subscribe(_ => Reset());
    }

    public IReadOnlyList<DialogueTopic> ChosenTopics => chosenTopics;

    public IGameEvent<ConversationEndEventArgs> OnConversationEnd => onConversationEnd;

    public void Show<TSpeaker>(TSpeaker speaker) where TSpeaker : IActor
    {
        getSpeaker = null;
        speakerFilter = null;
        SetSpeaker(speaker.GetFormId<Actor>());
    }

    public void Show(Func<Actor?> getSpeaker)
    {
        ArgumentNullException.ThrowIfNull(getSpeaker);
        this.getSpeaker = getSpeaker;
        speakerFilter = null;
    }

    public void Show(QuestReferenceAlias speaker, Func<bool>? condition = null)
    {
        ArgumentNullException.ThrowIfNull(speaker);
        Show(() => condition?.Invoke() != false ? speaker.ReferenceId?.FormOrNull?.AsActor() : null);
    }

    public void Show(Func<Actor, bool> speakerFilter)
    {
        ArgumentNullException.ThrowIfNull(speakerFilter);
        this.speakerFilter = speakerFilter;
        getSpeaker = null;
        if (!IsConversing)
        {
            SetSpeaker(crosshairActorId?.FormOrNull is { } crosshairActor && speakerFilter(crosshairActor) ? crosshairActor.FormId : null);
        }
    }

    public void ForgetSaidTopics()
    {
        foreach (string saidKey in saidTexts.ToArray())
        {
            saidTexts.Remove(saidKey);
        }
        foreach ((PlayerTopic playerTopic, _) in shownTopics)
        {
            playerTopic.IsSaid = false;
        }
    }

    public void Hide()
    {
        getSpeaker = null;
        speakerFilter = null;
        SetSpeaker(null);
    }

    private bool IsConversing => speakerId != null && gameTarget.MenuTopicManager.IsConversationOpenWith(speakerId.Value);

    private void SetSpeaker(FormId<Actor>? id)
    {
        if (speakerId == id)
        {
            return;
        }
        if (speakerId != null)
        {
            voiceSilencer.RemoveGreeting(speakerId.Value, this);
            voiceSilencer.RemoveGoodbye(speakerId.Value, this);
        }
        speakerId = id;
        ShowTopics(speakerId?.FormOrNull is { } speakerActor ? Open(speakerActor).Topics : []);
    }

    private void OnCrosshairReference(CrosshairReferenceEventArgs args)
    {
        Actor? actor = args.CrosshairReference?.AsActor();
        crosshairActorId = actor?.FormId;
        if (actor != null)
        {
            SetSpeakerIfAccepted(actor.Value);
        }
    }

    private void SetSpeakerIfAccepted(Actor actor)
    {
        if (speakerFilter != null && !IsConversing && speakerFilter(actor))
        {
            SetSpeaker(actor.FormId);
        }
    }

    private void Update()
    {
        bool ended = conversationSpeakerId != null && !gameTarget.MenuTopicManager.IsConversationOpenWith(conversationSpeakerId.Value);
        if (ended)
        {
            EndConversation(goodbyeChosen ? ConversationEndReason.Goodbye : ConversationEndReason.Closed);
        }
        bool conversing = IsConversing;
        if (!conversing && getSpeaker != null)
        {
            SetSpeaker(getSpeaker()?.FormId);
        }
        if (speakerId is not { } currentSpeakerId)
        {
            return;
        }
        Actor? actor = currentSpeakerId.FormOrNull;
        if (conversing)
        {
            if (conversationSpeakerId == null && actor != null)
            {
                conversationSpeakerId = currentSpeakerId;
                shownInConversation = shownTopics.Count > 0;
                goodbyeChosen = false;
                chosenTopics.Clear();
                foreach ((PlayerTopic playerTopic, DialogueTopic topic) in shownTopics)
                {
                    playerTopic.SetReply(CreateReply(topic, actor.Value));
                }
            }
            // Recomputed on each frame of the conversation, so the goodbye can depend on a topic chosen in it.
            SetGoodbye(currentSpeakerId, shownInConversation ? actor : null);
            return;
        }
        if (speakerFilter != null && crosshairActorId != currentSpeakerId && !ended)
        {
            return;
        }
        if (speakerFilter != null && actor != null && !speakerFilter(actor.Value))
        {
            SetSpeaker(null);
            return;
        }
        (IReadOnlyList<DialogueTopic> topics, bool resumed) = actor != null ? Open(actor.Value) : ([], false);
        if (!IsShown(topics, actor))
        {
            ShowTopics(topics);
        }
        if (computeGreeting != null)
        {
            if (shownTopics.Count > 0 && actor != null)
            {
                voiceSilencer.SetGreeting(currentSpeakerId, this, resumed ? [] : [.. computeGreeting(actor.Value).Select(voices.ToReplyResponse)]);
            }
            else
            {
                voiceSilencer.RemoveGreeting(currentSpeakerId, this);
            }
        }
        SetGoodbye(currentSpeakerId, shownTopics.Count > 0 ? actor : null);
    }

    private void SetGoodbye(FormId<Actor> speakerId, Actor? actor)
    {
        if (computeGoodbye == null)
        {
            return;
        }
        if (actor != null)
        {
            voiceSilencer.SetGoodbye(speakerId, this, [.. computeGoodbye(actor.Value).Select(voices.ToReplyResponse)]);
        }
        else
        {
            voiceSilencer.RemoveGoodbye(speakerId, this);
        }
    }

    private (IReadOnlyList<DialogueTopic> Topics, bool Resumed) Open(Actor actor) =>
        DialogueBranchRules.Open(startingTopic, resumePoint is { } point && point.SpeakerId == actor.FormId ? point.Topic : null, actor, topic => IsSaid(topic, actor));

    private bool IsShown(IReadOnlyList<DialogueTopic> topics, Actor? actor) =>
        topics.Count == shownTopics.Count && topics.Zip(shownTopics).All(pair => pair.First == pair.Second.Topic && actor != null && pair.First.GetText(actor.Value) == pair.Second.PlayerTopic.Text);

    private void ShowTopics(IReadOnlyList<DialogueTopic> topics)
    {
        shownTopics.Clear();
        if (speakerId?.FormOrNull is { } speakerActor)
        {
            int otherCount = 0;
            int goodbyeCount = 0;
            foreach (DialogueTopic topic in topics)
            {
                PlayerTopic[] playerTopics = topic.IsGoodbye ? goodbyeTopics : otherTopics;
                int i = topic.IsGoodbye ? goodbyeCount++ : otherCount++;
                string text = topic.GetText(speakerActor);
                playerTopics[i].Text = text;
                playerTopics[i].SetReply(CreateReply(topic, speakerActor));
                playerTopics[i].IsSaid = saidTexts.Contains(GetSaidKey(speakerActor, text));
                playerTopics[i].Show(speakerActor);
                shownTopics.Add((playerTopics[i], topic));
                choices[(playerTopics[i], text)] = topic;
            }
        }
        foreach (PlayerTopic unusedTopic in otherTopics.Concat(goodbyeTopics).Where(topic => !shownTopics.Any(entry => entry.PlayerTopic == topic)))
        {
            unusedTopic.Hide();
        }
        if (conversationSpeakerId != null && shownTopics.Count > 0)
        {
            shownInConversation = true;
        }
        UpdateHiding();
    }

    private void UpdateHiding()
    {
        FormId<Actor>? speakerToHide = shownTopics.Any(entry => entry.Topic.HidesOtherTopics) ? speakerId : null;
        if (speakerToHide == hiddenSpeakerId)
        {
            return;
        }
        if (speakerToHide?.FormOrNull is { } speaker)
        {
            topicHider.Hide(this, speaker, [.. otherTopics.Concat(goodbyeTopics)]);
            hiddenSpeakerId = speakerToHide;
        }
        else
        {
            topicHider.Release(this);
            hiddenSpeakerId = null;
        }
    }

    private Reply CreateReply(DialogueTopic topic, Actor speaker) => new([.. topic.GetResponses(speaker).Select(voices.ToReplyResponse)], topic.IsGoodbye);

    private async Task ChooseAsync(PlayerTopicSelectedEventArgs args)
    {
        if (!choices.TryGetValue((args.Topic, args.Text), out DialogueTopic? chosenTopic))
        {
            return;
        }
        Actor actor = speakerId?.FormOrNull ?? throw new InvalidOperationException($"The speaker of the dialogue topic \"{args.Text}\" no longer exists.");
        goodbyeChosen = chosenTopic.IsGoodbye;
        chosenTopics.Add(chosenTopic);
        resumePoint = resumes && !chosenTopic.IsGoodbye ? (actor.FormId, chosenTopic) : null;
        var context = new DialogueTopicContext(chosenTopic, actor, session.NewGameOrSaveLoading);
        // A topic is recorded by the text it showed, since a computed text can change between showing it and choosing it.
        (IReadOnlyList<DialogueTopic> next, Task beginTask) = DialogueBranchRules.Choose(chosenTopic, startingTopic, context, topic => IsSaid(topic, actor), _ => saidTexts.Add(GetSaidKey(actor, args.Text)));
        ShowTopics(next);
        await beginTask;
        if (chosenTopic.EndCallback != null)
        {
            await args.ReplyEndTask;
            await chosenTopic.EndCallback(context);
        }
    }

    private void EndConversation(ConversationEndReason reason)
    {
        FormId<Actor>? endedSpeakerId = conversationSpeakerId;
        conversationSpeakerId = null;
        choices.Clear();
        foreach ((PlayerTopic playerTopic, DialogueTopic topic) in shownTopics)
        {
            choices[(playerTopic, playerTopic.Text)] = topic;
        }
        if (shownInConversation)
        {
            shownInConversation = false;
            onConversationEnd.Raise(new ConversationEndEventArgs(endedSpeakerId?.FormOrNull, reason));
        }
    }

    // The voice silencer clears the greeting as a save starts loading. One set during the load stores a form ID the loaded game can assign to another actor.
    private void Reset()
    {
        if (conversationSpeakerId != null)
        {
            EndConversation(ConversationEndReason.NewGameOrSaveLoading);
        }
        if (speakerId != null)
        {
            voiceSilencer.RemoveGreeting(speakerId.Value, this);
            voiceSilencer.RemoveGoodbye(speakerId.Value, this);
        }
        speakerId = null;
        crosshairActorId = null;
        resumePoint = null;
        chosenTopics.Clear();
        shownTopics.Clear();
        choices.Clear();
        UpdateHiding();
    }

    private bool IsSaid(DialogueTopic topic, Actor speaker) => saidTexts.Contains(GetSaidKey(speaker, topic.GetText(speaker)));

    private static string GetSaidKey(Actor speaker, string text) => $"{speaker.FormId.Value:X8} {text}";
}
