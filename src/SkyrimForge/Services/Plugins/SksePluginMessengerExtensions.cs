using System;
using System.Runtime.InteropServices;

namespace SkyrimForge;

/// <summary>Provides extension members for <see cref="ISksePluginMessenger"/>.</summary>
public static class SksePluginMessengerExtensions
{
    extension(ISksePluginMessenger messenger)
    {
        /// <summary>
        /// Sends <paramref name="payload"/> to one plugin, or to every plugin listening if no receiver is named.
        /// </summary>
        /// <param name="messageType">How the receiver is to read this message.</param>
        /// <param name="payload">The bytes to send.</param>
        /// <param name="receiver">
        /// The plugin to send to, named as <see cref="ISksePluginMessenger.Subscribe"/> describes, or <c>null</c> to reach every plugin listening for this one.
        /// </param>
        /// <inheritdoc cref="ISksePluginMessenger.Send(uint, nint, int, string?)" path="/returns"/>
        /// <inheritdoc cref="ISksePluginMessenger.Send(uint, nint, int, string?)" path="/remarks"/>
        public bool Send(uint messageType, ReadOnlySpan<byte> payload, string? receiver = null)
        {
            byte[] copy = GC.AllocateUninitializedArray<byte>(payload.Length, pinned: true);
            payload.CopyTo(copy);
            bool sent = messenger.Send(messageType, Marshal.UnsafeAddrOfPinnedArrayElement(copy, 0), copy.Length, receiver);
            GC.KeepAlive(copy);
            return sent;
        }
    }
}
