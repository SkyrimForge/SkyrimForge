namespace SkyrimForge.Infrastructure;

// The values are SKSE's MessagingInterface message types.
internal enum SessionMessage
{
    PostLoad = 0,
    PostPostLoad = 1,
    PreLoadGame = 2,
    PostLoadGame = 3,
    SaveGame = 4,
    DeleteGame = 5,
    InputLoaded = 6,
    NewGame = 7,
    DataLoaded = 8
}
