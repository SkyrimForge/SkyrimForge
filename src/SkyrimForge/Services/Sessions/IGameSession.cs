using System.Threading;

namespace SkyrimForge;

/// <summary>The current game, from a new character or a loaded save until the next one.</summary>
/// <remarks>
/// Work that awaits across a load resumes in the next game, whose objects and saved state differ.
/// Read <see cref="NewGameOrSaveLoading"/> when such work starts, pass it on, and check it after each await. Returning to the main menu does not cancel it.
/// </remarks>
public interface IGameSession
{
    /// <summary>
    /// A cancellation token canceled as a new game starts or a save starts loading, on <see cref="IGameEvents.OnNewGameOrSaveLoading"/>.
    /// </summary>
    CancellationToken NewGameOrSaveLoading { get; }
}
