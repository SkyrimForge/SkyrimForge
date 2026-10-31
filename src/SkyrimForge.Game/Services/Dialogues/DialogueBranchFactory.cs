using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class DialogueBranchFactory(PlayerTopicFactory topicFactory, VoiceSilencer voiceSilencer, TopicHider topicHider, IGameEvents events, IGameSession session, GameTarget gameTarget, IPerPluginSavedState savedState, ILog log, PluginVoices voices) : IDialogueBranchFactory
{
    private const string keyPrefix = "SkyrimForge.DialogueBranch.";

    private readonly PlayerTopicFactory topicFactory = topicFactory;
    private readonly VoiceSilencer voiceSilencer = voiceSilencer;
    private readonly TopicHider topicHider = topicHider;
    private readonly IGameEvents events = events;
    private readonly IGameSession session = session;
    private readonly GameTarget gameTarget = gameTarget;
    private readonly IPerPluginSavedState savedState = savedState;
    private readonly ILog log = log;
    private readonly PluginVoices voices = voices;
    private readonly HashSet<string> keys = new(StringComparer.Ordinal);

    public IDialogueBranch Create(string key, DialogueTopic startingTopic, Func<Actor, IEnumerable<TopicResponse>>? greeting = null, Func<Actor, IEnumerable<TopicResponse>>? goodbye = null, bool resumes = false, FormId<Quest>? questId = null)
    {
        // The Creation Kit's default topic priority is 50. Goodbye topics are given priorities below it and other topics priorities above it.
        const int maxShownTopics = 50;
        const byte topPriority = 100;
        const byte topGoodbyePriority = 49;
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(startingTopic);
        (int otherCount, int goodbyeCount) = DialogueBranchRules.CountMaxShownTopics(startingTopic);
        if (otherCount > maxShownTopics || goodbyeCount > maxShownTopics)
        {
            throw new ArgumentException($"The dialogue branch \"{key}\" shows {otherCount} topics and {goodbyeCount} goodbye topics at once. The limit for each is {maxShownTopics}.",
                nameof(startingTopic));
        }
        // Fixed voice files are resolved now, so a missing one throws as the plugin starts.
        IReadOnlyList<DialogueTopic> reachableTopics = DialogueBranchRules.GetReachableTopics(startingTopic);
        foreach (TopicResponse response in reachableTopics.SelectMany(topic => topic.FixedResponses))
        {
            _ = voices.ToReplyResponse(response);
        }
        if (!keys.Add(key))
        {
            throw new ArgumentException($"This plugin already created a dialogue branch with the key \"{key}\".", nameof(key));
        }
        foreach (DialogueTopic topic in reachableTopics)
        {
            topic.MakeChoicesReadOnly();
        }
        PlayerTopic[] otherTopics = [.. Enumerable.Range(0, otherCount).Select(i => topicFactory.Create((byte)(topPriority - i), questId))];
        PlayerTopic[] goodbyeTopics = [.. Enumerable.Range(0, goodbyeCount).Select(i => topicFactory.Create((byte)(topGoodbyePriority - i), questId))];
        var saidStringSet = savedState.GetStringSet($"{keyPrefix}{key}.said");
        return new DialogueBranch(saidStringSet, startingTopic, greeting, goodbye, resumes, otherTopics, goodbyeTopics, voices, voiceSilencer, topicHider, events, session, gameTarget, log);
    }
}
