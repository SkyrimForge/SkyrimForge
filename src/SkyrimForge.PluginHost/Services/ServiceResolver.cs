using SkyrimForge.Game;
using SkyrimForge.Infrastructure;

namespace SkyrimForge.PluginHost.Services;

internal sealed class ServiceResolver
{
    public ServiceCollection ServiceCollection { get; }

    public ServiceResolver(GameTarget gameTarget, IGameEvents events, ILog log, TaskQueueSource taskQueueSource, IPluginCollection plugins, string skseLogDirectory)
    {
        ServiceCollection = new ServiceCollection();
        ServiceCollection.Add(() => gameTarget);
        ServiceCollection.Add(() => events);
        ServiceCollection.Add(() => log);
        ServiceCollection.Add(() => taskQueueSource);
        ServiceCollection.Add(() => plugins);
        ServiceCollection.Add<IConsole>(() => new GameConsole());
        ServiceCollection.Add<IConsoleCommands>(() => new ConsoleCommands());
        ServiceCollection.Add<IFrameworkFunctionHooks>(() => new FunctionHooks(Product.Name));
        ServiceCollection.Add<IGameSession>(() => new GameSession(events, log));
        ServiceCollection.Add<IDebug>(() => new Debug());
        ServiceCollection.Add<IUtility>(() => new Utility(events, ServiceCollection.GetRequired<IGameSession>()));
        ServiceCollection.Add<IModEvents>(() => new ModEvents());
        ServiceCollection.Add<IGameFileSystem>(() => new GameFileSystem());
        ServiceCollection.Add<ISkseLogDirectory>(() => new SkseLogDirectory(skseLogDirectory));
        ServiceCollection.Add<CoSave>();
        ServiceCollection.Add<MouthAnimator>();
        ServiceCollection.Add<SubtitleSwitch>();
        ServiceCollection.Add<UnvoicedLipSync>();
        ServiceCollection.Add<VoiceSilencer>();
        ServiceCollection.Add<TopicHider>();
        ServiceCollection.Add<ICourier, Courier>();
    }

    public IGameEvents Events => ServiceCollection.GetRequired<IGameEvents>();

    public TaskQueueSource TaskQueueSource => ServiceCollection.GetRequired<TaskQueueSource>();

    public CoSave CoSave => ServiceCollection.GetRequired<CoSave>();

    public CoSave? CoSaveIfPreviouslyRequested => ServiceCollection.GetConstructedInstance<CoSave>();

    public VoiceSilencer? VoiceSilencerIfPreviouslyRequested => ServiceCollection.GetConstructedInstance<VoiceSilencer>();
}
