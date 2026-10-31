using SkyrimForge.Game;
using System;
using System.Threading.Tasks;

namespace SkyrimForge.Papyrus;

/// <summary>Provides extension members for <see cref="PapyrusScript{T}"/>.</summary>
public static class PapyrusScriptExtensions
{
    extension<T>(PapyrusScript<T> self) where T : IObjectReference
    {
        /// <summary>Adds the item <paramref name="akItemToAdd"/> identifies to this reference's inventory.</summary>
        /// <exception cref="InvalidOperationException">
        /// No form has the ID, or the form is not a <typeparamref name="TForm"/>. The exception is stored in the returned task.
        /// </exception>
        public async Task AddItem<TForm>(FormId<TForm> akItemToAdd, int aiCount = 1, bool abSilent = false) where TForm : struct, IForm
        {
            TForm form = akItemToAdd.Form;
            await self.AddItem(form, aiCount, abSilent);
        }

        /// <summary>Returns how many of the item <paramref name="akItem"/> identifies are in this reference's inventory.</summary>
        /// <exception cref="InvalidOperationException">
        /// No form has the ID, or the form is not a <typeparamref name="TForm"/>. The exception is stored in the returned task.
        /// </exception>
        public async Task<int> GetItemCount<TForm>(FormId<TForm> akItem) where TForm : struct, IForm
        {
            TForm form = akItem.Form;
            return await self.GetItemCount(form);
        }

        /// <summary>Removes the item <paramref name="akItemToRemove"/> identifies from this reference's inventory.</summary>
        /// <exception cref="InvalidOperationException">
        /// No form has the ID, or the form is not a <typeparamref name="TForm"/>. The exception is stored in the returned task.
        /// </exception>
        public async Task RemoveItem<TForm>(FormId<TForm> akItemToRemove, int aiCount = 1, bool abSilent = false, IObjectReference? akOtherContainer = null) where TForm : struct, IForm
        {
            TForm form = akItemToRemove.Form;
            await self.RemoveItem(form, aiCount, abSilent, akOtherContainer);
        }
    }
}
