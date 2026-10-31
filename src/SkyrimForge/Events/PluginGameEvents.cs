using SkyrimForge.Game;
using System;
using System.Threading.Tasks;

namespace SkyrimForge;

internal sealed partial class PluginGameEvents(IGameEvents events, ILog log) : IGameEvents
{
    private readonly IGameEvents events = events;
    private readonly ILog log = log;

    public IGameEvent<FrameEventArgs> OnFrame => WithLog(events.OnFrame, nameof(OnFrame));

    public IGameEvent<PostLoadEventArgs> OnPostLoad => WithLog(events.OnPostLoad, nameof(OnPostLoad));

    public IGameEvent<PostPostLoadEventArgs> OnPostPostLoad => WithLog(events.OnPostPostLoad, nameof(OnPostPostLoad));

    public IGameEvent<InputLoadedEventArgs> OnInputLoaded => WithLog(events.OnInputLoaded, nameof(OnInputLoaded));

    public IGameEvent<DataLoadedEventArgs> OnDataLoaded => WithLog(events.OnDataLoaded, nameof(OnDataLoaded));

    public IGameEvent<NewGameEventArgs> OnNewGame => WithLog(events.OnNewGame, nameof(OnNewGame));

    public IGameEvent<SaveEventArgs> OnSaveLoading => WithLog(events.OnSaveLoading, nameof(OnSaveLoading));

    public IGameEvent<NewGameOrSaveLoadingEventArgs> OnNewGameOrSaveLoading => WithLog(events.OnNewGameOrSaveLoading, nameof(OnNewGameOrSaveLoading));

    public IGameEvent<SaveLoadedEventArgs> OnSaveLoaded => WithLog(events.OnSaveLoaded, nameof(OnSaveLoaded));

    public IGameEvent<NewGameOrSaveLoadedEventArgs> OnNewGameOrSaveLoaded => WithLog(events.OnNewGameOrSaveLoaded, nameof(OnNewGameOrSaveLoaded));

    public IGameEvent<SaveEventArgs> OnSaveWriting => WithLog(events.OnSaveWriting, nameof(OnSaveWriting));

    public IGameEvent<SaveEventArgs> OnSaveDeleted => WithLog(events.OnSaveDeleted, nameof(OnSaveDeleted));

    public IGameEvent<InputEventArgs> OnKeyDown(BSKeyboardDevice.Keys.Key key, ModifierKeys modifiers) => WithLog(events.OnKeyDown(key, modifiers), nameof(OnKeyDown));

    private GameEventWithLog<TArgs> WithLog<TArgs>(IGameEvent<TArgs> innerGameEvent, string name) where TArgs : struct
    {
        return new GameEventWithLog<TArgs>(innerGameEvent, name, log);
    }

    // Logs and rethrows a handler's exception, so the inner event still drops the subscriber.
    private sealed class GameEventWithLog<TArgs>(IGameEvent<TArgs> innerEvent, string name, ILog log) : IGameEvent<TArgs> where TArgs : struct
    {
        public ISubscription Subscribe(GameEventHandler<TArgs> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            return innerEvent.Subscribe(args =>
            {
                try
                {
                    handler(args);
                }
                catch (Exception ex)
                {
                    Report(ex);
                    throw;
                }
            });
        }

        public ISubscription Subscribe(AsyncGameEventHandler<TArgs> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            return innerEvent.Subscribe(args => Invoke(handler, args));
        }

        private async Task Invoke(AsyncGameEventHandler<TArgs> handler, TArgs args)
        {
            try
            {
                await handler(args);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Report(ex);
                throw;
            }
        }

        private void Report(Exception exception) => log.Write($"{name} dropped a subscriber that threw: {exception}");
    }
}
