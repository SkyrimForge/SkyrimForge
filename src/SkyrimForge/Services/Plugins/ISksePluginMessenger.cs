using System;

namespace SkyrimForge;

/// <summary>Sends messages to the other SKSE plugins in the load order and receives what they send.</summary>
/// <remarks>A payload is an address in the sender's memory. The sender and the receiver agree on its layout and lifetime.</remarks>
public interface ISksePluginMessenger
{
    /// <summary>
    /// Sends a message to one plugin, or to every plugin listening if no receiver is named.
    /// </summary>
    /// <param name="messageType">How the receiver is to read this message.</param>
    /// <param name="data">The payload's address. Every listener runs before this returns.</param>
    /// <param name="length">How many bytes the payload occupies.</param>
    /// <param name="receiver">The plugin to send to, named as <see cref="Subscribe"/> describes, or <c>null</c> to reach every plugin listening for this one.</param>
    /// <returns>Whether SKSE accepted the message. It declines one sent before plugins may message each other.</returns>
    /// <remarks>
    /// Every SkyrimForge plugin sends as SkyrimForge, so a receiver tells their messages apart by message type alone. Use a type no other SkyrimForge plugin uses.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
    bool Send(uint messageType, nint data = 0, int length = 0, string? receiver = null);

    /// <summary>
    /// Receives what one named plugin sends. The handler runs on the thread that sends the message.
    /// </summary>
    /// <param name="sender">
    /// The plugin to listen for, by the name its version data declares to SKSE, not its file name.
    /// SkyrimForge.Runtime.dll declares SkyrimForge, and PrismaUI.dll declares PrismaUI. With a misspelled name or an absent plugin, the handler never runs,
    /// and no error is raised.
    /// </param>
    /// <param name="handler">A handler that throws is dropped, and its exception is written to the log of the plugin that subscribed it.</param>
    /// <returns>A handle that removes the handler.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sender"/> or <paramref name="handler"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sender"/> is SKSE, whose messages <see cref="IGameEvents"/> raises as events.</exception>
    /// <exception cref="InvalidOperationException">SKSE declined the registration.</exception>
    ISubscription Subscribe(string sender, Action<SksePluginMessage> handler);
}
