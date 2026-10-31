using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>Arguments for <see cref="IScaleformFunctionRegistrar.OnMovieCreated"/>.</summary>
/// <param name="Movie">The movie the game finished.</param>
/// <param name="PluginObject">
/// The ActionScript object SKSE makes for the framework, not the movie's own root.
/// Every plugin receives the same object, so a member one plugin sets replaces another's of the same name.
/// </param>
public readonly record struct ScaleformMovieCreatedEventArgs(GFxMovieView Movie, GFxValue PluginObject);
