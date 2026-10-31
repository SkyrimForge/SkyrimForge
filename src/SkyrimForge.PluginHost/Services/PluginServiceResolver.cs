using SkyrimForge.Game;
using SkyrimForge.Hosting;
using SkyrimForge.Infrastructure;
using SkyrimForge.Papyrus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SkyrimForge.PluginHost.Services;

internal sealed class PluginServiceResolver
{
    private readonly Type pluginType;
    private readonly ServiceCollection serviceCollection;

    internal static string GetIdentity(Type pluginType)
    {
        PluginIdentityAttribute? identityAttribute = pluginType.GetCustomAttribute<PluginIdentityAttribute>();
        if (identityAttribute == null)
        {
            return pluginType.FullName!;
        }
        if (string.IsNullOrWhiteSpace(identityAttribute.Identity) || identityAttribute.Identity.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"The identity \"{identityAttribute.Identity}\" of {pluginType.FullName} is empty or contains a character not valid in a file name.");
        }
        return identityAttribute.Identity;
    }

    public PluginServiceResolver(Type pluginType, ServiceResolver serviceResolver, string directory, IReadOnlyList<IPluginServiceSource> serviceSources)
    {
        this.pluginType = pluginType;
        string identity = GetIdentity(pluginType);
        serviceCollection = new ServiceCollection(serviceResolver.ServiceCollection);
        serviceCollection.Add(() => serviceResolver.CoSave.CreatePluginSavedState(identity));
        serviceCollection.Add<ILog>(() => new FileLog(Path.Combine(directory, identity + ".log")));
        serviceCollection.Add<IFunctionHooks>(() => new FunctionHooks(identity));
        serviceCollection.Add<ICodePatcher>(() => new CodePatcher(identity, serviceCollection.GetRequired<ILog>()));
        serviceCollection.Add(() => serviceResolver.TaskQueueSource.CreateLogTaskQueue(serviceCollection.GetRequired<ILog>()));
        serviceCollection.Add<IGameEvents>(() => new PluginGameEvents(serviceResolver.Events, serviceCollection.GetRequired<ILog>()));
        serviceCollection.Add<IPapyrusFunctionRegistrar, PapyrusFunctionRegistrar>();
        serviceCollection.Add<IPapyrusEvents, PapyrusEvents>();
        serviceCollection.Add<ICustomMenuRegistrar, CustomMenuRegistrar>();
        serviceCollection.Add<IAnimationEvents, AnimationEvents>();
        serviceCollection.Add<IStoryEvents, StoryEvents>();
        serviceCollection.Add<IPlayerLocationEvents, PlayerLocationEvents>();
        serviceCollection.Add<IMessageBoxPrompter, MessageBoxPrompter>();
        serviceCollection.Add<IConsoleCommandRegistrar, ConsoleCommandRegistrar>();
        serviceCollection.Add<IBookMenuPresenter, BookMenuPresenter>();
        serviceCollection.Add<IReadableNotes, ReadableNotes>();
        serviceCollection.Add<IPlayerPositionHistory, PlayerPositionHistory>();
        serviceCollection.Add<IWorldMapMarkers, WorldMapMarkers>();
        serviceCollection.Add<ISksePluginMessenger, SksePluginMessenger>();
        serviceCollection.Add<PlayerTopicFactory>();
        serviceCollection.Add(() => new PluginVoices(pluginType.Assembly.GetName().Name!));
        serviceCollection.Add<IRuntimeQuestFactory, RuntimeQuestFactory>();
        serviceCollection.Add<IUnvoicedSpeech, UnvoicedSpeech>();
        serviceCollection.Add<IForceGreet, ForceGreet>();
        serviceCollection.Add<IGreetingSilencer, GreetingSilencer>();
        serviceCollection.Add<IFollowers, Followers>();
        serviceCollection.Add<IScaleformFunctionRegistrar, ScaleformFunctionRegistrar>();
        serviceCollection.Add<IDialogueBranchFactory, DialogueBranchFactory>();
        serviceCollection.Add<ITriggerZoneFactory>(() =>
            TriggerZoneFactory.Create
            (
                serviceCollection.GetRequired<GameTarget>(),
                serviceCollection.GetRequired<IGameEvents>(),
                serviceCollection.GetRequired<IPerPluginSavedState>(),
                serviceCollection.GetRequired<ILog>()
            ));
        serviceCollection.Add<ICalendarTimerFactory>(() =>
        {
            GameTarget gameTarget = serviceCollection.GetRequired<GameTarget>();
            return new CalendarTimerFactory
            (
                serviceCollection.GetRequired<IGameEvents>(),
                serviceCollection.GetRequired<IPerPluginSavedState>(),
                serviceCollection.GetRequired<ILog>(),
                () => gameTarget.Calendar.TimePassed
            );
        });

        foreach (IPluginServiceSource source in serviceSources)
        {
            try
            {
                source.AddServices(serviceCollection);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{source.GetType().FullName} could not add its services: {ex.Message}", ex);
            }
        }
    }

    public IEnumerable<Type> PublicServiceTypes => serviceCollection.PublicServiceTypes;

    public object Construct()
    {
        return serviceCollection.Construct(pluginType);
    }

    public T GetRequired<T>()
    {
        return serviceCollection.GetRequired<T>();
    }
}
