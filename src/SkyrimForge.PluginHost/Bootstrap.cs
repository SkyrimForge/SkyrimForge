using SkyrimForge.Game;
using SkyrimForge.Hosting;
using SkyrimForge.Infrastructure;
using SkyrimForge.Papyrus;
using SkyrimForge.PluginHost.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SkyrimForge.PluginHost;

/// <summary>The managed entry point the native host calls once CoreCLR is running in the game process.</summary>
public static class Bootstrap
{
    internal const string StartedMessage = Product.Name + " started";

    internal const string PluginDirectoryName = "Plugins";

    internal const string SharedLibraryDirectoryName = "SharedLibraries";

    /// <summary>Starts the framework. The native host calls this through hostfxr, which resolves it by name, so it is public.</summary>
    [UnmanagedCallersOnly]
    public static int Initialize(nint logDirectory)
    {
        // The native host starts the runtime only after SKSE provides a folder, so the pointer is never zero.
        return Start(Marshal.PtrToStringUTF8(logDirectory)!);
    }

    internal static int Start(string logDirectory)
    {
        var log = new FileLog(GetLogPath(logDirectory));
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            log.Write($"Unobserved task exception: {args.Exception}");
            args.SetObserved();
        };

        // An exception thrown into the native caller terminates the process.
        try
        {
            string processFileName = CurrentProcessFileName;
            log.Write($"{StartedMessage} on .NET {Environment.Version} in {processFileName} (Process ID: {Environment.ProcessId})");

            GameTarget gameTarget = GameTargetFactory.Create();

            IGameEvents events = GameEvents.Create(log, CurrentGameEdition.Value == GameEdition.VR, () => gameTarget.UI.GameIsPaused(), new KeyboardReader(gameTarget));

            RunPlugins(gameTarget, events, log, logDirectory);
            return 0;
        }
        catch (Exception ex)
        {
            log.Write($"{nameof(Start)} failed: {ex}");
            return 1;
        }
    }

    private static void RunPlugins(GameTarget gameTarget, IGameEvents events, FileLog log, string logDirectory)
    {
        string hostDirectory = AssemblyOrAppContextDirectory;
        string directory = Path.Combine(hostDirectory, PluginDirectoryName);
        var taskQueueSource = new TaskQueueSource();
        TaskQueueSource.Install(taskQueueSource, log);
        ITaskQueue logTaskQueue = taskQueueSource.CreateLogTaskQueue(log);
        PapyrusDispatcher.Install(new PapyrusDispatcher(gameTarget, logTaskQueue, log));

        var plugins = new PluginCollection();
        IReadOnlyDictionary<string, string> sharedLibraryPaths = SharedLibraryPaths.Find(Path.Combine(hostDirectory, SharedLibraryDirectoryName), log);
        (IReadOnlyList<IPluginServiceSource> serviceSources, IReadOnlyList<string> serviceSourceFailures) = ServiceSourceLoader.Load(sharedLibraryPaths.Values, log);
        var serviceResolver = new ServiceResolver(gameTarget, events, log, taskQueueSource, plugins, logDirectory);
        var pluginLoader = new PluginLoader(directory, log, serviceResolver, serviceSources, sharedLibraryPaths);
        plugins.AddRange(pluginLoader.Load());

        serviceResolver.CoSaveIfPreviouslyRequested?.Install();
        serviceResolver.VoiceSilencerIfPreviouslyRequested?.Install();

        var startFailures = new List<string>();
        foreach (IPlugin plugin in plugins)
        {
            try
            {
                plugin.Start();
            }
            catch (Exception ex)
            {
                log.Write($"Plugin failed: {plugin.GetType().FullName}: {ex}");
                pluginLoader.GetLog(plugin).Write($"Start failed: {ex}");
                startFailures.Add($"{plugin.GetType().FullName}: {PluginLoader.Describe(ex)}");
                continue;
            }
            log.Write($"Started {plugin.GetType().FullName}");
        }

        var failureMessages = new List<string>();
        if (serviceSourceFailures.Count > 0)
        {
            failureMessages.Add($"{Product.Name} could not load these service sources:\n\n{string.Join("\n\n", serviceSourceFailures)}");
        }
        if (pluginLoader.Failures.Count > 0)
        {
            failureMessages.Add($"{Product.Name} could not load these plugins:\n\n{string.Join("\n\n", pluginLoader.Failures)}");
        }
        if (startFailures.Count > 0)
        {
            failureMessages.Add($"{Product.Name} could not start these plugins:\n\n{string.Join("\n\n", startFailures)}");
        }
        if (failureMessages.Count > 0)
        {
            ShowAtMainMenu(events, string.Join("\n\n", failureMessages));
        }
    }

    private static void ShowAtMainMenu(IGameEvents events, string message)
    {
        ISubscription? subscription = null;
        subscription = events.OnMenuOpenClose.Subscribe(args =>
        {
            if (args.MenuName == MainMenu.MenuName && args.Opening)
            {
                subscription?.Unsubscribe();
                GlobalFunctions.DebugMessageBox(message);
            }
        });
    }

    internal static string GetLogPath(string logDirectory)
    {
        return Path.Combine(logDirectory, Product.Name + ".log");
    }

    internal static string CurrentProcessFileName => Path.GetFileName(Environment.ProcessPath) ?? "unknown";

    private static string AssemblyOrAppContextDirectory => Path.GetDirectoryName(typeof(Bootstrap).Assembly.Location) ?? AppContext.BaseDirectory;
}
