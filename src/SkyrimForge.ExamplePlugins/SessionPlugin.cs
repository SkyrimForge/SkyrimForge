namespace SkyrimForge.ExamplePlugins;

/// <summary>Logs the SKSE messages about loading and saving.</summary>
/// <remarks>
/// OnDataLoaded arrives once, after Start has run. OnSaveLoading arrives while the previous world is still loaded.
/// OnSaveWriting's file name has no extension, unlike OnSaveLoading's.
/// </remarks>
public sealed class SessionPlugin(IGameEvents events, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        events.OnPostLoad.Subscribe(_ => log.Write("OnPostLoad."));
        events.OnPostPostLoad.Subscribe(_ => log.Write("OnPostPostLoad."));
        events.OnInputLoaded.Subscribe(_ => log.Write("OnInputLoaded."));
        events.OnDataLoaded.Subscribe(_ => log.Write("OnDataLoaded."));
        events.OnNewGame.Subscribe(_ => log.Write("OnNewGame."));
        events.OnSaveLoading.Subscribe(args => log.Write($"OnSaveLoading: {Describe(args.FileName)}."));
        events.OnSaveLoaded.Subscribe(_ => log.Write("OnSaveLoaded."));
        events.OnSaveWriting.Subscribe(args => log.Write($"OnSaveWriting: {Describe(args.FileName)}."));
        events.OnSaveDeleted.Subscribe(args => log.Write($"OnSaveDeleted: {Describe(args.FileName)}."));
        log.Write("Logging SKSE session messages.");
    }

    private static string Describe(string? save) => save != null ? $"\"{save}\"" : "no file name";
}
