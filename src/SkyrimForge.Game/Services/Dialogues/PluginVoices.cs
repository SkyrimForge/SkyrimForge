using System;
using System.Collections.Generic;
using System.IO;

namespace SkyrimForge.Game;

internal sealed class PluginVoices(string pluginAssemblyName)
{
    private readonly string directory = Path.Combine(TopicResponse.VoiceDirectory, pluginAssemblyName);

    // The game reads only the files present when it starts.
    private readonly HashSet<string> foundVoicePaths = new(StringComparer.OrdinalIgnoreCase);

    internal ReplyResponse ToReplyResponse(TopicResponse response)
    {
        ResponseEmotion emotion = new(response.Emotion, response.EmotionValue, response.UsesEmotionAnimation);
        if (response.VoiceRelativePath == null)
        {
            return new ReplyResponse(response.Text, null, emotion, response.SpeakerIdleId, response.ListenerIdleId, response.SilenceDuration);
        }
        string voiceDirectoryPath = Path.Combine(directory, response.VoiceRelativePath);
        if (!foundVoicePaths.Contains(voiceDirectoryPath))
        {
            string fullPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, voiceDirectoryPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"The voice file {fullPath} does not exist.", fullPath);
            }
            foundVoicePaths.Add(voiceDirectoryPath);
        }
        return new ReplyResponse(response.Text, voiceDirectoryPath, emotion, response.SpeakerIdleId, response.ListenerIdleId);
    }
}
