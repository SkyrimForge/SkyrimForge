using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="BGSSoundOutput"/>.</summary>
public static class BGSSoundOutputExtensions
{
    extension(BGSSoundOutput soundOutput)
    {
        /// <summary>Gets the bias of each of the eight speakers for one channel of an input sound.</summary>
        /// <param name="inputChannelCount">The number of channels in the input sound, 1 for mono or 2 for stereo.</param>
        /// <param name="inputChannel">The channel of a stereo input, 0 for left or 1 for right. The game ignores it for a mono input.</param>
        /// <returns>
        /// Each speaker's stored percentage divided by 100, in the order L, R, C, Lfe, Rl, Rr, Bl, and Br of <see cref="BGSSoundOutput.SpeakerArrays.Channel"/>,
        /// or <c>null</c> if the form declares no speaker outputs or <paramref name="inputChannelCount"/> is above 2.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputChannelCount"/> is 2 and <paramref name="inputChannel"/> is above 1.</exception>
        public IReadOnlyList<float>? DoGetSpeakerBias(uint inputChannelCount, uint inputChannel)
        {
            const uint stereoChannelCount = 2;
            const uint speakerCount = 8;
            // The game indexes the speaker output rows by a stereo input's channel without checking it, so a channel above 1 reads past them.
            if (inputChannelCount == stereoChannelCount)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(inputChannel, stereoChannelCount);
            }
            float[] bias = new float[speakerCount];
            return NativeSupplements.SF_BGSSoundOutput_DoGetSpeakerBias(soundOutput.Address, inputChannelCount, inputChannel, speakerCount, ref bias[0]) ? bias : null;
        }
    }
}

internal static partial class NativeSupplements
{
    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_BGSSoundOutput_DoGetSpeakerBias(nint self, uint inputChannelCount, uint inputChannel, uint outputChannelCount, ref float bias);
}
