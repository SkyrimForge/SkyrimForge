using SkyrimForge.Game;
using System.Collections.Generic;
using System.Threading;

namespace SkyrimForge;

/// <summary>The text and form IDs a plugin stores under keys it chooses.</summary>
/// <remarks><see cref="SavedStateEntriesExtensions"/> adds typed entries and collections.</remarks>
public interface ISavedStateEntries
{
    /// <summary>Reads the text stored under <paramref name="key"/>, or null if nothing is stored.</summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    string? GetString(string key);

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>, to be written into the next save. Null drops the key.</summary>
    /// <remarks>
    /// A <see cref="FormId{TForm}"/> that System.Text.Json writes into the text, at any depth, is mapped through the load order the save recorded,
    /// as <see cref="SetFormId"/> describes.
    /// A form whose .esm, .esp, or .esl file is no longer loaded becomes a JSON null, and a dictionary entry keyed by one is removed.
    /// A form ID written as a number is not mapped.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    void SetString(string key, string? value);

    /// <summary>
    /// Reads the form ID stored under <paramref name="key"/>, mapped to this session's load order.
    /// Null if nothing is stored, or if the .esm, .esp, or .esl file declaring that form is no longer loaded.
    /// </summary>
    /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    FormId<Form>? GetFormId(string key);

    /// <summary>Stores a form's ID under <paramref name="key"/>, to be written into the next save. Null drops the key.</summary>
    /// <remarks>
    /// SKSE maps a stored ID through the load order the save recorded.
    /// The same number written through <see cref="SetString"/> skips that mapping and comes to identify a different form.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    void SetFormId(string key, FormId<Form>? formId);

    /// <summary>Returns a copy of the keys that have text, in no order.</summary>
    IReadOnlyCollection<string> GetStringKeys();

    /// <summary>Returns a copy of the keys that have form IDs, in no order.</summary>
    IReadOnlyCollection<string> GetFormIdKeys();

    internal ISavedStateEntries GetEntries(string key);

    internal IReadOnlyCollection<string> GetEntriesKeys();

    internal bool HasEntries(string key);

    internal void RemoveEntries(string key);

    internal void MoveEntries(string sourceKey, string destinationKey);

    // Every member of the store acquires this lock. A container acquires it across the calls of one operation, so concurrent changes do not lose an entry.
    internal Lock EntriesLock { get; }
}
