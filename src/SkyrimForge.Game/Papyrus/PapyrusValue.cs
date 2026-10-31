using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;

namespace SkyrimForge.Papyrus;

/// <summary>One Papyrus value, read as whichever type is requested: an argument a script passed, a value a function returned, or a property.</summary>
/// <remarks>
/// An argument or receiver is valid only for the duration of the <see cref="PapyrusCall"/> that provided it. Copy a value out to use it after the call.
/// </remarks>
public readonly ref struct PapyrusValue
{
    private readonly Variable variable;

    private readonly FormType objectType;

    internal PapyrusValue(Variable variable, FormType objectType)
    {
        this.variable = variable;
        this.objectType = objectType;
    }

    /// <summary>The shims dereference the variable without checking, so a missing one would fault the game.</summary>
    private Variable Variable => variable.Exists ? variable : throw new InvalidOperationException("The value has no variable.");

    // Every read tests the type first, because Variable's accessors check with an assert that release builds remove, and then read the union regardless.

    /// <summary>The value as an int.</summary>
    /// <exception cref="InvalidOperationException">It stores another type.</exception>
    public int Int => Variable.IsInt() ? Variable.GetSInt() : throw GetTypeMismatchException("Int");

    /// <summary>The value as a float.</summary>
    /// <exception cref="InvalidOperationException">It stores another type.</exception>
    public float Float => Variable.IsFloat() ? Variable.GetFloat() : throw GetTypeMismatchException("Float");

    /// <summary>The value as a bool.</summary>
    /// <exception cref="InvalidOperationException">It stores another type.</exception>
    public bool Bool => Variable.IsBool() ? Variable.GetBool() : throw GetTypeMismatchException("Bool");

    /// <summary>The value as a string, empty if nothing wrote one.</summary>
    /// <exception cref="InvalidOperationException">It stores another type.</exception>
    public string String => Variable.IsString() ? Variable.GetString() : throw GetTypeMismatchException("String");

    /// <summary>The value as a form, or <c>null</c> if it is None.</summary>
    /// <exception cref="InvalidOperationException">It stores a type other than an object.</exception>
    public Form? Form => Game.Form.OrNull(GetFormAddress());

    /// <summary>Reads the value as an array, with no entries if the value is None.</summary>
    /// <exception cref="InvalidOperationException">It stores a type other than an array.</exception>
    public PapyrusArray Array()
    {
        if (!Variable.IsArray() && !Variable.IsNoneArray())
        {
            throw GetTypeMismatchException("an array");
        }

        return new PapyrusArray(Variable.GetArray() is { } array ? array.GetEntries() : [], objectType);
    }

    /// <summary>
    /// Reads the value as <typeparamref name="T"/>, which is <c>int</c>, <c>float</c>, <c>bool</c>, <c>string</c>, a form view, or an array of one of those.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// It stores another type, a form that is not a <typeparamref name="T"/>, an array entry that is not of <typeparamref name="T"/>'s element type,
    /// or it is None and <typeparamref name="T"/> is a non-nullable value type.
    /// </exception>
    /// <exception cref="TypeInitializationException">Papyrus has no such type, wrapping the <see cref="NotSupportedException"/> that identifies it.</exception>
    public T? As<T>()
    {
        object? argumentValue = PapyrusMarshalling<T>.ReadArgument(this);
        if (argumentValue == null && default(T) != null)
        {
            throw new InvalidOperationException($"A Papyrus value containing {TypeName} was read as {typeof(T)}, which cannot represent None. Read it as the nullable type to accept it.");
        }
        return (T?)argumentValue;
    }

    internal nint GetFormAddress()
    {
        if (!(Variable.IsObject() || Variable.IsNoneObject()))
        {
            throw GetTypeMismatchException("an object");
        }
        return Variable.GetObject()?.Resolve((uint)objectType) ?? 0;
    }

    private InvalidOperationException GetTypeMismatchException(string readAs)
    {
        return new InvalidOperationException($"A Papyrus value containing {TypeName} was read as {readAs}. For an argument, the registration and the implementation disagree about the parameter. "
            + "For a return value or a property, the type requested is not the type the script declares.");
    }

    // The None tests precede IsObject and IsArray, which also accept a None variable.
    private string TypeName => Variable.IsInt() ? "Int"
            : Variable.IsFloat() ? "Float"
            : Variable.IsBool() ? "Bool"
            : Variable.IsString() ? "String"
            : Variable.IsNoneObject() ? "None, an object variable with nothing in it"
            : Variable.IsNoneArray() ? "None, an array variable with nothing in it" : Variable.IsObject() ? "an object" : Variable.IsArray() ? "an array" : "an unrecognized type";
}
