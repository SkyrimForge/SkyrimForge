using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkyrimForge.Game;

// The game ends a voice on the player's click only if it reads the voice from a .fuz.
internal static class SilentVoices
{
    private static readonly int shortestSeconds = (int)TopicResponse.MinimumSilence.TotalSeconds;
    private static readonly int longestSeconds = (int)TopicResponse.LongestSilentVoice.TotalSeconds;

    // The game ends a response 0.6 to 1 second after its voice ends.
    private static readonly TimeSpan responseTail = TimeSpan.FromSeconds(0.6);

    private static readonly TimeSpan briefSilence = TimeSpan.FromSeconds(0.1);

    internal const string BriefSilencePath = $@"{TopicResponse.VoiceDirectory}\silence_brief.fuz";

    internal static string GetPathForText(string text) => GetPathForSeconds((int)Math.Ceiling((UnvoicedSpeech.GetLineDuration(text) - responseTail).TotalSeconds));

    internal static string GetPathForDuration(TimeSpan duration) =>
        GetPathForSeconds(Math.Clamp((int)Math.Round((duration - responseTail).TotalSeconds), shortestSeconds, longestSeconds));

    // The game does not find a loose file written after it starts.
    internal static void Write()
    {
        string root = Path.GetDirectoryName(Environment.ProcessPath)!;
        Directory.CreateDirectory(Path.Combine(root, TopicResponse.VoiceDirectory));
        IEnumerable<(string Path, TimeSpan Duration)> files = Enumerable.Range(shortestSeconds, longestSeconds - shortestSeconds + 1)
            .Select(seconds => (GetPathForSeconds(seconds), TimeSpan.FromSeconds(seconds)))
            .Append((BriefSilencePath, briefSilence));
        foreach ((string relativePath, TimeSpan duration) in files)
        {
            string path = Path.Combine(root, relativePath);
            if (!File.Exists(path))
            {
                WriteFile(path, duration);
            }
        }
    }

    private static string GetPathForSeconds(int seconds) => $@"{TopicResponse.VoiceDirectory}\silence_{seconds}.fuz";

    private static void WriteFile(string path, TimeSpan duration)
    {
        const int sampleRate = 8000;
        int dataLength = (int)(sampleRate * duration.TotalSeconds) * 2;
        using var writer = new BinaryWriter(File.Create(path));
        // The .fuz header: its tag, version 1, and no lip data.
        writer.Write("FUZE"u8);
        writer.Write(1);
        writer.Write(0);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataLength);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataLength);
        writer.Write(new byte[dataLength]);
    }
}
