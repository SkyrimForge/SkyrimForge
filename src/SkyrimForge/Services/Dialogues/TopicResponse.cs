using SkyrimForge.Game;
using System;

namespace SkyrimForge;

/// <summary>One response a speaker says, shown as a subtitle, in reply to a <see cref="DialogueTopic"/> the player chooses or in a <see cref="SceneLine"/>.</summary>
public sealed record TopicResponse
{
    /// <summary>
    /// The directory, relative to the game's executable, of SkyrimForge's silent voices, with each plugin's voices in a subdirectory named for its assembly.
    /// </summary>
    public const string VoiceDirectory = @"Data\Sound\Voice\SkyrimForge";

    /// <summary>The shortest duration <see cref="Silence"/> and <see cref="Idle"/> accept.</summary>
    public static TimeSpan MinimumSilence { get; } = TimeSpan.FromSeconds(1);

    internal static TimeSpan LongestSilentVoice { get; } = TimeSpan.FromSeconds(12);

    internal static int GetSilentResponseCount(TimeSpan silenceDuration) => (int)Math.Ceiling(silenceDuration / LongestSilentVoice);

    /// <summary>Creates a response.</summary>
    /// <param name="text">The subtitle.</param>
    /// <param name="voiceRelativePath">
    /// The .fuz voice file, relative to the plugin's subdirectory of <see cref="VoiceDirectory"/>.
    /// Null creates an unvoiced response, shown for about 0.045 seconds per character, from about 3 to 13 total seconds.
    /// A missing file throws <see cref="System.IO.FileNotFoundException"/> from <see cref="IDialogueBranchFactory.Create"/> for a topic's fixed responses,
    /// from <see cref="IRuntimeQuestFactory.Create"/> for a scene line's fixed responses, and when the menu shows the topic for a topic's computed ones.
    /// A scene line's computed response that specifies a missing file is logged, and the line says nothing.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="voiceRelativePath"/> does not end in .fuz.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
    public TopicResponse(string text, string? voiceRelativePath = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (voiceRelativePath != null && !voiceRelativePath.EndsWith(".fuz", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The voice file \"{voiceRelativePath}\" does not end in .fuz. The game ends a voice on the player's click only where it reads a .fuz.", nameof(voiceRelativePath));
        }
        Text = text;
        VoiceRelativePath = voiceRelativePath;
    }

    /// <summary>Creates a response with no subtitle and a silent voice lasting about <paramref name="duration"/>, during which the speaker's mouth does not move.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is less than <see cref="MinimumSilence"/>.</exception>
    public static TopicResponse Silence(TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, MinimumSilence);
        return new TopicResponse("") { SilenceDuration = duration };
    }

    /// <summary>Creates a silent response lasting about <paramref name="duration"/>, during which the speaker plays <paramref name="idleId"/>.</summary>
    /// <remarks>In a line that also has an unvoiced response with text, the speaker's mouth does not move while that text is shown.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is less than <see cref="MinimumSilence"/>.</exception>
    public static TopicResponse Idle(FormId<Idle> idleId, TimeSpan duration) => Silence(duration) with { SpeakerIdleId = idleId };

    /// <summary>The subtitle.</summary>
    public string Text { get; }

    internal TimeSpan? SilenceDuration { get; private init; }

    /// <summary>The .fuz voice file. It is null for an unvoiced response.</summary>
    public string? VoiceRelativePath { get; }

    /// <summary>The emotion the speaker's face shows during the response, neutral by default.</summary>
    public EmotionType Emotion { get; init; }

    /// <summary>The strength of <see cref="Emotion"/>, from 0 to 100, 50 by default.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is greater than 100.</exception>
    public uint EmotionValue
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100u);
            field = value;
        }
    } = 50;

    /// <summary>The response's Use Emotion Animation flag, as the Creation Kit labels it.</summary>
    public bool UsesEmotionAnimation { get; init; }

    /// <summary>The idle the speaker plays as the response begins.</summary>
    public FormId<Idle>? SpeakerIdleId { get; init; }

    /// <summary>The response's Listener Idle Animation, as the Creation Kit labels it.</summary>
    public FormId<Idle>? ListenerIdleId { get; init; }

    /// <summary>Converts <paramref name="text"/> to an unvoiced response.</summary>
    public static implicit operator TopicResponse(string text) => new(text);
}
