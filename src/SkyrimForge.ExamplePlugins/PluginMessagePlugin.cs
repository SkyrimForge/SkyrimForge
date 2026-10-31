using SkyrimForge.Game;
using System;

namespace SkyrimForge.ExamplePlugins;

/// <summary>Sends a message through SKSE's messaging interface when F10 is pressed, and receives messages SKSE delivers to this framework.</summary>
public sealed class PluginMessagePlugin(IGameEvents events, ISksePluginMessenger messenger, ILog log) : IPlugin
{
    private const uint messageType = 0x5346_0001;

    private ISubscription? subscription;
    private byte[]? receivedBytes;

    /// <inheritdoc/>
    public void Start()
    {
        events.OnKeyDown(BSKeyboardDevice.Keys.Key.kF10, ModifierKeys.None).Subscribe(_ => SendMessageToSelf());
        log.Write("F10 sends a plugin message to this plugin.");
    }

    private void SendMessageToSelf()
    {
        const string pluginName = "SkyrimForge";
        if (subscription == null)
        {
            subscription = messenger.Subscribe(pluginName, Receive);
        }

        receivedBytes = null;
        bool sent = messenger.Send(messageType, [0x2A, 0x53, 0x53, 0x0D], pluginName);

        if (!sent)
        {
            log.Write("SKSE rejected the message.");
            return;
        }
        if (receivedBytes == null)
        {
            log.Write("The message was sent but not received.");
            return;
        }
        log.Write($"Received a message of type 0x{messageType:X} from {pluginName}: 0x{Convert.ToHexString(receivedBytes)}.");
    }

    /// <summary>Copies the payload, because the sender controls the buffer's lifetime.</summary>
    private void Receive(SksePluginMessage message)
    {
        if (message.MessageType == messageType)
        {
            receivedBytes = message.Payload.ToArray();
        }
    }
}
