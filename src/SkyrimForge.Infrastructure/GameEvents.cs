using SkyrimForge.Game;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SkyrimForge.Infrastructure;

internal sealed unsafe partial class GameEvents : IGameEvents
{
    private static GameEvents? instance;

    private readonly GameEvent<FrameEventArgs> onFrame;
    private readonly GameEvent<PostLoadEventArgs> onPostLoad;
    private readonly GameEvent<PostPostLoadEventArgs> onPostPostLoad;
    private readonly GameEvent<InputLoadedEventArgs> onInputLoaded;
    private readonly GameEvent<DataLoadedEventArgs> onDataLoaded;
    private readonly GameEvent<NewGameEventArgs> onNewGame;
    private readonly GameEvent<SaveEventArgs> onSaveLoading;
    private readonly GameEvent<NewGameOrSaveLoadingEventArgs> onNewGameOrSaveLoading;
    private readonly GameEvent<SaveLoadedEventArgs> onSaveLoaded;
    private readonly GameEvent<NewGameOrSaveLoadedEventArgs> onNewGameOrSaveLoaded;
    private readonly GameEvent<SaveEventArgs> onSaveWriting;
    private readonly GameEvent<SaveEventArgs> onSaveDeleted;
    private readonly ILog log;

    // Caps one frame's contribution to unpaused time, so a load's long frame does not count whole.
    private static readonly TimeSpan longestFrame = TimeSpan.FromSeconds(0.25);

    // Real time, because the game's run-time counter runs slow and its frame interval runs ahead while a menu opens or closes.
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Func<bool> isGamePaused;
    private readonly KeyCombinationEvents keyCombinations;

    private TimeSpan unpausedTime;
    private TimeSpan? lastFrameTime;

    private GameEvents(ILog log, bool runningVr, Func<bool> isGamePaused, IKeyboardReader keyboardReader)
    {
        this.log = log;
        this.isGamePaused = isGamePaused;
        onFrame = new GameEvent<FrameEventArgs>(ex => LogSubscriberDropped(nameof(OnFrame), ex));
        onPostLoad = new GameEvent<PostLoadEventArgs>(ex => LogSubscriberDropped(nameof(OnPostLoad), ex));
        onPostPostLoad = new GameEvent<PostPostLoadEventArgs>(ex => LogSubscriberDropped(nameof(OnPostPostLoad), ex));
        onInputLoaded = new GameEvent<InputLoadedEventArgs>(ex => LogSubscriberDropped(nameof(OnInputLoaded), ex));
        onDataLoaded = new GameEvent<DataLoadedEventArgs>(ex => LogSubscriberDropped(nameof(OnDataLoaded), ex));
        onNewGame = new GameEvent<NewGameEventArgs>(ex => LogSubscriberDropped(nameof(OnNewGame), ex));
        onSaveLoading = new GameEvent<SaveEventArgs>(ex => LogSubscriberDropped(nameof(OnSaveLoading), ex));
        onNewGameOrSaveLoading = new GameEvent<NewGameOrSaveLoadingEventArgs>(ex => LogSubscriberDropped(nameof(OnNewGameOrSaveLoading), ex));
        onSaveLoaded = new GameEvent<SaveLoadedEventArgs>(ex => LogSubscriberDropped(nameof(OnSaveLoaded), ex));
        onNewGameOrSaveLoaded = new GameEvent<NewGameOrSaveLoadedEventArgs>(ex => LogSubscriberDropped(nameof(OnNewGameOrSaveLoaded), ex));
        onSaveWriting = new GameEvent<SaveEventArgs>(ex => LogSubscriberDropped(nameof(OnSaveWriting), ex));
        onSaveDeleted = new GameEvent<SaveEventArgs>(ex => LogSubscriberDropped(nameof(OnSaveDeleted), ex));
        OnPostLoad = runningVr ? new UnraisedVrEvent<PostLoadEventArgs>(onPostLoad, nameof(OnPostLoad), log) : onPostLoad;
        OnPostPostLoad = runningVr ? new UnraisedVrEvent<PostPostLoadEventArgs>(onPostPostLoad, nameof(OnPostPostLoad), log) : onPostPostLoad;
        InitializeGeneratedEvents();
        keyCombinations = new KeyCombinationEvents(onInput, keyboardReader, ex => LogSubscriberDropped(nameof(OnKeyDown), ex));
    }

    /// <inheritdoc/>
    public IGameEvent<FrameEventArgs> OnFrame => onFrame;

    /// <inheritdoc/>
    public IGameEvent<PostLoadEventArgs> OnPostLoad { get; }

    /// <inheritdoc/>
    public IGameEvent<PostPostLoadEventArgs> OnPostPostLoad { get; }

    /// <inheritdoc/>
    public IGameEvent<InputLoadedEventArgs> OnInputLoaded => onInputLoaded;

    /// <inheritdoc/>
    public IGameEvent<DataLoadedEventArgs> OnDataLoaded => onDataLoaded;

    /// <inheritdoc/>
    public IGameEvent<NewGameEventArgs> OnNewGame => onNewGame;

    /// <inheritdoc/>
    public IGameEvent<SaveEventArgs> OnSaveLoading => onSaveLoading;

    /// <inheritdoc/>
    public IGameEvent<NewGameOrSaveLoadingEventArgs> OnNewGameOrSaveLoading => onNewGameOrSaveLoading;

    /// <inheritdoc/>
    public IGameEvent<SaveLoadedEventArgs> OnSaveLoaded => onSaveLoaded;

    /// <inheritdoc/>
    public IGameEvent<NewGameOrSaveLoadedEventArgs> OnNewGameOrSaveLoaded => onNewGameOrSaveLoaded;

    /// <inheritdoc/>
    public IGameEvent<SaveEventArgs> OnSaveWriting => onSaveWriting;

    /// <inheritdoc/>
    public IGameEvent<SaveEventArgs> OnSaveDeleted => onSaveDeleted;

    /// <inheritdoc/>
    public IGameEvent<InputEventArgs> OnKeyDown(BSKeyboardDevice.Keys.Key key, ModifierKeys modifiers) => keyCombinations.Get(key, modifiers);

    public static GameEvents Create(ILog log, bool runningVr, Func<bool> isGamePaused, IKeyboardReader keyboardReader)
    {
        var events = new GameEvents(log, runningVr, isGamePaused, keyboardReader);
        instance = events;
        InstallOnFrame(log);
        NativeGlobals.SF_Lifecycle_Register(&OnLifecycleHook);
        RegisterGeneratedSinks(log);
        return events;
    }

    // Runs on the game's thread, except for kDataLoaded, which arrives on another thread. An exception thrown into SKSE's dispatch ends the process.
    [UnmanagedCallersOnly]
    private static void OnLifecycleHook(SessionMessage type, nint name, int length)
    {
        GameEvents? events = instance;
        if (events == null)
        {
            return;
        }
        try
        {
            var save = new SaveEventArgs(name != 0 && length > 0 ? Marshal.PtrToStringUTF8(name, length) : null);
            switch (type)
            {
                case SessionMessage.PostLoad: events.onPostLoad.Raise(default); break;
                case SessionMessage.PostPostLoad: events.onPostPostLoad.Raise(default); break;
                case SessionMessage.InputLoaded: events.onInputLoaded.Raise(default); break;
                case SessionMessage.DataLoaded: events.onDataLoaded.Raise(default); break;
                case SessionMessage.NewGame:
                    events.onNewGameOrSaveLoading.Raise(default);
                    events.onNewGame.Raise(default);
                    events.onNewGameOrSaveLoaded.Raise(default);
                    break;
                case SessionMessage.PreLoadGame:
                    events.onNewGameOrSaveLoading.Raise(default);
                    events.onSaveLoading.Raise(save);
                    break;
                case SessionMessage.PostLoadGame:
                    events.onSaveLoaded.Raise(default);
                    events.onNewGameOrSaveLoaded.Raise(default);
                    break;
                case SessionMessage.SaveGame: events.onSaveWriting.Raise(save); break;
                case SessionMessage.DeleteGame: events.onSaveDeleted.Raise(save); break;
                default: break;
            }
        }
        catch (Exception ex)
        {
            events.LogHookFailure(nameof(OnLifecycleHook), ex);
        }
    }

    private static void InstallOnFrame(ILog log)
    {
        GameOffset updateCallOffset = new(0x11F, 0x160, 0x11F);
        if (NativeGlobals.SF_Frame_Register(&OnFrameHook,
            NativeGlobals.SF_Hook_FunctionAddress(GameFunction.MainLoop.SpecialEdition, GameFunction.MainLoop.AnniversaryEdition, GameFunction.MainLoop.VR),
            NativeGlobals.SF_Hook_FunctionAddress(GameFunction.Main__Update.SpecialEdition, GameFunction.Main__Update.AnniversaryEdition, GameFunction.Main__Update.VR),
            updateCallOffset.SpecialEdition, updateCallOffset.AnniversaryEdition, updateCallOffset.VR))
        {
            log.Write("OnFrame hooked Main::Update.");
        }
        else
        {
            log.Write("OnFrame unavailable: the Main::Update call site did not resolve or verify.");
        }
    }

    // An exception thrown into the game's code ends the process.
    [UnmanagedCallersOnly]
    private static void OnFrameHook(float interval)
    {
        GameEvents? events = instance;
        if (events != null)
        {
            try
            {
                events.onFrame.Raise(new FrameEventArgs(TimeSpan.FromSeconds(interval), events.AdvanceUnpausedTime()));
            }
            catch (Exception ex)
            {
                events.LogHookFailure(nameof(OnFrameHook), ex);
            }
        }
    }

    private TimeSpan AdvanceUnpausedTime()
    {
        TimeSpan now = clock.Elapsed;
        TimeSpan step = now - lastFrameTime ?? TimeSpan.Zero;
        lastFrameTime = now;
        if (!IsPaused)
        {
            unpausedTime += step < longestFrame ? step : longestFrame;
        }
        return unpausedTime;
    }

    // A frame before the game creates its UI counts as unpaused, because the UI throws until then.
    private bool IsPaused
    {
        get
        {
            try
            {
                return isGamePaused();
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    private void LogSubscriberDropped(string name, Exception exception)
    {
        log.Write($"{name} dropped a subscriber that threw: {exception.GetType().Name}: {exception.Message}");
    }

    private void LogHookFailure(string hookName, Exception exception)
    {
        log.Write($"{hookName} threw and was dropped: {exception.GetType().Name}: {exception.Message}");
    }

    private sealed class UnraisedVrEvent<TArgs>(IGameEvent<TArgs> inner, string name, ILog log) : IGameEvent<TArgs> where TArgs : struct
    {
        public ISubscription Subscribe(GameEventHandler<TArgs> handler)
        {
            ReportUnraised();
            return inner.Subscribe(handler);
        }

        public ISubscription Subscribe(AsyncGameEventHandler<TArgs> handler)
        {
            ReportUnraised();
            return inner.Subscribe(handler);
        }

        private void ReportUnraised() => log.Write($"{name} is never raised on Skyrim VR, because SKSE sends it before the .NET runtime starts. OnInputLoaded is the first event a VR plugin receives.");
    }
}
