using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Sends mod events. Every registration for an event's name receives it, including the sending plugin's own.</summary>
/// <remarks>
/// <see cref="IGameEvents.OnModCallback"/> handlers run on the sending thread before <see cref="Send"/> returns.
/// Members can be called on any thread.
/// </remarks>
public interface IModEvents
{
    /// <summary>Broadcasts <paramref name="eventName"/>.</summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="eventName"/> or <paramref name="stringArgument"/> is <c>null</c>.</exception>
    void Send(string eventName, string stringArgument = "", float numberArgument = 0f, IForm? sender = null);
}
