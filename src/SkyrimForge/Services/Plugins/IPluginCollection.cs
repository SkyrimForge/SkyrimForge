namespace SkyrimForge;

/// <summary>Allows one <see cref="IPlugin"/> to look up another to access its public members.</summary>
/// <remarks>
/// Members can be called on any thread.
/// <para>
/// Each plugin loads in an assembly load context of its own, so an interface that one plugin implements and another looks up belongs in a shared library,
/// in <c>SKSE\Plugins\SkyrimForge\SharedLibraries\&lt;Name&gt;\&lt;Name&gt;.dll</c>.
/// A project that sets the MSBuild property <c>SkyrimForgeSharedLibrary</c> to <c>true</c> builds its assembly there.
/// </para>
/// </remarks>
public interface IPluginCollection
{
    /// <summary>Finds the plugin implementing <typeparamref name="T"/>. Do not call before <see cref="IPlugin.Start"/>.</summary>
    /// <exception cref="System.InvalidOperationException">No plugin or multiple plugins implement <typeparamref name="T"/>.</exception>
    T Get<T>() where T : class;

    /// <summary>Finds the plugin implementing <typeparamref name="T"/>, or <c>null</c> if none does. Do not call before <see cref="IPlugin.Start"/>.</summary>
    /// <exception cref="System.InvalidOperationException">Multiple plugins implement <typeparamref name="T"/>.</exception>
    T? GetOrNull<T>() where T : class;
}
