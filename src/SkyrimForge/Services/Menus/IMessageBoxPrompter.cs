using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Shows message boxes and returns the button pressed.</summary>
/// <remarks>Call members on the game's thread.</remarks>
public interface IMessageBoxPrompter
{
    /// <summary>Shows a message box with one button for each entry of <paramref name="buttons"/>.</summary>
    /// <returns>
    /// A task containing the index in <paramref name="buttons"/> of the button pressed. It completes in work queued through <see cref="ITaskQueue"/>.
    /// The task is canceled if the game destroys the message box without a button press.
    /// <para>
    /// It is also canceled by <paramref name="cancellationToken"/>. No box is shown when the token is already canceled at the call.
    /// Otherwise canceling ends only the wait, and the box opens or stays open.
    /// Pass <see cref="IGameSession.NewGameOrSaveLoading"/> so the wait ends as a new game starts or a save starts loading.
    /// </para>
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="buttons"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="buttons"/> is empty or contains null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The game has no message box factory, the address library has no identifier for the function that queues a message box, or that function faulted.
    /// The exception is stored in the returned task.
    /// </exception>
    Task<int> ShowAsync(string message, IReadOnlyList<string> buttons, CancellationToken cancellationToken = default);
}
