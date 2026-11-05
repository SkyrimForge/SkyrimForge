using SkyrimForge.Papyrus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

internal sealed class MouthAnimator(IUtility utility)
{
    private const int bigAah = 1;
    private static readonly TimeSpan openTime = TimeSpan.FromSeconds(0.14), nearlyClosedTime = TimeSpan.FromSeconds(0.11);

    internal async Task AnimateAsync(FormId<Actor> speakerId, Task stopTask, CancellationToken newGameOrSaveLoadingToken)
    {
        const float openWeight = 0.6f, nearlyClosedWeight = 0.1f;
        try
        {
            bool usePapyrus = true;
            for (bool open = true; !stopTask.IsCompleted && speakerId.FormOrNull is { } speaker; open = !open)
            {
                float weight = open ? openWeight : nearlyClosedWeight;
                if (usePapyrus)
                {
                    usePapyrus = await SetPhonemeAsync(speaker, weight);
                }
                if (!usePapyrus && speaker.GetFaceGenAnimationData() is { } animationData)
                {
                    BSFaceGenKeyframeMultiple phonemes = animationData.PhenomeKeyFrame;
                    phonemes.SetValue(bigAah, weight);
                    phonemes.IsUpdated = true;
                }
                await Task.WhenAny(stopTask, utility.WaitAsync(open ? openTime : nearlyClosedTime));
            }
        }
        finally
        {
            // A phoneme weight left in the phoneme keyframe overrides the game's lip sync for every later line.
            // BSFaceGenAnimationData.Reset does not clear the phoneme keyframe, but BSFaceGenKeyframeMultiple.Reset does.
            if (!newGameOrSaveLoadingToken.IsCancellationRequested && speakerId.FormOrNull?.GetFaceGenAnimationData() is { } animationData)
            {
                BSFaceGenKeyframeMultiple phonemes = animationData.PhenomeKeyFrame;
                phonemes.Reset();
                phonemes.IsUpdated = true;
            }
        }
    }

    private static async Task<bool> SetPhonemeAsync(Actor speaker, float weight)
    {
        try
        {
            await speaker.Papyrus.SetExpressionPhoneme(bigAah, weight);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
