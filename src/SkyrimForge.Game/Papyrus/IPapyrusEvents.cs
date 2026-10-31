namespace SkyrimForge.Papyrus;

/// <summary>Publishes events to Papyrus.</summary>
/// <remarks>A registration reaches every script on the registered object that declares the event, so the registering script and the receiving script can differ.</remarks>
public interface IPapyrusEvents
{
    /// <summary>Declares an event this plugin raises, or returns the existing one. Declare events on every start to reach the registrations a save restored.</summary>
    /// <remarks>
    /// Every plugin in the process shares the registrations of a name, so prefix it. Names are case-insensitive.
    /// A form cannot be passed to an event declared this way, since no script states the class of its parameter.
    /// </remarks>
    /// <param name="eventName">The name the receiving script declares, as <c>Event OnGoldSpent(int amount)</c>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="eventName"/> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="eventName"/> is empty.</exception>
    /// <exception cref="System.InvalidOperationException">No registration set could be made for the name.</exception>
    PapyrusEvent Declare(string eventName);

    /// <summary>
    /// Declares an event this plugin raises, with the script that states its parameter types, or returns the existing one.
    /// Declare events on every start to reach the registrations a save restored.
    /// </summary>
    /// <remarks>
    /// Each form passed is bound to the class the script's declaration states, and a receiving script that declares another class for that parameter does not receive the event.
    /// Every plugin in the process shares the registrations of a name, so prefix it. Names are case-insensitive.
    /// </remarks>
    /// <param name="scriptName">The script whose declaration states the parameter types, as <c>MyScript</c>.</param>
    /// <param name="eventName">The name the receiving script declares, as <c>Event OnTargetChosen(Actor target)</c>.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="scriptName"/> or <paramref name="eventName"/> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="scriptName"/> or <paramref name="eventName"/> is empty.</exception>
    /// <exception cref="System.InvalidOperationException">No registration set could be made for the name.</exception>
    PapyrusEvent Declare(string scriptName, string eventName);
}
