using System;

namespace SkyrimForge.Game;

internal static class RuntimeForms
{
    // The game creates forms only once its data has loaded.
    internal static Form Create(FormType type) =>
        IFormFactory.GetFormFactoryByType(type)?.Create() ?? throw new InvalidOperationException($"The game's form factory created no {type}.");

    // A created form is written into every save unless kTemporary is set.
    internal static void SetTemporary<TForm>(TForm form, bool temporary) where TForm : struct, IForm
    {
        const uint temporaryFlag = (uint)Form.RecordFlags.RecordFlag.kTemporary;
        form.FormFlags = temporary ? form.FormFlags | temporaryFlag : form.FormFlags & ~temporaryFlag;
    }
}
