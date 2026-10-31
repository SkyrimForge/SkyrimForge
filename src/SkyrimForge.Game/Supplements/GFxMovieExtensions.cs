namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="IGFxMovie"/>.</summary>
public static class GFxMovieExtensions
{
    extension<T>(T movie) where T : IGFxMovie
    {
        /// <summary>Sets the variable at <paramref name="pathToVar"/> to an ActionScript Boolean.</summary>
        /// <returns>Whether the variable was set.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="pathToVar"/> is <c>null</c>.</exception>
        public bool SetVariable(string pathToVar, bool value, GFxMovie.SetVarType setType = GFxMovie.SetVarType.kSticky)
        {
            using ScaleformValue booleanValue = ScaleformValue.Create();
            booleanValue.Value.SetBoolean(value);
            return movie.SetVariable(pathToVar, booleanValue.Value, setType);
        }
    }
}
