using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SkyrimForge;

/// <summary>Shows runtime text in the book menu.</summary>
/// <remarks>
/// Opened during a conversation, the menu shows above the dialogue menu, and the conversation continues once it closes.
/// Call members on the game's thread.
/// </remarks>
public interface IBookMenuPresenter
{
    /// <summary>Opens the book menu on <paramref name="htmlPages"/>.</summary>
    /// <param name="htmlPages">
    /// The pages as HTML. Encode text with <see cref="WebUtility.HtmlEncode(string?)"/>. <see cref="Game.BookFonts"/> lists the font faces.
    /// </param>
    /// <param name="cancellationToken">
    /// Stops the wait. No menu is shown when the token is already canceled at the call. Otherwise canceling ends only the wait, and the menu opens or stays open.
    /// </param>
    /// <returns>A task that completes in work queued through <see cref="ITaskQueue"/> when the menu closes, and does not complete if the game opens no menu.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="htmlPages"/> is <c>null</c>.</exception>
    /// <exception cref="System.InvalidOperationException">A book menu is already open. The exception is stored in the returned task.</exception>
    Task ShowAsync(IEnumerable<string> htmlPages, CancellationToken cancellationToken = default);
}
