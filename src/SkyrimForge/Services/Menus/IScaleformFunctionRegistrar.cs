using SkyrimForge.Game;
using System;

namespace SkyrimForge;

/// <summary>Adds functions that this plugin implements to Scaleform movies, for their ActionScript to call.</summary>
/// <remarks>
/// <code>
/// // _root.getPlayerGold() in the movie's ActionScript calls the delegate below.
/// scaleform.Register(movie, root, "getPlayerGold", call =&gt; call.RetVal?.SetNumber(player.GetGoldAmount()));
/// </code>
/// <para>
/// A delegate may call the game directly. An exception from a delegate is written to the log of the plugin that registered it.
/// A registration lasts until the process exits.
/// </para>
/// </remarks>
public interface IScaleformFunctionRegistrar
{
    /// <summary>Puts a function named <paramref name="name"/> on <paramref name="target"/>.</summary>
    /// <param name="movie">
    /// The movie that creates the function, which is <c>UiMovie</c> on the menu being extended.
    /// </param>
    /// <param name="target">
    /// The ActionScript object that takes the function as a member, usually the movie's <c>_root</c>.
    /// </param>
    /// <param name="name">The name ActionScript uses for it.</param>
    /// <param name="handler">
    /// The delegate that runs for each call. <c>Args.Get()</c> returns the arguments the movie passed, and writing into <c>RetVal</c> sends a value back.
    /// Leaving <c>RetVal</c> alone returns undefined.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="handler"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">The handler could not be allocated, or setting the member on the object returned false.</exception>
    void Register(IGFxMovie movie, GFxValue target, string name, Action<GFxFunctionHandler.Params> handler);

    /// <summary>Raised as the game finishes each movie.</summary>
    /// <remarks>
    /// The game builds a menu's movie each time the menu opens, so a function is registered again on each one.
    /// A handler may call the game, and may call <see cref="Register"/> for the movie it was given.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The running SKSE has no Scaleform interface.</exception>
    IGameEvent<ScaleformMovieCreatedEventArgs> OnMovieCreated { get; }
}
