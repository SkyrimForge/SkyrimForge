using SkyrimForge.Game;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Counts each time the player opens a container and stores the count per container, with no Papyrus script and no record edit.</summary>
/// <remarks>
/// Attaching a script to every container means overriding every container record, and only the last override in the load order takes effect.
/// Testing the base object's form type overrides no record and also matches containers added by other .esp files.
/// </remarks>
public sealed class ObjectScriptPlugin(IGameEvents events, IPerPluginSavedState savedState, IDebug debug, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        log.Write("Counting each container the player opens.");
        events.OnPlayerActivate.Subscribe(args =>
        {
            if (args.ObjectActivated is { } activatedReference && activatedReference.GetBaseObject()?.GetFormType() == FormType.Container)
            {
                ISavedStatePair<int> openCountPair = savedState.GetFormEntries(activatedReference).GetPair<int>("opened");
                int openCount = (openCountPair.Value ?? 0) + 1;
                openCountPair.Value = openCount;
                debug.Notification($"Opened {openCount} times");
            }
        });
    }
}
