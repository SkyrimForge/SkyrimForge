namespace SkyrimForge.Hosting;

/// <summary>
/// Adds a library's services so plugins can declare them as constructor parameters for dependency injection.
/// </summary>
/// <remarks>
/// The framework loads an implementation from <c>SKSE\Plugins\SkyrimForge\SharedLibraries\&lt;Name&gt;\&lt;Name&gt;.dll</c>, not from the plugin directory,
/// and constructs it through a public parameterless constructor.
/// A project that sets the MSBuild property <c>SkyrimForgeSharedLibrary</c> to <c>true</c> builds its assembly there.
/// </remarks>
public interface IPluginServiceSource
{
    /// <summary>
    /// Adds a library's services. The framework calls this once for each plugin, whether an <see cref="IPlugin"/> class or a class marked <c>[PapyrusScript]</c>.
    /// </summary>
    void AddServices(IServiceRegistrar serviceRegistrar);
}
