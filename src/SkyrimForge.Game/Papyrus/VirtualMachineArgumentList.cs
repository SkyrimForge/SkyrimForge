using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Papyrus;

// A dispatch's callback frees the list passed to it. Other code calls Free.
internal sealed class VirtualMachineArgumentList
{
    private int count;

    private VirtualMachineArgumentList(IFunctionArguments nativeArguments) => NativeArguments = nativeArguments;

    internal static VirtualMachineArgumentList Allocate()
    {
        IFunctionArguments functionArguments = IFunctionArguments.OrNull(NativePapyrus.SF_Papyrus_BeginArguments())
            ?? throw new InvalidOperationException("The Papyrus argument list could not be allocated.");
        return new VirtualMachineArgumentList(functionArguments);
    }

    internal IFunctionArguments NativeArguments { get; }

    internal bool ContainsForms { get; private set; }

    internal void Free() => NativePapyrus.SF_Papyrus_DeleteArguments(NativeArguments.Address);

    // The returned address stays valid as further arguments are appended.
    internal Variable Append()
    {
        count++;
        return Variable.OrNull(NativePapyrus.SF_Papyrus_AppendArgument(NativeArguments.Address)) ?? throw new InvalidOperationException("The Papyrus argument could not be appended.");
    }

    internal void AppendForm(nint form)
    {
        count++;
        if (form != 0)
        {
            ContainsForms = true;
        }
        NativePapyrus.SF_Papyrus_AppendForm(NativeArguments.Address, form);
    }

    internal void AppendFormArray(nint[] forms)
    {
        NativePapyrus.SF_Papyrus_AppendFormArray(NativeArguments.Address, forms, forms.Length);
        count++;
        ContainsForms = true;
    }

    internal void AppendArray<T>(IReadOnlyCollection<T> values, TypeInfo.RawType arrayRawType, Action<Variable, T> setEntry)
    {
        Array_ array = Array_.OrNull(NativePapyrus.SF_Papyrus_AppendArray(NativeArguments.Address, arrayRawType, (uint)values.Count)) ??
            throw new InvalidOperationException($"No {arrayRawType} of {values.Count} entries was allocated. Either no Papyrus virtual machine exists, or it did not allocate the array.");

        count++;

        // Zip stops at the shorter sequence, so a length mismatch would leave entries unset.
        IReadOnlyList<Variable> entries = array.GetEntries();
        if (entries.Count != values.Count)
        {
            throw new InvalidOperationException($"A {arrayRawType} was requested with {values.Count} entries and made with {entries.Count}.");
        }
        foreach ((Variable entry, T value) in entries.Zip(values))
        {
            setEntry(entry, value);
        }
    }

    internal FormType Bind(string className, string functionName)
    {
        // A form bound to any class other than the one its parameter declares makes the Papyrus VM skip the function and return None to the callback.
        int status = NativePapyrus.SF_Papyrus_BindArgumentsFor(NativeArguments.Address, className, functionName, out int declaredCount, out int returnObjectType);
        if (status == 0)
        {
            if (returnObjectType >= 0) { return (FormType)returnObjectType; }

            throw new InvalidOperationException($"Reading the declared return type of {className}.{functionName} faulted.");
        }

        throw new InvalidOperationException(status switch
        {
            1 => "The Papyrus virtual machine is not available.",
            2 => $"The Papyrus virtual machine has no script class {className}.",
            3 => $"Neither {className} nor any class it inherits from has a function {functionName}.",
            4 => $"{className}.{functionName} declares {declaredCount} parameter(s), but {count} argument(s) were passed.",
            5 => $"{className}.{functionName} declares an object parameter of a class the Papyrus virtual machine cannot name, because it is a script and not a form.",
            6 => $"{className}.{functionName} received a form for a parameter that is not an object.",
            7 => $"{className}.{functionName} received a scalar for a parameter that is an object.",
            8 => $"Reading a declared parameter type of {className}.{functionName} faulted.",
            9 => $"{className}.{functionName} received an array of forms for a parameter that is not an array of objects.",
            10 => $"The Papyrus virtual machine did not allocate the array {className}.{functionName} declares, or allocated one of another length.",
            _ => $"Binding the arguments for {className}.{functionName} returned the unrecognized status {status}."
        });
    }
}

internal static partial class NativePapyrus
{
    // BSScript::IFunctionArguments is an abstract C++ class, which managed code can pass but not construct.
    [LibraryImport(library)]
    internal static partial nint SF_Papyrus_BeginArguments();

    [LibraryImport(library)]
    internal static partial void SF_Papyrus_DeleteArguments(nint args);

    [LibraryImport(library)]
    internal static partial nint SF_Papyrus_AppendArgument(nint args);

    // The raw type is the array's, not the entry's. Only the Papyrus VM can allocate a BSScript::Array, which declares no constructor.
    [LibraryImport(library)]
    internal static partial nint SF_Papyrus_AppendArray(nint args, TypeInfo.RawType rawType, uint size);

    [LibraryImport(library)]
    internal static partial void SF_Papyrus_AppendForm(nint args, nint form);

    [LibraryImport(library)]
    internal static partial void SF_Papyrus_AppendFormArray(nint args, [In] nint[] forms, int count);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_BindArgumentsFor(nint args, string className, string functionName, out int declaredCount, out int returnObjectType);

    [LibraryImport(library)]
    internal static partial void SF_Papyrus_SetVariableForm(nint variable, nint form, FormType typeId);

    // The raw type is the array's. The object type is read only for an array of objects.
    [LibraryImport(library)]
    internal static partial nint SF_Papyrus_SetVariableArray(nint variable, TypeInfo.RawType rawType, FormType objectType, uint size);
}
