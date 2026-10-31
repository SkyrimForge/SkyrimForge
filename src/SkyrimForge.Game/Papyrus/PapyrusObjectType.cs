using SkyrimForge.Game;
using System;

namespace SkyrimForge.Papyrus;

// The form type cannot be read from the C# type at runtime, so the generator writes the pairing into PapyrusObjectTypes.Generated.cs.
internal sealed record PapyrusObjectType(Type View, FormType FormType, Func<nint, object?> CreateView)
{
    internal static PapyrusObjectType Create<TView>(FormType formType) where TView : struct =>
        new(typeof(TView), formType, pointer => Form.OrNull(pointer) is { } form ? FormCasts.CastOrNull<Form, TView>(form) : null);

    internal static PapyrusObjectType FromView(Type view) => PapyrusObjectTypes.Known.TryGetValue(view, out PapyrusObjectType? objectType)
            ? objectType : throw new NotSupportedException($"{view} is not a Papyrus type. A parameter or return value is int, float, bool, string, or a form view such as ObjectReference or Actor.");
}
