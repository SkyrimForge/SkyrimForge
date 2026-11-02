using SkyrimForge.Quests;

namespace SkyrimForge.Game;

internal sealed record SceneRecordTravel(Package Package, ScenePackage Action, GameList<Package> Packages);
