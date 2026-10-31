using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Papyrus;

/// <summary>A script suspended by a latent native. A <see cref="Return()"/> overload resumes it.</summary>
/// <remarks>
/// <code>
/// papyrus.RegisterLatentStatic("MyScript", PapyrusType.Int, "SlowDouble", [PapyrusType.Int],
///     (call, waiting) =&gt;
///     {
///         int value = call.GetArgument(0).Int;
///         tasks.AddTask(() =&gt; waiting.Return(value * 2));
///         return true;
///     });
/// </code>
/// <para>A script whose value never arrives waits until the session ends. A second Return, or one for a script no longer waiting, is not detected.</para>
/// </remarks>
public sealed class PapyrusLatentCall
{
    private readonly uint stackId;

    private readonly FormType returnObjectType;

    internal PapyrusLatentCall(uint stackId, FormType returnObjectType)
    {
        this.stackId = stackId;
        this.returnObjectType = returnObjectType;
    }

    /// <summary>Resumes the script with None, which a latent function declaring no return type returns.</summary>
    /// <exception cref="InvalidOperationException">The Papyrus VM does not exist yet or has been destroyed.</exception>
    public void Return() => Resume(_ => { });

    /// <summary>Resumes the script with an int.</summary>
    /// <inheritdoc cref="Return()" path="/exception"/>
    public void Return(int value) => Resume(returnVariable => returnVariable.SetSInt(value));

    /// <summary>Resumes the script with a float.</summary>
    /// <inheritdoc cref="Return()" path="/exception"/>
    public void Return(float value) => Resume(returnVariable => returnVariable.SetFloat(value));

    /// <summary>Resumes the script with a bool.</summary>
    /// <inheritdoc cref="Return()" path="/exception"/>
    public void Return(bool value) => Resume(returnVariable => returnVariable.SetBool(value));

    /// <summary>Resumes the script with a string.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    /// <inheritdoc cref="Return()" path="/exception"/>
    public void Return(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Resume(returnVariable => returnVariable.SetString(value));
    }

    /// <summary>Resumes the script with a form, bound to the class the function was registered as returning.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <c>null</c>.</exception>
    /// <inheritdoc cref="Return()" path="/exception"/>
    public void Return<TForm>(TForm form) where TForm : IForm
    {
        ArgumentNullException.ThrowIfNull(form);
        Resume(returnVariable => NativePapyrus.SF_Papyrus_SetVariableForm(returnVariable.Address, form.Address, returnObjectType));
    }

    /// <summary>Resumes the script with a form, bound to the class the function was registered as returning, or with None if <paramref name="form"/> is <c>null</c>.</summary>
    /// <inheritdoc cref="Return()" path="/exception"/>
    public void Return<TForm>(TForm? form) where TForm : struct, IForm =>
        Resume(returnVariable => NativePapyrus.SF_Papyrus_SetVariableForm(returnVariable.Address, form?.Address ?? 0, returnObjectType));

    // Managed code obtains a Variable by appending to an argument list.
    private void Resume(Action<Variable> fill)
    {
        VirtualMachineArgumentList virtualMachineArgumentList = VirtualMachineArgumentList.Allocate();
        try
        {
            Variable returnVariable = virtualMachineArgumentList.Append();
            fill(returnVariable);
            if (!NativePapyrus.SF_Papyrus_ReturnLatent(stackId, returnVariable.Address))
            {
                throw new InvalidOperationException("No Papyrus virtual machine exists, so the script was not resumed.");
            }
        }
        finally
        {
            virtualMachineArgumentList.Free();
        }
    }
}

internal static partial class NativePapyrus
{
    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_Papyrus_ReturnLatent(uint stackId, nint value);
}
