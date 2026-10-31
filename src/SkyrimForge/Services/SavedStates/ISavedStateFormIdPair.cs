using SkyrimForge.Game;

namespace SkyrimForge;

/// <summary>One form ID in a saved-state store, under one key.</summary>
/// <remarks>
/// The store does not record <typeparamref name="TForm"/>, so the ID read is declared as the type the pair specifies.
/// Members can be called on any thread.
/// </remarks>
public interface ISavedStateFormIdPair<TForm> where TForm : struct, IForm
{
    /// <summary>The key this reads and writes within its store.</summary>
    string Key { get; }

    /// <summary>
    /// The stored form ID, translated into this session's load order. It is null if nothing is stored or the .esm, .esp, or .esl file declaring that form is no longer loaded.
    /// Assigning null drops the key.
    /// </summary>
    FormId<TForm>? Value { get; set; }
}
