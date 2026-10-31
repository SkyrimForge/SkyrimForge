namespace SkyrimForge.Game;

internal readonly record struct ResponseEmotion(EmotionType Type, uint Value, bool Animated)
{
    internal static ResponseEmotion Default { get; } = new(EmotionType.kNeutral, 50, false);
}
