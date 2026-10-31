using SkyrimForge.Game;
using System.Globalization;
using System.Threading.Tasks;

namespace SkyrimForge.ExamplePlugins;

/// <summary>
/// Sends and receives mod events. Sending an event name invokes every registration for that name, from Papyrus scripts, SKSE plugins, and SkyrimForge plugins.
/// </summary>
public sealed class ModMessagePlugin(IGameEvents events, IModEvents modEvents, ILog log) : IPlugin
{
    // The type's full name, to avoid collisions with other mods.
    private static readonly string ownEvent = typeof(ModMessagePlugin).FullName!;

    /// <inheritdoc/>
    public void Start()
    {
        events.OnModCallback.Subscribe(args =>
        {
            string senderName = args.Sender is { } sender ? sender.GetName() : "no form";
            string number = args.NumberArgument.ToString("F0", CultureInfo.InvariantCulture);
            log.Write($"Received \"{args.EventName}\" \"{args.StringArgument}\" {number} from {senderName}.");
        });
        SendAfterSaveLoadsAsync().LogFault(log);
        log.Write("Logging mod events. One is sent after a save loads.");
    }

    private async Task SendAfterSaveLoadsAsync()
    {
        await events.OnSaveLoaded.WaitForNextAsync();
        modEvents.Send(ownEvent, "hello", 42f);
        log.Write($"Sent \"{ownEvent}\".");
    }
}
