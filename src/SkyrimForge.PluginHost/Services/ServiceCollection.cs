using SkyrimForge.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkyrimForge.PluginHost.Services;

internal sealed class ServiceCollection(ServiceCollection? fallbackCollection = null) : IServiceRegistrar
{
    private readonly Dictionary<Type, IServiceDescriptor> serviceDescriptors = [];
    private readonly Dictionary<Type, object> cachedInstances = [];
    // Detects a cycle, which would otherwise overflow the stack and terminate the process.
    private readonly List<Type> servicesUnderConstruction = [];

    public IEnumerable<Type> PublicServiceTypes => serviceDescriptors.Keys.Where(type => type.IsPublic);

    public void Add<T>(Func<T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Type type = typeof(T);
        Add(type, new ServiceDescriptorWithFactory(factory));
    }

    public void Add<T>() where T : class => Add<T, T>();

    public void Add<TService, TImplementation>() where TService : class where TImplementation : class, TService
    {
        Type serviceType = typeof(TService);
        Type implementationType = typeof(TImplementation);
        Add(serviceType, new ServiceDescriptorWithType(implementationType));
    }

    private void Add(Type serviceType, IServiceDescriptor serviceDescriptor)
    {
        if (!serviceDescriptors.TryAdd(serviceType, serviceDescriptor))
        {
            throw new ArgumentException($"{serviceType.FullName} is already registered.");
        }
    }

    private IServiceDescriptor? GetDescriptor(Type serviceType)
    {
        return serviceDescriptors.TryGetValue(serviceType, out IServiceDescriptor? descriptor) ? descriptor : null;
    }

    public object? GetConstructedInstance(Type serviceType)
    {
        return cachedInstances.GetValueOrDefault(serviceType);
    }

    public T? GetConstructedInstance<T>()
    {
        return (T?)GetConstructedInstance(typeof(T));
    }

    public object? Get(Type serviceType)
    {
        IServiceDescriptor? descriptor = GetDescriptor(serviceType);
        if (descriptor == null)
        {
            if (fallbackCollection != null)
            {
                return fallbackCollection.Get(serviceType);
            }
            return null;
        }
        object? instance = GetConstructedInstance(serviceType);
        if (instance != null) { return instance; }
        if (servicesUnderConstruction.Contains(serviceType))
        {
            string chain = string.Join(" requires ", servicesUnderConstruction.Append(serviceType).Select(type => type.FullName));
            throw new InvalidOperationException($"{serviceType.FullName} requires itself: {chain}.");
        }
        servicesUnderConstruction.Add(serviceType);
        try
        {
            instance = descriptor.Construct(this);
        }
        finally
        {
            servicesUnderConstruction.Remove(serviceType);
        }
        cachedInstances.Add(serviceType, instance);
        return instance;
    }

    public object GetRequired(Type serviceType)
    {
        object? instance = Get(serviceType);
        if (instance != null) { return instance; }
        throw new ArgumentException(serviceType.FullName + " was not registered.", nameof(serviceType));
    }

    public T GetRequired<T>()
    {
        return (T)GetRequired(typeof(T));
    }

    public object Construct(Type constructibleType)
    {
        return Construct(constructibleType, true);
    }

    internal object Construct(Type constructibleType, bool includeOnlyPublic)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        // CoSave's constructor is internal.
        if (!includeOnlyPublic) { flags |= BindingFlags.NonPublic; }
        ConstructorInfo[] constructors = constructibleType.GetConstructors(flags);
        if (constructors.Length != 1)
        {
            string message = $"{constructibleType.FullName} must have exactly one {(includeOnlyPublic ? "public " : "")}constructor. It has {constructors.Length}.";
            throw new ArgumentException(message, nameof(constructibleType));
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
