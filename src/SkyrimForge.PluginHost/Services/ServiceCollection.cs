using SkyrimForge.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkyrimForge.PluginHost.Services;

internal sealed class ServiceCollection(ServiceCollection? fallbackCollection = null) : IServiceRegistrar
{
    private readonly Dictionary<Type, Func<object>> serviceFactories = [];
    private readonly Dictionary<Type, object> cachedInstances = [];
    // Detects a cycle, which would otherwise overflow the stack and terminate the process.
    private readonly List<Type> servicesUnderConstruction = [];

    public IEnumerable<Type> PublicServiceTypes => serviceFactories.Keys.Where(type => type.IsPublic);

    public void Add<T>(Func<T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Add(typeof(T), factory);
    }

    public void Add<T>() where T : class => Add<T, T>();

    public void Add<TService, TImplementation>() where TService : class where TImplementation : class, TService =>
        Add(typeof(TService), () => Construct(typeof(TImplementation)));

    private void Add(Type serviceType, Func<object> factory)
    {
        if (!serviceFactories.TryAdd(serviceType, factory))
        {
            throw new ArgumentException($"{serviceType.FullName} is already registered.");
        }
    }

    public T? GetConstructedInstance<T>() => (T?)cachedInstances.GetValueOrDefault(typeof(T));

    public object? Get(Type serviceType)
    {
        if (!serviceFactories.TryGetValue(serviceType, out Func<object>? factory))
        {
            return fallbackCollection?.Get(serviceType);
        }
        if (cachedInstances.TryGetValue(serviceType, out object? instance))
        {
            return instance;
        }
        if (servicesUnderConstruction.Contains(serviceType))
        {
            string chain = string.Join(" requires ", servicesUnderConstruction.Append(serviceType).Select(type => type.FullName));
            throw new InvalidOperationException($"{serviceType.FullName} requires itself: {chain}.");
        }
        servicesUnderConstruction.Add(serviceType);
        try
        {
            instance = factory();
        }
        finally
        {
            servicesUnderConstruction.Remove(serviceType);
        }
        cachedInstances.Add(serviceType, instance);
        return instance;
    }

    public T GetRequired<T>() => (T)(Get(typeof(T)) ?? throw new ArgumentException(typeof(T).FullName + " was not registered."));

    public object Construct(Type constructibleType)
    {
        ConstructorInfo[] constructors = constructibleType.GetConstructors();
        if (constructors.Length != 1)
        {
            throw new ArgumentException($"{constructibleType.FullName} must have exactly one public constructor. It has {constructors.Length}.", nameof(constructibleType));
        }
        ConstructorInfo constructor = constructors[0];
        ParameterInfo[] parameters = constructor.GetParameters();
        object[] arguments = [.. parameters.Select(parameter => GetArgument(constructibleType, parameter.ParameterType))];
        return constructor.Invoke(arguments);
    }

    private object GetArgument(Type constructibleType, Type parameterType)
    {
        return Get(parameterType)
            ?? throw new InvalidOperationException(
                $"{constructibleType.FullName} requires {parameterType.FullName}, which is not registered. The services page at https://docs.skyrimforge.net/reference/services.html lists those a constructor can declare.");
    }
}
