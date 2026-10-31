using System;

namespace SkyrimForge.Game;

/// <summary>Provides extension members for <see cref="FormId{TForm}"/> and <see cref="IForm"/>.</summary>
public static class FormIdExtensions
{
    extension<TForm>(FormId<TForm> id) where TForm : struct, IForm
    {
        /// <summary>The form the ID identifies, which each read looks up.</summary>
        /// <exception cref="InvalidOperationException">No form has the ID, or the form is not a <typeparamref name="TForm"/>.</exception>
        /// <exception cref="ArgumentException"><see cref="FormCasts"/> declares no cast to <typeparamref name="TForm"/>.</exception>
        public TForm Form
        {
            get
            {
                Form form = Form.LookupByID(id.Value) ?? throw new InvalidOperationException($"No form resolves to {id}.");
                return FormCasts.CastOrNull<Form, TForm>(form) ?? throw new InvalidOperationException($"{id} resolves to a form that is not a {typeof(TForm).Name}.");
            }
        }

        /// <summary>
        /// The form the ID identifies, which each read looks up. Null means no form has the ID or the form is not a <typeparamref name="TForm"/>.
        /// </summary>
        /// <exception cref="ArgumentException"><see cref="FormCasts"/> declares no cast to <typeparamref name="TForm"/>.</exception>
        public TForm? FormOrNull => Form.LookupByID(id.Value) is { } form ? FormCasts.CastOrNull<Form, TForm>(form) : null;
    }

    extension<TForm>(TForm form) where TForm : struct, IForm
    {
        /// <summary>The form's ID, declared as the form's view type.</summary>
        public FormId<TForm> FormId => new(form.FormID);
    }

    // The receiver is not generic, because explicit type arguments on a member of a generic extension block must include the block's type arguments.
    extension(IForm form)
    {
        /// <summary>Does not check that the form is a <typeparamref name="TView"/>.</summary>
        internal FormId<TView> GetFormId<TView>() where TView : struct, IForm => new(form.FormID);
    }
}
