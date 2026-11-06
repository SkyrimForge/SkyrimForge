using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        foreach (MethodInfo method in GetNativeMethods(scriptType))
        {
            ParameterInfo[] parameters = method.GetParameters();
            bool receivesSelf = parameters.FirstOrDefault()?.IsDefined(typeof(PapyrusSelfAttribute)) == true;
            Type bodyType = Expression.GetDelegateType([.. parameters.Select(parameter => parameter.ParameterType), method.ReturnType]);
            MethodInfo overload = typeof(PapyrusFunctionRegistrarExtensions).GetMethods().Single(candidate =>
            {
                if (candidate.Name == (receivesSelf ? nameof(PapyrusFunctionRegistrarExtensions.RegisterInstance) : nameof(PapyrusFunctionRegistrarExtensions.RegisterStatic)))
                {
                    // The body's type name (such as Func`3 or Action`2) identifies the overload.
                    ParameterInfo lastParameter = candidate.GetParameters()[^1];
                    return lastParameter.ParameterType.Name == bodyType.Name;
                }
                return false;
            });
            Delegate body = method.CreateDelegate(bodyType, method.IsStatic ? null : script);
            (overload.IsGenericMethodDefinition ? overload.MakeGenericMethod(bodyType.GetGenericArguments()) : overload)
                .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [registrar, scriptType.Name, method.Name, body], null);
        }
    }
}
