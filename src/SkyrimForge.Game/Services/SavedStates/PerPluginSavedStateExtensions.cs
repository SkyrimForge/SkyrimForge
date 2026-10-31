namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="IPerPluginSavedState"/>.</summary>
public static class PerPluginSavedStateExtensions
{
    extension(IPerPluginSavedState savedState)
    {
        /// <summary>Returns the same store as <see cref="IPerPluginSavedState.GetFormEntries(FormId{Form})"/>.</summary>
        public ISavedStateEntries GetFormEntries<TForm>(TForm form) where TForm : struct, IForm => savedState.GetFormEntries(form.FormId);
    }
}
