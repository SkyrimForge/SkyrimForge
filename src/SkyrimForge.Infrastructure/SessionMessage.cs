namespace SkyrimForge.Infrastructure;

// The values match the enum of the same name in Lifecycle.cpp.
internal enum SessionMessage
{
    PostLoad = 1,
    PostPostLoad = 2,
    InputLoaded = 3,
    DataLoaded = 4,
    NewGame = 5,
    PreLoadGame = 6,
    PostLoadGame = 7,
    SaveGame = 8,
    DeleteGame = 9
}
