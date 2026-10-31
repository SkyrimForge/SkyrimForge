using SkyrimForge.Game;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Counts how many times this save has been loaded, and remembers the player's race.</summary>
/// <remarks>
/// The count is stored in the co-save, so each save has its own count. It is written to the file when the game next saves.
/// </remarks>
public sealed class SavedStatePlugin(GameTarget gameTarget, IPerPluginSavedState savedState, IGameEvents events, ILog log) : IPlugin
{
    private readonly ISavedStateFormIdPair<Race> racePair = savedState.GetFormIdPair<Race>("race");

    /// <inheritdoc/>
    public void Start()
    {
        events.OnSaveLoaded.Subscribe(Count);
    }

    private void Count(SaveLoadedEventArgs args)
    {
        ISavedStatePair<int> timesLoadedPair = savedState.GetPair<int>("timesLoaded");
        int timesLoaded = (timesLoadedPair.Value ?? 0) + 1;
        timesLoadedPair.Value = timesLoaded;

        string recalledText = DescribeStoredRace();
        StoreRace();
        log.Write($"This save has been loaded {timesLoaded} times. {recalledText}");
    }

    // The framework translates a stored form ID into the current load order.
    private string DescribeStoredRace()
    {
        if (racePair.Value is not { } raceId)
        {
            return "No stored race.";
        }
        return $"Stored race 0x{raceId}" +
            (raceId.FormOrNull is { } race ? $": \"{race.GetName()}.\"" : ", which is not loaded.");
    }

    private void StoreRace()
    {
        if (gameTarget.Player.GetRace() is { } race)
        {
            racePair.Value = race.FormId;
        }
    }
}
