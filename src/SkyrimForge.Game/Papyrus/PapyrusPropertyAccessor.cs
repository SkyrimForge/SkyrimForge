using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SkyrimForge.Papyrus;

internal static class PapyrusPropertyAccessor
{
    public static TValue? Get<TValue>(IForm receiver, string className, string propertyName)
    {
        // Read against Form, so a property declaring a narrower class returns the form typed as its base.
        return new PapyrusValue(GetPropertyVariable(receiver, className, propertyName, "read"), FormType.None).As<TValue>();
    }

    public static void Set(IForm receiver, string className, string propertyName, Action<Variable> setPropertyValue)
    {
        setPropertyValue(GetPropertyVariable(receiver, className, propertyName, "write"));
    }

    public static void SetForm(IForm receiver, string className, string propertyName, IForm? value)
    {
        // The class is read first. The other order writes a handle into a variable declared as something else, without a report.
        FormType formClass = GetDeclaredFormClass(className, propertyName, "write");
        NativePapyrus.SF_Papyrus_SetVariableForm(GetPropertyVariable(receiver, className, propertyName, "write").Address, value?.Address ?? 0, formClass);
    }

    public static void SetArray<T>(IForm receiver, string className, string propertyName, IReadOnlyCollection<T>? values, Action<Variable, T> setEntry)
    {
        if (values == null)
        {
            ClearArray(receiver, className, propertyName);
            return;
        }
        WriteArray(className, propertyName, GetPropertyVariable(receiver, className, propertyName, "write"), values, setEntry);
    }

    public static void SetFormArray<TForm>(IForm receiver, string className, string propertyName, IReadOnlyCollection<TForm>? forms) where TForm : IForm
    {
        if (forms == null)
        {
            ClearArray(receiver, className, propertyName);
            return;
        }
        WriteFormArray(className, propertyName, GetPropertyVariable(receiver, className, propertyName, "write"), forms);
    }

    public static void ClearArray(IForm receiver, string className, string propertyName)
    {
        int status = NativePapyrus.SF_Papyrus_ClearPropertyArray(receiver.Address, className, propertyName);
        if (status != 0)
        {
            throw new InvalidOperationException(status switch
            {
                1 => $"There is no Papyrus virtual machine, so {className}.{propertyName} was not cleared.",
                2 => $"That form has no instance of {className} bound to it, so it has no {className}.{propertyName} to clear.",
                3 => $"{className} declares no property called {propertyName}.",
                4 => $"{className}.{propertyName} is a full property and has no storage to clear. Run its accessors through {nameof(PapyrusScriptDispatcher)}.",
                5 => $"{className}.{propertyName} refers to a variable past the ones its object has, so the compiled script is older than the code reading it. Recompile it.",
                6 => $"{className}.{propertyName} is not declared as an array. Clear a form property by passing a null {nameof(IForm)} to {nameof(PapyrusScriptDispatcher)}.{nameof(PapyrusScriptDispatcher.SetProperty)}.",
                _ => $"Clearing {className}.{propertyName} returned the unrecognized status {status}."
            });
        }
    }

    internal static void WriteArray<T>(string className, string propertyName, Variable destination, IReadOnlyCollection<T> values, Action<Variable, T> setEntry)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(setEntry);
        foreach ((Variable entry, T value) in Allocate(className, propertyName, destination, values.Count).Entries.Zip(values))
        {
            setEntry(entry, value);
        }
    }

    internal static void WriteFormArray<TForm>(string className, string propertyName, Variable destination, IReadOnlyCollection<TForm> forms) where TForm : IForm
    {
        ArgumentNullException.ThrowIfNull(forms);
        (IReadOnlyList<Variable> entries, FormType elementType) =
            Allocate(className, propertyName, destination, forms.Count);
        foreach ((Variable entry, TForm form) in entries.Zip(forms))
        {
            NativePapyrus.SF_Papyrus_SetVariableForm(entry.Address, form.Address, elementType);
        }
    }

    private static (IReadOnlyList<Variable> Entries, FormType ElementType) Allocate(string className, string propertyName, Variable destination, int count)
    {
        int status = NativePapyrus.SF_Papyrus_PropertyArray(className, propertyName, destination.Address, (uint)count, out nint array, out FormType elementType);
        if (status != 0 || Array_.OrNull(array) is not { } allocatedArray)
        {
            throw CreateArrayException(status, className, propertyName);
        }

        // Zip against a shorter array would leave entries None without a report.
        IReadOnlyList<Variable> entries = allocatedArray.GetEntries();
        return entries.Count == count ? (entries, elementType) : throw new InvalidOperationException($"{className}.{propertyName} was allocated with {count} entries, but has {entries.Count}.");
    }

    internal static FormType GetDeclaredFormClass(string className, string propertyName, string operation)
    {
        int status = NativePapyrus.SF_Papyrus_PropertyClass(className, propertyName, out FormType declaredClass);
        return status == 0 ? declaredClass : throw CreateClassException(status, className, propertyName, operation);
    }

    private static Variable GetPropertyVariable(IForm receiver, string className, string propertyName, string operation)
    {
        int status = NativePapyrus.SF_Papyrus_PropertySlot(receiver.Address, className, propertyName, out nint variableAddress);
        return (status == 0 ? Variable.OrNull(variableAddress) : null) ?? throw CreatePropertyException(status, className, propertyName, operation);
    }

    private static InvalidOperationException CreatePropertyException(int status, string className, string propertyName, string operation)
    {
        return new InvalidOperationException(status switch
        {
            1 => $"There is no Papyrus virtual machine, so the {operation} of {className}.{propertyName} did not happen.",
            2 => $"That form has no instance of {className} bound to it, so it has no {className}.{propertyName} to {operation}.",
            3 => $"{className} declares no property called {propertyName}.",
            4 => $"{className}.{propertyName} is a full property and has no stored variable to {operation}. "
                + "Run its accessors through " + nameof(PapyrusScriptDispatcher) + ".",
            5 => $"{className}.{propertyName} refers to a variable past the ones its object has, so the compiled script is older than the code reading it. Recompile it.",
            _ => $"An attempt to {operation} {className}.{propertyName} returned the unrecognized status {status}."
        });
    }

    private static InvalidOperationException CreateClassException(int status, string className, string propertyName, string operation)
    {
        return new InvalidOperationException(status switch
        {
            1 => $"There is no Papyrus virtual machine, so the {operation} of {className}.{propertyName} did not happen.",
            2 => $"The Papyrus virtual machine has no script class {className}.",
            3 => $"{className} declares no property called {propertyName}.",
            4 => $"{className}.{propertyName} is not declared as a form, so no form can be written to it. An array of forms is written as an array.",
            5 => $"{className}.{propertyName} declares a class the Papyrus virtual machine cannot name, because it is a script and no form can be one.",
            6 => $"Reading the class {className}.{propertyName} declares faulted.",
            _ => $"Reading the class {className}.{propertyName} declares returned the unrecognized status {status}."
        });
    }

    private static InvalidOperationException CreateArrayException(int status, string className, string propertyName) => new(status switch
        {
            1 => $"There is no Papyrus virtual machine, so {className}.{propertyName} was not written.",
            2 => $"The Papyrus virtual machine has no script class {className}.",
            3 => $"{className} declares no property called {propertyName}.",
            4 => $"{className}.{propertyName} is not declared as an array, so no array can be written to it.",
            5 => $"{className}.{propertyName} declares entries of a class the Papyrus virtual machine cannot name, because it is a script and no form can be one.",
            6 => $"Reading the class the entries of {className}.{propertyName} declare faulted.",
            7 => $"The Papyrus virtual machine did not allocate the array {className}.{propertyName} declares, or allocated one of another length.",
            _ => $"Allocating the array for {className}.{propertyName} returned the unrecognized status {status}."
        });
}

internal static partial class NativePapyrus
{
    // The Papyrus VM's own GetPropertyValue and SetPropertyValue each return true and do nothing.
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_PropertySlot(nint receiver, string className, string propertyName, out nint variableAddress);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_PropertyClass(string className, string propertyName, out FormType typeId);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_PropertyArray(string className, string propertyName, nint variable, uint size, out nint array, out FormType elementType);

    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_ClearPropertyArray(nint receiver, string className, string propertyName);
}
