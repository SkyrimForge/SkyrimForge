using System;

namespace SkyrimForge.Game;

internal readonly record struct ReplyResponse(string Text, string? VoicePath, ResponseEmotion Emotion, FormId<Idle>? SpeakerIdleId = null, FormId<Idle>? ListenerIdleId = null, TimeSpan? Silence = null);
