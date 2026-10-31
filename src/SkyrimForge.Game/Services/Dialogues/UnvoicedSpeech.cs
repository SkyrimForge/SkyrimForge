using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class UnvoicedSpeech(GameTarget gameTarget, IUtility utility, IGameSession session, MouthAnimator mouthAnimator, SubtitleSwitch subtitleSwitch, ILog log)
    : IUnvoicedSpeech
{
    private static readonly TimeSpan
        timePerCharacter = TimeSpan.FromSeconds(0.045),
        shortestLineDuration = TimeSpan.FromSeconds(2),
        timeBetweenLines = TimeSpan.FromSeconds(0.3);

    private readonly GameTarget gameTarget = gameTarget;
    private readonly IUtility utility = utility;
    private readonly IGameSession session = session;
    private readonly MouthAnimator mouthAnimator = mouthAnimator;
    private readonly SubtitleSwitch subtitleSwitch = subtitleSwitch;
    private readonly ILog log = log;

    internal static TimeSpan LongestLine => TopicResponse.LongestSilentVoice;

    internal static TimeSpan GetLineDuration(string line)
    {
        TimeSpan duration = timePerCharacter * line.Length;
        return duration < shortestLineDuration ? shortestLineDuration : duration > LongestLine ? LongestLine : duration;
    }

    // Static, so one plugin's lines interrupt another plugin's lines for the same speaker.
    private static readonly Lock interruptionSourcesLock = new();
    private static readonly Dictionary<nint, CancellationTokenSource> interruptionSourcesBySpeaker = [];

    public Task SayAsync<TSpeaker>(TSpeaker speaker, params IEnumerable<string> lines) where TSpeaker : IActor
    {
        ArgumentNullException.ThrowIfNull(lines);
        return Core();

        async Task Core()
        {
            string[] spokenLines = [.. lines];
            if (spokenLines.Length == 0)
            {
                Stop(speaker);
                return;
            }
            nint speakerAddress = speaker.Address;
            // Read now, since NewGameOrSaveLoading returns the next game's cancellation token once a load starts.
            CancellationToken newGameOrSaveLoadingToken = session.NewGameOrSaveLoading;
            var interruptionSource = CancellationTokenSource.CreateLinkedTokenSource(newGameOrSaveLoadingToken);
            lock (interruptionSourcesLock)
            {
                if (interruptionSourcesBySpeaker.Remove(speakerAddress, out CancellationTokenSource? replacedSource))
                {
                    replacedSource.Cancel();
                }
                interruptionSourcesBySpeaker[speakerAddress] = interruptionSource;
            }
            try
            {
                await SayAllAsync(speaker, spokenLines, interruptionSource.Token, newGameOrSaveLoadingToken);
            }
            finally
            {
                Release(speakerAddress, interruptionSource);
            }
        }
    }

    public void Stop<TSpeaker>(TSpeaker speaker) where TSpeaker : IActor
    {
        nint speakerAddress = speaker.Address;
        lock (interruptionSourcesLock)
        {
            if (interruptionSourcesBySpeaker.Remove(speakerAddress, out CancellationTokenSource? interruptionSource))
            {
                interruptionSource.Cancel();
            }
        }
    }

    // Disposed under the lock, so SayAsync and Stop never cancel a disposed source.
    private static void Release(nint speakerAddress, CancellationTokenSource interruptionSource)
    {
        lock (interruptionSourcesLock)
        {
            if (interruptionSourcesBySpeaker.TryGetValue(speakerAddress, out CancellationTokenSource? currentSource) && currentSource == interruptionSource)
            {
                interruptionSourcesBySpeaker.Remove(speakerAddress);
            }
            interruptionSource.Dispose();
        }
    }

    // The speaker is looked up after each wait, since the game can free the reference.
    private async Task SayAllAsync<TSpeaker>(TSpeaker speaker, string[] lines, CancellationToken interruptionToken, CancellationToken newGameOrSaveLoadingToken) where TSpeaker : IActor
    {
        FormId<Actor> speakerId = speaker.GetFormId<Actor>();
        bool waiting = false;
        if (!speaker.IsPlayerRef() && speaker.Is3DLoaded() && !gameTarget.MenuTopicManager.IsConversationOpenWith(speakerId))
        {
            speaker.PutCreatedPackage(KnownForms.zzzDEFAULTzzzWaitForDialogue.Form, true, false, true);
            waiting = true;
        }
        subtitleSwitch.FindSettings();
        subtitleSwitch.BeginLine(lines);
        try
        {
            foreach ((int i, string line) in lines.Index())
            {
                if (i > 0)
                {
                    await utility.WaitAsync(timeBetweenLines, interruptionToken);
                }
                await SayLineAsync(speakerId, line, interruptionToken, newGameOrSaveLoadingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.Write($"Unvoiced lines threw: {ex}");
            throw;
        }
        finally
        {
            subtitleSwitch.EndLine(lines);
            if (waiting && !newGameOrSaveLoadingToken.IsCancellationRequested && IsCurrentInterruption(speaker.Address, interruptionToken))
            {
                speakerId.FormOrNull?.EvaluatePackage(true, false);
            }
        }
    }

    // Read under the lock, since a source is disposed only after it leaves the table.
    private static bool IsCurrentInterruption(nint speakerAddress, CancellationToken interruptionToken)
    {
        lock (interruptionSourcesLock)
        {
            return !interruptionSourcesBySpeaker.TryGetValue(speakerAddress, out CancellationTokenSource? currentSource) || currentSource.Token == interruptionToken;
        }
    }

    private async Task SayLineAsync(FormId<Actor> speakerId, string line, CancellationToken interruptionToken, CancellationToken newGameOrSaveLoadingToken)
    {
        interruptionToken.ThrowIfCancellationRequested();
        if (speakerId.FormOrNull is not { } speaker)
        {
            return;
        }
        SubtitleManager subtitles = gameTarget.SubtitleManager;
        nint speakerAddress = speaker.Address;
        subtitles.AddSubtitle(speaker, line);
        // RemoveSubtitle compares only the address, so it is safe after the reference is freed.
        using (interruptionToken.Register(() => subtitles.RemoveSubtitle(speaker, line, gameTarget.MenuTopicManager)))
        {
            Task lineWait = utility.WaitAsync(GetLineDuration(line), interruptionToken);
            Task mouthAnimation = mouthAnimator.AnimateAsync(speakerId, lineWait, newGameOrSaveLoadingToken);
            while (!mouthAnimation.IsCompleted)
            {
                await Task.WhenAny(mouthAnimation, utility.WaitAsync(TimeSpan.Zero, interruptionToken));
                interruptionToken.ThrowIfCancellationRequested();
                // The game removes all of a speaker's subtitles when one of the speaker's own lines ends, so a removed line is added again.
                if (!mouthAnimation.IsCompleted && !subtitles.Subtitles.Get().Any(entry => entry.Speaker?.Address == speakerAddress && entry.Subtitle == line)
                    && speakerId.FormOrNull is { } reloadedSpeaker)
                {
                    // A reloaded reference has a new address.
                    speaker = reloadedSpeaker;
                    speakerAddress = speaker.Address;
                    subtitles.AddSubtitle(speaker, line);
                }
            }
            await mouthAnimation;
            // Throws if the line was interrupted, which ends the lines after it.
            await lineWait;
        }
        subtitles.RemoveSubtitle(speaker, line, gameTarget.MenuTopicManager);
    }
}
