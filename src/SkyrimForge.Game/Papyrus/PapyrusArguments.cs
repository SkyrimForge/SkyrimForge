using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SkyrimForge.Papyrus;

/// <summary>Arguments for a Papyrus call, built one at a time.</summary>
/// <remarks>Adding to an instance already passed to a call, or passing it to a second call, throws <see cref="InvalidOperationException"/>.</remarks>
public sealed class PapyrusArguments
{
    private const int maxArrayLength = 128;

    private readonly List<Action<VirtualMachineArgumentList>> recordedArguments = [];

    private bool consumed;

    /// <summary>Creates an empty set of arguments for one call.</summary>
    public PapyrusArguments() { }

    internal static void ThrowIfArrayTooLong<T>(IReadOnlyCollection<T>? values, [CallerArgumentExpression(nameof(values))] string? paramName = null) =>
        ThrowIfArrayTooLong(values?.Count ?? 0, paramName);

    internal static void ThrowIfArrayTooLong(int length, string? paramName)
    {
        if (length > maxArrayLength)
        {
            throw new ArgumentOutOfRangeException(paramName, length, $"A Papyrus array stores at most {maxArrayLength} entries.");
        }
    }

    internal void Consume()
    {
        ThrowIfConsumed();
        consumed = true;
    }

    private void ThrowIfConsumed()
    {
        if (consumed)
        {
            throw new InvalidOperationException("These Papyrus arguments were already passed to a call. Create new arguments for each call.");
        }
    }

    // The caller frees the returned list, or passes it to a callback that frees it.
    internal VirtualMachineArgumentList Build()
    {
        VirtualMachineArgumentList virtualMachineArgumentList = VirtualMachineArgumentList.Allocate();
        try
        {
            foreach (Action<VirtualMachineArgumentList> recordedArgument in recordedArguments)
            {
                recordedArgument(virtualMachineArgumentList);
            }
        }
        catch (Exception)
        {
            virtualMachineArgumentList.Free();
            throw;
        }
        return virtualMachineArgumentList;
    }

    private PapyrusArguments Record(Action<VirtualMachineArgumentList> recordedArgument)
    {
        ThrowIfConsumed();
        recordedArguments.Add(recordedArgument);
        return this;
    }

    /// <summary>Appends an argument left unset, which Papyrus reads as None.</summary>
    public PapyrusArguments AddNone() => Record(virtualMachineArgumentList => virtualMachineArgumentList.Append());

    /// <summary>Appends an int argument.</summary>
    public PapyrusArguments Add(int value) => Record(virtualMachineArgumentList => virtualMachineArgumentList.Append().SetSInt(value));

    /// <summary>Appends a float argument.</summary>
    public PapyrusArguments Add(float value) => Record(virtualMachineArgumentList => virtualMachineArgumentList.Append().SetFloat(value));

    /// <summary>Appends a bool argument.</summary>
    public PapyrusArguments Add(bool value) => Record(virtualMachineArgumentList => virtualMachineArgumentList.Append().SetBool(value));

    /// <summary>Appends a string. Null is appended as the empty string.</summary>
    public PapyrusArguments Add(string? value) => Record(virtualMachineArgumentList => virtualMachineArgumentList.Append().SetString(value ?? ""));

    /// <summary>Appends a form, bound to the class the function declares.</summary>
    public PapyrusArguments Add(IForm? form)
    {
        nint formAddress = form?.Address ?? 0;
        return Record(virtualMachineArgumentList => virtualMachineArgumentList.AppendForm(formAddress));
    }

    /// <summary>Appends an array of forms, bound to the class the parameter declares. Null or an empty sequence arrives in Papyrus as None.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="forms"/> has more than 128 entries.</exception>
    public PapyrusArguments Add<TForm>(IEnumerable<TForm>? forms) where TForm : IForm
    {
        if (forms == null)
        {
            return AddNone();
        }
        nint[] formAddresses = [.. forms.Select(form => form.Address)];
        ThrowIfArrayTooLong(formAddresses, nameof(forms));
        return Record(virtualMachineArgumentList => virtualMachineArgumentList.AppendFormArray(formAddresses));
    }

    /// <summary>Appends an array of ints. Null appends None, not an empty array.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="values"/> has more than 128 entries.</exception>
    public PapyrusArguments Add(IReadOnlyCollection<int>? values) => AddArray(values, TypeInfo.RawType.kIntArray, (entry, value) => entry.SetSInt(value));

    /// <summary>Appends an array of floats. Null appends None, not an empty array.</summary>
    /// <inheritdoc cref="Add(IReadOnlyCollection{int})" path="/exception"/>
    public PapyrusArguments Add(IReadOnlyCollection<float>? values) => AddArray(values, TypeInfo.RawType.kFloatArray, (entry, value) => entry.SetFloat(value));

    /// <summary>Appends an array of bools. Null appends None, not an empty array.</summary>
    /// <inheritdoc cref="Add(IReadOnlyCollection{int})" path="/exception"/>
    public PapyrusArguments Add(IReadOnlyCollection<bool>? values) => AddArray(values, TypeInfo.RawType.kBoolArray, (entry, value) => entry.SetBool(value));

    /// <summary>Appends an array of strings. Null appends None, not an empty array, and a <c>null</c> entry is set as the empty string.</summary>
    /// <inheritdoc cref="Add(IReadOnlyCollection{int})" path="/exception"/>
    public PapyrusArguments Add(IReadOnlyCollection<string?>? values) => AddArray(values, TypeInfo.RawType.kStringArray, (entry, value) => entry.SetString(value ?? ""));

    private PapyrusArguments AddArray<T>(IReadOnlyCollection<T>? values, TypeInfo.RawType arrayRawType, Action<Variable, T> setEntry)
    {
        if (values == null)
        {
            return AddNone();
        }
        ThrowIfArrayTooLong(values);
        T[] entries = [.. values];
        return Record(virtualMachineArgumentList => virtualMachineArgumentList.AppendArray(entries, arrayRawType, setEntry));
    }
}
