using System;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="Form"/>.</summary>
public static class FormExtensions
{
    // Declared on Form rather than on every IForm, since ObjectReferenceBindings declares this member for every IObjectReference, and two would make the call ambiguous.
    extension(Form form)
    {
        /// <summary>Determines whether the form has <paramref name="keyword"/>, as Papyrus's Form.HasKeyword does.</summary>
        /// <returns><c>true</c> if the form has the keyword. A form of a type with no keywords returns <c>false</c>.</returns>
        /// <remarks>Convert a view whose type declares no HasKeyword, such as Armor, to <see cref="Form"/> first. Call on the game's thread.</remarks>
        public bool HasKeyword<TKeyword>(TKeyword keyword) where TKeyword : IKeyword =>
            form.AsBGSKeywordForm()?.HasKeyword(keyword) ?? form.AsReference()?.HasKeyword(keyword) ?? false;
    }

    extension(Form)
    {
        /// <summary>Gets the form with <paramref name="editorId"/>.</summary>
        /// <param name="editorId">The editor ID.</param>
        /// <returns>The form.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="editorId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">No form has <paramref name="editorId"/>.</exception>
        /// <remarks>
        /// The game retains editor IDs only for keywords and a few other form types.
        /// <see cref="FormBindings.LookupByEditorID(string)"/> returns null instead of throwing.
        /// </remarks>
        public static Form GetByEditorId(string editorId)
        {
            ArgumentNullException.ThrowIfNull(editorId);
            return Form.LookupByEditorID(editorId)
                ?? throw new InvalidOperationException($"No form has the editor ID \"{editorId}\". The game retains editor IDs only for keywords and a few other form types.");
        }

        /// <summary>Gets the form with <paramref name="editorId"/> as a <typeparamref name="TForm"/>.</summary>
        /// <param name="editorId">The editor ID.</param>
        /// <returns>The form.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="editorId"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">No form has <paramref name="editorId"/>, or the form is not a <typeparamref name="TForm"/>.</exception>
        /// <exception cref="ArgumentException"><see cref="FormCasts"/> declares no cast to <typeparamref name="TForm"/>.</exception>
        /// <remarks>
        /// The game retains editor IDs only for keywords and a few other form types.
        /// <see cref="FormBindings.LookupByEditorID(string)"/> returns null instead of throwing.
        /// </remarks>
        public static TForm GetByEditorId<TForm>(string editorId) where TForm : struct, IForm =>
            FormCasts.CastOrNull<Form, TForm>(Form.GetByEditorId(editorId))
                ?? throw new InvalidOperationException($"The editor ID \"{editorId}\" resolves to a form that is not a {typeof(TForm).Name}.");
    }
}
