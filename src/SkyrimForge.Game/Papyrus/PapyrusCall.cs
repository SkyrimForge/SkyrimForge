using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Generic;

namespace SkyrimForge.Papyrus;

/// <summary>Arguments passed to a native Papyrus function and the variable for its return value.</summary>
/// <remarks>Valid only for the duration of the call, so a value needed afterwards must be copied out.</remarks>
public readonly ref struct PapyrusCall
{
    private readonly StackFrame frame;
    private readonly Variable returnValue;

    private readonly FormType[] objectTypes;

    private readonly FormType returnObjectType;

    private readonly TypeInfo.RawType returnRawType;

    private readonly uint page;

    private readonly Variable self;

    private readonly FormType selfObjectType;

    private readonly bool isLatent;

    internal PapyrusCall(StackFrame frame, Variable returnValue, FormType[] objectTypes, FormType returnObjectType, TypeInfo.RawType returnRawType, Variable self, FormType selfObjectType, bool isLatent)
    {
        this.frame = frame;
        this.returnValue = returnValue;
        this.objectTypes = objectTypes;
        this.returnObjectType = returnObjectType;
        this.returnRawType = returnRawType;
        this.self = self;
        this.selfObjectType = selfObjectType;
        this.isLatent = isLatent;
        page = frame.GetPageForFrame();
    }

    // The shims dereference the variable without checking, so a default call would fault the game instead of throwing.
    private Variable ReturnValue => isLatent ? throw new InvalidOperationException("A latent function returns its value through PapyrusLatentCall.Return, not through PapyrusCall.Return.")
        : returnValue.Exists ? returnValue : throw new InvalidOperationException("The call has no return variable.");

    /// <summary>
    /// The receiver, read as whichever type is requested: <c>call.Self.As&lt;ObjectReference&gt;()</c>.
    /// A function registered through <see cref="IPapyrusFunctionRegistrar.RegisterStatic"/> is static and reads None here.
    /// </summary>
    /// <remarks>The receiver occupies no argument index.</remarks>
    public PapyrusValue Self => self.Exists ? new PapyrusValue(self, selfObjectType) : throw new InvalidOperationException("The call has no receiver.");

    /// <summary>Reads the argument at <paramref name="index"/> as whichever type is requested of it: <c>call.GetArgument(0).Int</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The function was registered with no parameter at that index.</exception>
    public PapyrusValue GetArgument(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, objectTypes.Length);
        Variable variable = frame.GetStackFrameVariable((uint)index, page) ?? throw new InvalidOperationException($"The stack frame has no variable at index {index}.");
        return new PapyrusValue(variable, objectTypes[index]);
    }

    /// <summary>Sets the return value to an int.</summary>
    /// <exception cref="InvalidOperationException">The function was registered as latent, whose value goes through <see cref="PapyrusLatentCall"/>.</exception>
    public void Return(int value) => ReturnValue.SetSInt(value);

    /// <summary>Sets the return value to a float.</summary>
    /// <exception cref="InvalidOperationException">The function was registered as latent, whose value goes through <see cref="PapyrusLatentCall"/>.</exception>
    public void Return(float value) => ReturnValue.SetFloat(value);

    /// <summary>Sets the return value to a bool.</summary>
    /// <exception cref="InvalidOperationException">The function was registered as latent, whose value goes through <see cref="PapyrusLatentCall"/>.</exception>
    public void Return(bool value) => ReturnValue.SetBool(value);

    /// <summary>Sets the return value to a string.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The function was registered as latent, whose value goes through <see cref="PapyrusLatentCall"/>.</exception>
    public void Return(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ReturnValue.SetString(value);
    }

    /// <summary>Sets the return value to a form, bound to the class the function was registered as returning.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The function was registered as latent, whose value goes through <see cref="PapyrusLatentCall"/>.</exception>
    public void Return<TForm>(TForm form) where TForm : IForm
    {
        ArgumentNullException.ThrowIfNull(form);
        NativePapyrus.SF_Papyrus_SetVariableForm(ReturnValue.Address, form.Address, returnObjectType);
    }

    /// <summary>Sets the return value to a form, bound to the class the function was registered as returning, or to None if <paramref name="form"/> is <c>null</c>.</summary>
    /// <exception cref="InvalidOperationException">The function was registered as latent, whose value goes through <see cref="PapyrusLatentCall"/>.</exception>
    public void Return<TForm>(TForm? form) where TForm : struct, IForm
    {
        NativePapyrus.SF_Papyrus_SetVariableForm(ReturnValue.Address, form?.Address ?? 0, returnObjectType);
    }

    /// <summary>Sets the return value to an array.</summary>
    /// <remarks>
    /// Every entry must be of the array type the function was registered as returning: int, float, bool, string, or form.
    /// A <c>null</c> entry reads in the script as the default of that type.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="values"/> has more than 128 entries.</exception>
    /// <exception cref="InvalidOperationException">
    /// An entry is not of the type the function was registered as returning, the function was registered as latent,
    /// or the Papyrus VM would not allocate the array or allocated one of another length.
    /// </exception>
    /// <exception cref="NotSupportedException">An entry is of a type with no Papyrus equivalent.</exception>
    public void Return(Array values)
    {
        ArgumentNullException.ThrowIfNull(values);
        PapyrusArguments.ThrowIfArrayTooLong(values.Length, nameof(values));
        WriteArray(values);
    }

    // Named apart from Return because Return<T>(T) would have the same signature as Return<TForm>(TForm).
    internal void ReturnAs<T>(T value)
    {
        if (value is Array entries)
        {
            WriteArray(entries);
            return;
        }
        Write(ReturnValue, value);
    }

    private void WriteArray(Array values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values.GetValue(i) is { } value && GetArrayType(value) != returnRawType)
            {
                throw new InvalidOperationException($"The function is registered as returning {returnRawType}, and entry {i} of the array returned is a {value.GetType().Name}.");
            }
        }
        Array_ array = Array_.OrNull(NativePapyrus.SF_Papyrus_SetVariableArray(ReturnValue.Address, returnRawType, returnObjectType, (uint)values.Length))
            ?? throw new InvalidOperationException("The Papyrus virtual machine did not allocate an array of "
                + $"{values.Length} entries of the type this function declares. A Papyrus array has at most 128 entries, and only a function declaring an array can return one.");

        IReadOnlyList<Variable> entries = array.GetEntries();
        if (entries.Count != values.Length)
        {
            throw new InvalidOperationException($"An array was requested with {values.Length} entries and allocated with {entries.Count}.");
        }
        for (int i = 0; i < values.Length; i++)
        {
            Write(entries[i], values.GetValue(i));
        }
    }

    private void Write(Variable variable, object? value)
    {
        switch (value)
        {
            case int number: variable.SetSInt(number); break;
            case float number: variable.SetFloat(number); break;
            case bool flag: variable.SetBool(flag); break;
            case string text: variable.SetString(text); break;
            case IForm form:
                NativePapyrus.SF_Papyrus_SetVariableForm(variable.Address, form.Address, returnObjectType);
                break;
            case null: break;
            default: throw CreateUnsupportedTypeException(value);
        }
    }

    private static TypeInfo.RawType GetArrayType(object value) => value switch
    {
        int => TypeInfo.RawType.kIntArray,
        float => TypeInfo.RawType.kFloatArray,
        bool => TypeInfo.RawType.kBoolArray,
        string => TypeInfo.RawType.kStringArray,
        IForm => TypeInfo.RawType.kObjectArray,
        _ => throw CreateUnsupportedTypeException(value)
    };

    private static NotSupportedException CreateUnsupportedTypeException(object value) =>
        new($"{value.GetType()} is not a Papyrus type. A return value is int, float, bool, string, a form view such as ObjectReference or Actor, or an array of those.");
}
