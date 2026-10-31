using System;

namespace SkyrimForge;

/// <summary>The game's debug functions.</summary>
/// <remarks>Call members on the game's thread.</remarks>
public interface IDebug
{
    /// <summary>Shows a message box the player must dismiss, which Papyrus calls Debug.MessageBox.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
    [Obsolete("Use IMessageBoxPrompter.ShowAsync.")]
    void MessageBox(string message);

    /// <summary>Shows a notification in the corner of the screen.</summary>
    /// <param name="message">The text shown.</param>
    /// <param name="sound">The editor ID of a sound record or a sound descriptor to play with it, or null for none.</param>
    /// <param name="dropIfQueued">Whether the notification is dropped if the same text is already waiting to be shown.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
    void Notification(string message, string? sound = null, bool dropIfQueued = true);
}
