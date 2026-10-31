using SkyrimForge.Game;
using System;

namespace SkyrimForge.Papyrus;

internal static class PapyrusMarshalling<T>
{
    static PapyrusMarshalling()
    {
        Type marshalledType = typeof(T);

        // IsSZArray, because IsArray also matches multidimensional arrays, which Papyrus cannot declare.
        if (marshalledType.IsSZArray && marshalledType.GetElementType() is { } elementType)
        {
            var (parameterType, readElement) = MapToPapyrus(elementType);
            DeclaredParameterType = PapyrusType.MakeArrayType(parameterType);
            ReadArgument = argument => ReadEntries(elementType, argument, readElement);
        }
        else
        {
            (DeclaredParameterType, ReadArgument) = MapToPapyrus(marshalledType);
        }
    }

    private static (PapyrusType DeclaredParameterType, Func<PapyrusValue, object?> ReadArgument) MapToPapyrus(Type valueType)
    {
        // For int?[], this receives int?, so the array declares Int[] while ReadEntries builds an int?[].
        Type underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        if (underlyingType == typeof(int))
        {
            return (PapyrusType.Int, argument => argument.Int);
        }
        if (underlyingType == typeof(float))
        {
            return (PapyrusType.Float, argument => argument.Float);
        }
        if (underlyingType == typeof(bool))
        {
            return (PapyrusType.Bool, argument => argument.Bool);
        }
        if (underlyingType == typeof(string))
        {
            return (PapyrusType.String, argument => argument.String);
        }

        PapyrusObjectType objectType = PapyrusObjectType.FromView(underlyingType);
        return (PapyrusType.Form(objectType.FormType), argument =>
        {
            nint address = argument.GetFormAddress();
            if (address == 0) { return null; }
            var view = objectType.CreateView(address);
            if (view != null) { return view; }
            throw new InvalidOperationException($"A Papyrus value storing a form of type {Form.OrNull(address)?.GetFormType()} was read as {underlyingType.Name}, which that form is not.");
        });
    }

    private static Array ReadEntries(Type elementType, PapyrusValue argument, Func<PapyrusValue, object?> readEntry)
    {
        PapyrusArray entries = argument.Array();
        Array array = Array.CreateInstance(elementType, entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            object? entry = readEntry(entries[i]);
            // SetValue would write the default for a None entry, which for a form view is a view over address zero.
            if (entry == null && elementType.IsValueType && Nullable.GetUnderlyingType(elementType) == null)
            {
                throw new InvalidOperationException($"Entry {i} of a Papyrus array is None, which {elementType} cannot represent. Declare the entries nullable to accept it.");
            }
            array.SetValue(entry, i);
        }
        return array;
    }

    internal static PapyrusType DeclaredParameterType { get; }

    // Returns object? because an unconstrained T? returns None as default(T) for a value type, which for a form view is a view over address zero.
    // Read through PapyrusValue.As, which tests for null, instead of casting this directly.
    internal static Func<PapyrusValue, object?> ReadArgument { get; }
}
