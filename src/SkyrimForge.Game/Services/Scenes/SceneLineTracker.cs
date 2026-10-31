using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyrimForge.Game;

internal sealed class SceneLineTracker
{
    private readonly GameTarget gameTarget;
    private readonly Func<QuestReferenceAlias, bool> isPlayer;
    private readonly GameEvent<QuestSceneLineEventArgs> onBegin;
    private readonly GameEvent<QuestSceneLineEventArgs> onEnd;

    private readonly Dictionary<uint, LineVariant> lineInfos = [];

    private readonly List<SceneRecordSayOnceInfo> sayOnceInfos = [];

    // A scene's entries are removed as it ends, because the game may not raise the end of a line the scene's end cut short.
    private readonly HashSet<uint> activeLineInfos = [];

    private readonly Dictionary<uint, IReadOnlyList<TopicResponse>> computedResponses = [];

    private (uint InfoId, int BeginCount, string? Subtitle)? playerLine;

    internal SceneLineTracker(GameTarget gameTarget, IGameEvents events, IUtility utility, ILog log, string questName, Func<QuestReferenceAlias, bool> isPlayer)
    {
        this.gameTarget = gameTarget;
        this.isPlayer = isPlayer;
        onBegin = new GameEvent<QuestSceneLineEventArgs>(ex => log.Write($"A subscriber to the scene lines of the quest \"{questName}\" threw: {ex}"));
        onEnd = new GameEvent<QuestSceneLineEventArgs>(ex => log.Write($"A subscriber to the scene lines of the quest \"{questName}\" threw: {ex}"));
        // For each info a scene's dialogue action says, the game raises a begin as each response begins, and one end. The event arrives on a worker thread.
        events.OnTopicInfo.Subscribe(async args =>
        {
            if (args.TopicInfoFormID is not { } topicInfoId)
            {
                return;
            }
            uint infoId = topicInfoId.Value;
            bool begins = args.Type == TESTopicInfoEvent.TopicInfoEventType.kTopicBegin;
            await utility.WaitForGameThreadAsync();
            // The game finishes saying a line whose scene has ended, so a line's events are raised only while its scene plays.
            if (lineInfos.TryGetValue(infoId, out LineVariant? saidLine) && saidLine.Scene.IsPlaying)
            {
                QuestSceneLineEventArgs line = new(saidLine.Scene, saidLine.Line, saidLine.VariantIndex);
                if (begins)
                {
                    if (isPlayer(saidLine.Line.Speaker))
                    {
                        ShowPlayerSubtitle(infoId, computedResponses.GetValueOrDefault(infoId) ?? saidLine.Line.Variants[saidLine.VariantIndex]);
                    }
                    if (activeLineInfos.Add(infoId))
                    {
                        onBegin.Raise(line);
                    }
                }
                else
                {
                    if (playerLine?.InfoId == infoId)
                    {
                        HidePlayerSubtitle();
                    }
                    activeLineInfos.Remove(infoId);
                    onEnd.Raise(line);
                }
            }
        });
    }

    internal IGameEvent<QuestSceneLineEventArgs> OnBegin => onBegin;

    internal IGameEvent<QuestSceneLineEventArgs> OnEnd => onEnd;

    internal void Add(QuestScene scene, SceneRecord sceneRecord)
    {
        sayOnceInfos.AddRange(sceneRecord.SayOnceInfos);
        foreach ((TopicInfo info, SceneLine line, int variantIndex) in sceneRecord.Lines)
        {
            lineInfos[info.FormId.Value] = new(scene, line, variantIndex);
        }
    }

    internal void Clear()
    {
        sayOnceInfos.Clear();
        lineInfos.Clear();
        computedResponses.Clear();
        activeLineInfos.Clear();
        playerLine = null;
    }

    internal void EndScene(QuestScene scene)
    {
        if (playerLine is { } line && lineInfos.TryGetValue(line.InfoId, out LineVariant? cutLine) && cutLine.Scene == scene)
        {
            HidePlayerSubtitle();
        }
        activeLineInfos.RemoveWhere(infoId => lineInfos.TryGetValue(infoId, out LineVariant? saidLine) && saidLine.Scene == scene);
    }

    internal void SetComputedResponses(TopicInfo info, IReadOnlyList<TopicResponse> responses) => computedResponses[info.FormId.Value] = responses;

    internal IEnumerable<string> GetSaidKeys() => sayOnceInfos.Where(sayOnceInfo => sayOnceInfo.Info.SaidOnce).Select(sayOnceInfo => sayOnceInfo.Key);

    internal void MarkSaid(IReadOnlyCollection<string> saidKeys) => SetSaid(sayOnceInfos.Where(sayOnceInfo => saidKeys.Contains(sayOnceInfo.Key)), true);

    internal void ClearSaid() => SetSaid(sayOnceInfos, false);

    private static void SetSaid(IEnumerable<SceneRecordSayOnceInfo> infos, bool said)
    {
        foreach ((TopicInfo info, _) in infos)
        {
            TopicInfo writtenInfo = info;
            writtenInfo.SaidOnce = said;
        }
    }

    // The game shows no subtitle for a line the player says. It raises a beginning for each response it says of the info, in order,
    // and says a silence longer than the longest silent voice as several responses.
    private void ShowPlayerSubtitle(uint infoId, IReadOnlyList<TopicResponse> responses)
    {
        int beginCount = playerLine is { } line && line.InfoId == infoId ? line.BeginCount : 0;
        HidePlayerSubtitle();
        string[] texts = [.. responses.SelectMany(response =>
            Enumerable.Repeat(response.Text, response.SilenceDuration is { } silenceDuration ? TopicResponse.GetSilentResponseCount(silenceDuration) : 1))];
        string? subtitle = beginCount < texts.Length && texts[beginCount].Length > 0 ? texts[beginCount] : null;
        if (subtitle != null)
        {
            gameTarget.SubtitleManager.AddSubtitle(gameTarget.Player, subtitle);
        }
        playerLine = (infoId, beginCount + 1, subtitle);
    }

    private void HidePlayerSubtitle()
    {
        if (playerLine?.Subtitle is { } subtitle)
        {
            gameTarget.SubtitleManager.RemoveSubtitle(gameTarget.Player, subtitle, gameTarget.MenuTopicManager);
        }
        playerLine = null;
    }

    private sealed record LineVariant(QuestScene Scene, SceneLine Line, int VariantIndex);
}
