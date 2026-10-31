using System;

namespace SkyrimForge.PluginHost.Services;

internal sealed class ServiceDescriptorWithType(Type implementationType) : IServiceDescriptor
{
    public object Construct(ServiceCollection serviceCollection) => serviceCollection.Construct(implementationType, false);
}
