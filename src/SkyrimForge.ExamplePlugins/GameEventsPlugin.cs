using System.Linq;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Subscribes to every game event, logging each one's fields with actor references resolved to names.</summary>
public sealed class GameEventsPlugin(IGameEvents events, IStoryEvents storyEvents, ILog log) : IPlugin
{
    /// <inheritdoc/>
    public void Start()
    {
        GameEventSubscriptions.SubscribeAll(events, Report);
        storyEvents.OnStoryEvent.Subscribe(args =>
        {
            string values = string.Join(", ", args.Values.Select(value => $"{value.Name}={EventField.Describe(value)}"));
            log.Write($"Story {args.Signature} \"{args.Name}\": {values}");
        });
        log.Write("Logging every game event.");
    }

    private void Report(string name, string summary) => log.Write(summary.Length > 0 ? $"{name}: {summary}" : name);
}
