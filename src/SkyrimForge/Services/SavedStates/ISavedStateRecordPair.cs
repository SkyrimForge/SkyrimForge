using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>One record in a saved-state store, read and written as <typeparamref name="T"/> under one key.</summary>
/// <remarks>
/// The record is stored as System.Text.Json writes it.
/// Each <see cref="FormId{TForm}"/> in it, at any depth, is mapped through the load order a save recorded, as <see cref="ISavedStateEntries.SetString"/> describes.
/// A form whose .esm, .esp, or .esl file is no longer loaded is stored as a JSON null,
/// which a <see cref="FormId{TForm}"/> property reads as the ID 0 and a nullable one reads as null.
/// Enums are stored as their integers.
/// Members can be called on any thread.
/// </remarks>
public interface ISavedStateRecordPair<T> where T : class
{
    /// <summary>The key this reads and writes within its store.</summary>
    string Key { get; }

    /// <summary>
    /// The stored record. It is null if nothing is stored under <see cref="Key"/>.
    /// Assigning null drops the key.
    /// Each read deserializes the stored JSON into a new object, so a change to that object is stored only by assigning the object.
    /// </summary>
    /// <exception cref="System.Text.Json.JsonException">The stored JSON does not deserialize as <typeparamref name="T"/>.</exception>
    T? Value { get; set; }
}
