using SkyrimForge.Game;

namespace SkyrimForge.Papyrus;

/// <summary>A form with its Papyrus functions reachable on it, which <c>form.Papyrus</c> returns.</summary>
/// <remarks>
/// A function that both a script and a script it extends declare is instead a static of each script's class, which takes the form first,
/// as <c>FormScript.GetWeight(form)</c> and <c>ActorBaseScript.GetWeight(actorBase)</c> do.
/// </remarks>
public sealed class PapyrusScript<T> where T : IForm
{
    internal PapyrusScript(T receiver) => Receiver = receiver;

    /// <summary>The receiving form.</summary>
    public T Receiver { get; }
}
