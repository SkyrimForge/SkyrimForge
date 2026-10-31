namespace SkyrimForge.Game;

internal static class GameListUtility
{
    internal delegate int CopyEntries<TEntry>(nint self, ref TEntry first, int capacity) where TEntry : struct;

    internal static int Count<TEntry>(nint self, CopyEntries<TEntry> copyEntries) where TEntry : struct
    {
        TEntry defaultEntry = default;
        return copyEntries(self, ref defaultEntry, 0);
    }
}
