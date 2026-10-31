using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>
/// Silences the greetings an actor says as a conversation with the player opens and to the player nearby outside one,
/// and the remarks it says unprompted outside a conversation: idle chatter and the lines said on noticing a corpse.
/// </summary>
/// <remarks>
/// The goodbye parameter of <see cref="IDialogueBranchFactory.Create"/> silences or replaces the goodbye said as a conversation closes.
/// The greeting of a dialogue branch shown for the actor is said in place of the silence.
/// The silence ends for every actor when a save starts loading or a new game begins.
/// Call members on the game's thread.
/// </remarks>
public interface IGreetingSilencer
{
    /// <summary>Silences the greetings of <paramref name="actor"/> until this plugin restores them.</summary>
    void SilenceGreetings<TActor>(TActor actor) where TActor : IActor;

    /// <summary>Ends this plugin's silence of the greetings of <paramref name="actor"/>. They are said again once no other plugin silences them.</summary>
    void RestoreGreetings<TActor>(TActor actor) where TActor : IActor;

    /// <summary>Silences the remarks of <paramref name="actor"/> until this plugin restores them.</summary>
    void SilenceRemarks<TActor>(TActor actor) where TActor : IActor;

    /// <summary>Ends this plugin's silence of the remarks of <paramref name="actor"/>. They are said again once no other plugin silences them.</summary>
    void RestoreRemarks<TActor>(TActor actor) where TActor : IActor;
}
