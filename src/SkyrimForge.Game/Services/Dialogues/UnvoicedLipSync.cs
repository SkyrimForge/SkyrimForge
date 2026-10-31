using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class UnvoicedLipSync(MouthAnimator mouthAnimator, IUtility utility, IGameSession session, GameTarget gameTarget, IGameEvents events, ILog log)
{
    private readonly MouthAnimator mouthAnimator = mouthAnimator;
    private readonly IUtility utility = utility;
    private readonly IGameSession session = session;
    private readonly GameTarget gameTarget = gameTarget;
    private readonly IGameEvents events = events;

    // Read and written on the game's thread.
    private readonly Dictionary<FormId<Actor>, Line> linesBySpeaker = [];

    internal void Install()
    {
        // A topic event arrives on a worker thread.
        events.OnTopicInfo.Subscribe(async args =>
        {
            if (args.Type == TESTopicInfoEvent.TopicInfoEventType.kTopicEnd && args.TopicInfoFormID is { } topicInfoId)
            {
                FormId<TopicInfo> endedId = (FormId<TopicInfo>)topicInfoId;
                await utility.WaitForGameThreadAsync();
                StopWhere(line => line.InfoId == endedId);
            }
        });
        events.OnFrame.Subscribe(_ =>
        {
            if (linesBySpeaker.Count > 0 && gameTarget.MenuTopicManager is not { MenuOpen: true })
            {
                StopWhere(line => line.InConversation);
            }
        });
        events.OnNewGameOrSaveLoading.Subscribe(_ => StopWhere(_ => true));
    }

    internal void Begin(FormId<Actor> speakerId, FormId<TopicInfo> infoId, bool inConversation) => BeginAsync(speakerId, infoId, inConversation).LogFault(log);

    private async Task BeginAsync(FormId<Actor> speakerId, FormId<TopicInfo> infoId, bool inConversation)
    {
        await utility.WaitForGameThreadAsync();
        Stop(speakerId);
        TaskCompletionSource endSource = new();
        linesBySpeaker.Add(speakerId, new Line(infoId, inConversation, endSource));
        mouthAnimator.AnimateAsync(speakerId, endSource.Task, session.NewGameOrSaveLoading).LogFault(log);
    }

    private void StopWhere(Func<Line, bool> test)
    {
        foreach (FormId<Actor> speakerId in linesBySpeaker.Where(entry => test(entry.Value)).Select(entry => entry.Key).ToArray())
        {
            Stop(speakerId);
        }
    }

    private void Stop(FormId<Actor> speakerId)
    {
        if (linesBySpeaker.Remove(speakerId, out Line? line))
        {
            line.Ended.TrySetResult();
        }
    }

    private sealed record Line(FormId<TopicInfo> InfoId, bool InConversation, TaskCompletionSource Ended);
}
