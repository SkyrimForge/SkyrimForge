using System;

namespace SkyrimForge.Hosting;

/// <summary>Registers services for dependency injection.</summary>
/// <remarks>
/// Each plugin has its own registrations, so a registered service is constructed once for each plugin that declares it.
/// The framework provides some services, such as <see cref="ILog"/> and <see cref="IGameEvents"/>, to each plugin, and registering one throws.
/// Registering one the framework shares among plugins, such as <see cref="IUtility"/>, replaces it for that plugin and for the services constructed for that plugin.
/// The services the framework shares are constructed with the original.
/// </remarks>
public interface IServiceRegistrar
{
    /// <summary>Registers a service that <paramref name="factory"/> constructs.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is registered already, or the framework provides it to each plugin.</exception>
    void Add<T>(Func<T> factory) where T : class;

    /// <summary>Registers a service the framework constructs through the type's one constructor, whose parameters are services too.</summary>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> is registered already, or the framework provides it to each plugin.</exception>
    void Add<T>() where T : class;

    /// <summary>Registers a service the framework constructs through the one constructor of <typeparamref name="TImplementation"/>, whose parameters are services too.</summary>
    /// <exception cref="ArgumentException"><typeparamref name="TService"/> is registered already, or the framework provides it to each plugin.</exception>
    void Add<TService, TImplementation>() where TService : class where TImplementation : class, TService;
}
