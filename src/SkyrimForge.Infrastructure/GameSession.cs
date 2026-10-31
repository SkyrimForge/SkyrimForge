using System;
using System.Threading;

namespace SkyrimForge.Infrastructure;

// Sources are not disposed, so a previous game's cancellation token remains usable.
internal sealed class GameSession : IGameSession
{
    private readonly ILog log;
    private CancellationTokenSource currentSource = new();

    public GameSession(IGameEvents events, ILog log)
    {
        this.log = log;
        events.OnNewGameOrSaveLoading.Subscribe(_ => ReplaceToken());
    }

    /// <inheritdoc/>
    public CancellationToken NewGameOrSaveLoading => Volatile.Read(ref currentSource).Token;

    private void ReplaceToken()
    {
        CancellationTokenSource previous = Interlocked.Exchange(ref currentSource, new CancellationTokenSource());
        try
        {
            previous.Cancel();
        }
        catch (AggregateException ex)
        {
            log.Write($"A NewGameOrSaveLoading cancellation callback threw: {ex.Flatten().Message}");
        }
    }
}
