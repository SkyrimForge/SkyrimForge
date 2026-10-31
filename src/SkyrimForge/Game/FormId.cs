using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace SkyrimForge.Game;

/// <summary>The ID of a form of type <typeparamref name="TForm"/>.</summary>
/// <remarks>
/// The type is not checked. Reading <c>Form</c> throws for a form of another type, and <c>FormOrNull</c> returns null.
/// A form ID encodes the load-order position of the .esm, .esp, or .esl file declaring the form,
/// so store one beyond the running process only in a saved-state form entry or in JSON that System.Text.Json writes into saved-state text.
/// Compute the ID of a form in a named file with <c>TESDataHandler.GetFormId</c> rather than writing it as a constant.
/// The game can assign the ID of a deleted runtime-created form to another form.
/// </remarks>
/// <param name="Value">The number the game identifies the form by.</param>
[JsonConverter(typeof(FormIdJsonConverter))]
public readonly record struct FormId<TForm>(uint Value) : IComparable<FormId<TForm>> where TForm : struct, IForm
{
    /// <summary>Orders IDs by their number.</summary>
    public int CompareTo(FormId<TForm> other) => Value.CompareTo(other.Value);

    /// <summary>Converts <paramref name="id"/> to the ID of a form of any type.</summary>
    public static implicit operator FormId<Form>(FormId<TForm> id) => new(id.Value);

    /// <summary>Converts <paramref name="id"/> to the ID of a <typeparamref name="TForm"/> without checking the form's type.</summary>
    public static explicit operator FormId<TForm>(FormId<Form> id) => new(id.Value);

    internal FormId<TOther> Cast<TOther>() where TOther : struct, IForm => new(Value);

    /// <summary>Whether both IDs identify the same form.</summary>
    public static bool operator ==(FormId<TForm> left, FormId<Form> right) => left.Value == right.Value;

    /// <summary>Whether the IDs identify different forms.</summary>
    public static bool operator !=(FormId<TForm> left, FormId<Form> right) => left.Value != right.Value;

    /// <summary>Whether both IDs are null or identify the same form.</summary>
    public static bool operator ==(FormId<TForm>? left, FormId<Form>? right) => left?.Value == right?.Value;

    /// <summary>Whether one ID alone is null or the IDs identify different forms.</summary>
    public static bool operator !=(FormId<TForm>? left, FormId<Form>? right) => left?.Value != right?.Value;

    /// <summary>Returns the ID as eight hexadecimal digits.</summary>
    public override string ToString() => Value.ToString("X8", CultureInfo.InvariantCulture);
}
