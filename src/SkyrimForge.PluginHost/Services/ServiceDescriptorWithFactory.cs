using System;

namespace SkyrimForge.PluginHost.Services;

internal sealed class ServiceDescriptorWithFactory(Func<object> constructor) : IServiceDescriptor
{
    public object Construct(ServiceCollection serviceCollection) => constructor();
}
