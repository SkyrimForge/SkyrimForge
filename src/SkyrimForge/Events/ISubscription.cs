namespace SkyrimForge;

/// <summary>A handle to one registration a plugin made, such as a handler subscribed to an event or a package added to an alias.</summary>
/// <remarks>Call <see cref="Unsubscribe"/> on a thread on which the member that returned the subscription can be called.</remarks>
public interface ISubscription
{
    /// <summary>Ends the registration. Calling it again does nothing.</summary>
    void Unsubscribe();
}
