using System;
using System.Threading;

namespace SkyrimForge;

/// <summary>A value created anew for each game, whether from a new character or a loaded save.</summary>
/// <remarks>The value remains through a return to the main menu, until a save loads or a new game starts. Call members on the game's thread.</remarks>
/// <exception cref="ArgumentNullException"><paramref name="session"/> or <paramref name="create"/> is <c>null</c>.</exception>
public sealed class SessionValue<T>(IGameSession session, Func<T> create)
{
    private readonly IGameSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly Func<T> create = create ?? throw new ArgumentNullException(nameof(create));

    private CancellationToken valueNewGameOrSaveLoadingToken;
    private T value = default!;

    /// <summary>The value for the current game, created on the first read in each game.</summary>
    public T Value
    {
        get
        {
            CancellationToken newGameOrSaveLoadingToken = session.NewGameOrSaveLoading;
            if (newGameOrSaveLoadingToken != valueNewGameOrSaveLoadingToken)
            {
                value = create();
                valueNewGameOrSaveLoadingToken = newGameOrSaveLoadingToken;
            }
            return value;
        }
    }
}
