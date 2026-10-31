namespace SkyrimForge.PluginHost.Services;

internal interface IServiceDescriptor
{
    object Construct(ServiceCollection serviceCollection);
}
