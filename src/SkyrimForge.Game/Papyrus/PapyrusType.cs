using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;

namespace SkyrimForge.Papyrus;

/// <summary>A type in a native function's signature, as the script declares it.</summary>
public readonly struct PapyrusType
{
    private PapyrusType(TypeInfo.RawType rawType, FormType objectType)
    {
        Raw = rawType;
        ObjectType = objectType;
    }

    /// <summary>The return type of a function declaring none. Papyrus has no None parameter.</summary>
    public static PapyrusType None => new(TypeInfo.RawType.kNone, FormType.None);

    /// <summary>The type of an int parameter or return value.</summary>
    public static PapyrusType Int => new(TypeInfo.RawType.kInt, FormType.None);

    /// <summary>The type of a float parameter or return value.</summary>
    public static PapyrusType Float => new(TypeInfo.RawType.kFloat, FormType.None);

    /// <summary>The type of a bool parameter or return value.</summary>
    public static PapyrusType Bool => new(TypeInfo.RawType.kBool, FormType.None);

    /// <summary>The type of a string parameter or return value.</summary>
    public static PapyrusType String => new(TypeInfo.RawType.kString, FormType.None);

    /// <summary>
    /// Builds the type for a form of <paramref name="objectType"/>,
    /// such as <c>ObjectReference</c> for <see cref="FormType.Reference"/> or <c>Actor</c> for <see cref="FormType.ActorCharacter"/>.
    /// </summary>
    public static PapyrusType Form(FormType objectType)
    {
        return new PapyrusType(TypeInfo.RawType.kObject, objectType);
    }

    /// <summary>Builds the type for an array of <paramref name="elementType"/>, such as <c>Int[]</c> or <c>Form[]</c>.</summary>
    /// <exception cref="NotSupportedException"><paramref name="elementType"/> is an array or None.</exception>
    public static PapyrusType MakeArrayType(PapyrusType elementType) => elementType.Raw switch
    {
        TypeInfo.RawType.kInt => new PapyrusType(TypeInfo.RawType.kIntArray, FormType.None),
        TypeInfo.RawType.kFloat => new PapyrusType(TypeInfo.RawType.kFloatArray, FormType.None),
        TypeInfo.RawType.kBool => new PapyrusType(TypeInfo.RawType.kBoolArray, FormType.None),
        TypeInfo.RawType.kString => new PapyrusType(TypeInfo.RawType.kStringArray, FormType.None),
        TypeInfo.RawType.kObject => new PapyrusType(TypeInfo.RawType.kObjectArray, elementType.ObjectType),
        _ => throw new NotSupportedException($"Papyrus has no array of {elementType.Raw}.")
    };

    /// <summary>
    /// Gets the Papyrus type of <typeparamref name="T"/>, which is <c>int</c>, <c>float</c>, <c>bool</c>, <c>string</c>, a form view, or an array of one of those.
    /// A nullable type maps to the type it wraps.
    /// </summary>
    /// <exception cref="TypeInitializationException">
    /// Papyrus has no such type, wrapping the <see cref="NotSupportedException"/> that identifies it.
    /// </exception>
    public static PapyrusType FromType<T>() => PapyrusMarshalling<T>.DeclaredParameterType;

    internal static FormType GetReceiverType<TSelf>()
    {
        PapyrusType type = FromType<TSelf>();
        return type.Raw == TypeInfo.RawType.kObject
            ? type.ObjectType
            : throw new ArgumentException($"{typeof(TSelf)} is not a form view and cannot be the receiver of an instance function.");
    }

    internal TypeInfo.RawType Raw { get; }

    internal FormType ObjectType { get; }

    internal bool IsArray => Raw is TypeInfo.RawType.kIntArray or TypeInfo.RawType.kFloatArray or TypeInfo.RawType.kBoolArray or TypeInfo.RawType.kStringArray or TypeInfo.RawType.kObjectArray;
}
