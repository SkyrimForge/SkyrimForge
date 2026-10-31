using System;

namespace SkyrimForge;

/// <summary>A message one SKSE plugin sends another.</summary>
/// <param name="Sender">The sending plugin, named as <see cref="ISksePluginMessenger.Subscribe"/> describes.</param>
/// <param name="MessageType">The sender's own number for this kind of message.</param>
/// <param name="Data">The payload's address in this process, or 0 for a message with no payload.</param>
/// <param name="Length">The payload's length in bytes.</param>
public readonly record struct SksePluginMessage(string Sender, uint MessageType, nint Data, int Length)
{
    /// <summary>The payload as a span over the sender's memory, empty for a message with no payload.</summary>
    public unsafe ReadOnlySpan<byte> Payload => new((void*)Data, Length);
}
