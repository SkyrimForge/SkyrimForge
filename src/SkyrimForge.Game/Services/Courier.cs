using SkyrimForge.Papyrus;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge.Game;

// The game reports each delivered item by base form alone, so the names are read from the courier's container as the courier's line begins.
internal sealed class Courier : ICourier
{
    private static readonly PapyrusScriptDispatcher script = new("WICourierScript");

    private readonly GameTarget gameTarget;
    private readonly IUtility utility;
    private readonly IDebug debug;

    // Replaced whole. The game raises a container change on any thread.
    private string[] names = [];

    public Courier(GameTarget gameTarget, IGameEvents events, IUtility utility, IDebug debug, ILog log)
    {
        this.gameTarget = gameTarget;
        this.utility = utility;
        this.debug = debug;
        events.OnTopicInfo.Subscribe(args =>
        {
            if (args.Type == TESTopicInfoEvent.TopicInfoEventType.kTopicBegin && args.SpeakerReference?.FormID == KnownForms.WICourierNPCRef.Value)
            {
                Volatile.Write(ref names, ReadNames());
            }
        });
        events.OnContainerChanged.Subscribe(args =>
        {
            if (args.OldContainer == KnownForms.WICourierContainerRef && args.NewContainer == KnownForms.PlayerRef)
            {
                AnnounceAsync(Interlocked.Exchange(ref names, [])).LogFault(log);
            }
        });
    }

    public async Task SendAsync<TItem>(TItem item) where TItem : IObjectReference
    {
        Quest form = KnownForms.WICourier.Form;
        PapyrusArguments arguments = new PapyrusArguments().Add(item);
        await script.CallAsync(form, "addRefToContainer", arguments);
    }

    private static string[] ReadNames() =>
    [
        .. (KnownForms.WICourierContainerRef.Form.GetInventoryChanges()?.EntryList.Get() ?? [])
            .Where(entry => entry.Object != null)
            .SelectMany(entry =>
            {
                TESBoundObject item = entry.Object!.Value;
                (string? Name, int Count)[] extraStacks = [.. entry.ExtraLists.Get().Select(extraList => (extraList.GetDisplayName(item), extraList.GetCount()))];
                int plainCount = entry.CountDelta - extraStacks.Sum(stack => stack.Count);
                (string? Name, int Count)[] stacks = plainCount > 0 ? [.. extraStacks, (item.GetName(), plainCount)] : extraStacks;
                return stacks;
            })
            .Where(stack => !string.IsNullOrEmpty(stack.Name))
            .Select(stack => stack.Count > 1 ? $"{stack.Name} ({stack.Count})" : stack.Name!)
    ];

    // The delay places each name after the "Item(s) added" notice from the courier's script.
    private async Task AnnounceAsync(string[] deliveredNames)
    {
        if (deliveredNames.Length == 0)
        {
            return;
        }
        await utility.WaitAsync(TimeSpan.FromSeconds(0.5));
        string addItemText = gameTarget.GameSettingCollection.GetSetting("sAddItemtoInventory")?.GetString()?.ToLowerInvariant()
            ?? throw new InvalidOperationException("The game declares no sAddItemtoInventory setting.");
        foreach (string name in deliveredNames)
        {
            debug.Notification($"{name} {addItemText}", dropIfQueued: false);
        }
    }
}
