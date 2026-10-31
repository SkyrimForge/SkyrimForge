using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SkyrimForge.Papyrus;

internal static class PapyrusScriptRegistration
{
    // SkyrimForge.PapyrusScripts writes the same methods into the script's .psc.
    private static IEnumerable<MethodInfo> GetNativeMethods(Type scriptType) =>
        scriptType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(method => !method.IsSpecialName);

    internal static void Register(IPapyrusFunctionRegistrar registrar, object script)
    {
        Type scriptType = script.GetType();
        Type? extendedType = scriptType.GetCustomAttribute<PapyrusScriptAttribute>()?.Extends;
        foreach (MethodInfo method in GetNativeMethods(scriptType))
        {
            ParameterInfo[] parameters = method.GetParameters();
            bool receivesSelf = parameters.FirstOrDefault()?.IsDefined(typeof(PapyrusSelfAttribute)) == true;
            int firstArgumentIndex = receivesSelf ? 1 : 0;
            Func<PapyrusValue, object?>[] readers = [.. parameters.Select(parameter => GetReader(parameter.ParameterType))];
            PapyrusType[] argumentTypes = [.. parameters.Skip(firstArgumentIndex).Select(parameter => GetPapyrusType(parameter.ParameterType))];
            PapyrusType returnType = method.ReturnType == typeof(void) ? PapyrusType.None : GetPapyrusType(method.ReturnType);
            object? target = method.IsStatic ? null : script;
            void InvokeMethod(PapyrusCall call)
            {
                // A loop, because a lambda cannot capture the call.
                object?[] arguments = new object?[readers.Length];
                if (receivesSelf)
                {
                    arguments[0] = readers[0](call.Self);
                }
                for (int i = firstArgumentIndex; i < readers.Length; i++)
                {
                    arguments[i] = readers[i](call.GetArgument(i - firstArgumentIndex));
                }
                object? result = method.Invoke(target, BindingFlags.DoNotWrapExceptions, null, arguments, null);
                if (method.ReturnType != typeof(void))
                {
                    call.ReturnAs(result);
                }
            }

            if (receivesSelf)
            {
                Type selfType = extendedType ?? throw new InvalidOperationException($"{method.Name} declares a [PapyrusSelf] parameter, but {scriptType.Name} extends no form type. Set PapyrusScriptAttribute.Extends.");
                registrar.RegisterInstance(scriptType.Name, GetPapyrusType(selfType).ObjectType, returnType, method.Name, argumentTypes, InvokeMethod);
            }
            else
            {
                registrar.RegisterStatic(scriptType.Name, returnType, method.Name, argumentTypes, InvokeMethod);
            }
        }
    }

    private static PapyrusType GetPapyrusType(Type type) =>
        (PapyrusType)
        typeof(PapyrusType)
        .GetMethod(nameof(PapyrusType.FromType))!.MakeGenericMethod(type)
        .Invoke(null, BindingFlags.DoNotWrapExceptions, null, null, null)!;

    private static Func<PapyrusValue, object?> GetReader(Type type) =>
        typeof(Reader<>)
        .MakeGenericType(type)
        .GetMethod(nameof(Reader<>.Read), BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Func<PapyrusValue, object?>>();

    // Reflection cannot call PapyrusValue.As directly, because PapyrusValue is a ref struct.
    private static class Reader<T>
    {
        internal static object? Read(PapyrusValue value) => value.As<T>();
    }
}
