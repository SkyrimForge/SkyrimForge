using SkyrimForge.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge;

internal static class DialogueBranchRules
{
    internal static IReadOnlyList<DialogueTopic> Start(DialogueTopic startingTopic, Actor speaker, Func<DialogueTopic, bool> isSaid) => GetShowableTopics([startingTopic], speaker, isSaid);

    internal static (IReadOnlyList<DialogueTopic> Topics, bool Resumed) Open(DialogueTopic startingTopic, DialogueTopic? resumedTopic, Actor speaker, Func<DialogueTopic, bool> isSaid) =>
        resumedTopic != null && GetShowableTopics(resumedTopic.Choices, speaker, isSaid) is { Count: > 0 } resumedChoices ? (resumedChoices, true) : (Start(startingTopic, speaker, isSaid), false);

    internal static (IReadOnlyList<DialogueTopic> Next, Task BeginTask) Choose(DialogueTopic chosenTopic, DialogueTopic startingTopic, DialogueTopicContext context, Func<DialogueTopic, bool> isSaid, Action<DialogueTopic> markSaid)
    {
        markSaid(chosenTopic);
        Task beginTask = chosenTopic.BeginCallback?.Invoke(context) ?? Task.CompletedTask;
        if (chosenTopic.IsGoodbye)
        {
            return ([], beginTask);
        }
        IReadOnlyList<DialogueTopic> next = GetShowableTopics(chosenTopic.Choices, context.Speaker, isSaid);
        return (next.Count > 0 ? next : Start(startingTopic, context.Speaker, isSaid), beginTask);
    }

    internal static IReadOnlyList<DialogueTopic> GetReachableTopics(DialogueTopic startingTopic)
    {
        var reachedTopics = new HashSet<DialogueTopic>();
        var pendingTopics = new Queue<DialogueTopic>([startingTopic]);
        var orderedTopics = new List<DialogueTopic>();
        while (pendingTopics.TryDequeue(out DialogueTopic? topic))
        {
            if (reachedTopics.Add(topic))
            {
                orderedTopics.Add(topic);
                foreach (DialogueTopic choice in topic.Choices)
                {
                    pendingTopics.Enqueue(choice);
                }
            }
        }
        return orderedTopics;
    }

    internal static (int OtherCount, int GoodbyeCount) CountMaxShownTopics(DialogueTopic startingTopic) =>
        GetReachableTopics(startingTopic)
        .Select(topic => topic.Choices.AsEnumerable())
        .Append([startingTopic])
        .Aggregate((OtherCount: 0, GoodbyeCount: 0), (maximum, listing) => (Math.Max(maximum.OtherCount, listing.Count(topic => !topic.IsGoodbye)), Math.Max(maximum.GoodbyeCount, listing.Count(topic => topic.IsGoodbye))));

    private static IReadOnlyList<DialogueTopic> GetShowableTopics(IEnumerable<DialogueTopic> topics, Actor speaker, Func<DialogueTopic, bool> isSaid) =>
        [.. topics.Where(topic => !(topic.IsSaidOnce && isSaid(topic)) && (topic.Condition?.Invoke(speaker) ?? true))];
}
