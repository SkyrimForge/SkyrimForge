using SkyrimForge.Game;
using System;
using System.Runtime.InteropServices;

namespace SkyrimForge.Papyrus;

/// <summary>
/// One event a plugin publishes to Papyrus, and the objects registered to receive it. Registrations are written into the co-save and restored on load.
/// </summary>
/// <remarks>
/// <code>
/// // The script declares the event and a native to register with:
/// //   Event OnGoldSpent(int amount)
/// //   Function RegisterForGoldSpent(Form target) global native
/// PapyrusEvent spent = events.Declare("OnGoldSpent");
/// papyrus.RegisterStatic("MyScript", "RegisterForGoldSpent", (Form target) =&gt; { spent.Register(target); });
/// spent.Send(new PapyrusArguments().Add(120));
/// </code>
/// </remarks>
public sealed class PapyrusEvent
{
    private readonly nint registrationSet;

    private readonly string? scriptName;

    internal PapyrusEvent(string name, string? scriptName, nint registrationSet)
    {
        Name = name;
        this.scriptName = scriptName;
        this.registrationSet = registrationSet;
    }

    /// <summary>The name the script declares for the event.</summary>
    public string Name { get; }

    /// <summary>How many objects are registered.</summary>
    public int RegistrationCount => NativePapyrus.SF_PapyrusEvents_Count(registrationSet);

    /// <summary>Registers a form, so scripts attached to it receive this event.</summary>
    /// <returns>Whether this registered it, false if it was registered already or the game could give no handle for it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is <c>null</c>.</exception>
    public bool Register<TForm>(TForm target) where TForm : IForm
    {
        ArgumentNullException.ThrowIfNull(target);
        return NativePapyrus.SF_PapyrusEvents_AddForm(registrationSet, target.Address);
    }

    /// <summary>Registers an active magic effect, so scripts attached to it receive this event.</summary>
    /// <returns>
    /// Whether this registered it, false if it was registered already or the game could give no handle for it.
    /// The game gives an effect a handle only if it has a target and a non-zero <c>UsUniqueID</c>, and many effects have a <c>UsUniqueID</c> of 0.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is <c>null</c>.</exception>
    public bool Register(IActiveMagicEffect target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return NativePapyrus.SF_PapyrusEvents_AddActiveEffect(registrationSet, target.Address);
    }

    /// <summary>Registers a quest alias, so scripts attached to it receive this event.</summary>
    /// <returns>Whether this registered it, false if it was registered already or the game could give no handle for it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is <c>null</c>.</exception>
    public bool Register(IAlias target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return NativePapyrus.SF_PapyrusEvents_AddAlias(registrationSet, target.Address);
    }

    /// <summary>Stops a form receiving this event. Returns whether it was registered.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="target"/> is <c>null</c>.</exception>
    public bool Unregister<TForm>(TForm target) where TForm : IForm
    {
        ArgumentNullException.ThrowIfNull(target);
        return NativePapyrus.SF_PapyrusEvents_RemoveForm(registrationSet, target.Address);
    }

    /// <summary>Stops an active magic effect receiving this event. Returns whether it was registered.</summary>
    /// <inheritdoc cref="Unregister{TForm}(TForm)" path="/exception"/>
    public bool Unregister(IActiveMagicEffect target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return NativePapyrus.SF_PapyrusEvents_RemoveActiveEffect(registrationSet, target.Address);
    }

    /// <summary>Stops a quest alias receiving this event. Returns whether it was registered.</summary>
    /// <inheritdoc cref="Unregister{TForm}(TForm)" path="/exception"/>
    public bool Unregister(IAlias target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return NativePapyrus.SF_PapyrusEvents_RemoveAlias(registrationSet, target.Address);
    }

    /// <summary>Stops everything receiving this event. The game does this itself before a save is loaded and on a new game.</summary>
    public void UnregisterAll() => NativePapyrus.SF_PapyrusEvents_Clear(registrationSet);

    /// <summary>Raises the event on every registered object.</summary>
    /// <param name="arguments">
    /// The arguments the event declares, in order, or <c>null</c> for one declaring none. Arguments can be passed once, so create them for each raising.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// No Papyrus VM exists.
    /// <paramref name="arguments"/> was passed to a call already, or contains a form if the event was declared by name alone.
    /// For an event declared with a script, the script does not declare the event or the arguments do not match its declaration.
    /// </exception>
    public void Send(PapyrusArguments? arguments = null)
    {
        arguments?.Consume();
        VirtualMachineArgumentList virtualMachineArgumentList = (arguments ?? new PapyrusArguments()).Build();
        try
        {
            if (scriptName != null)
            {
                virtualMachineArgumentList.Bind(scriptName, Name);
            }
            else if (virtualMachineArgumentList.ContainsForms)
            {
                throw new InvalidOperationException($"{Name} was declared by name alone, so a form argument cannot be bound to a class. Declare the event with the name of the script that declares it.");
            }
            if (!NativePapyrus.SF_PapyrusEvents_Send(registrationSet, virtualMachineArgumentList.NativeArguments.Address))
            {
                throw new InvalidOperationException($"No Papyrus virtual machine exists, so {Name} was not raised.");
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
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint SF_PapyrusEvents_Declare(string eventName);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_PapyrusEvents_AddForm(nint registrationSet, nint form);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_PapyrusEvents_AddAlias(nint registrationSet, nint alias);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_PapyrusEvents_AddActiveEffect(nint registrationSet, nint effect);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_PapyrusEvents_RemoveForm(nint registrationSet, nint form);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_PapyrusEvents_RemoveAlias(nint registrationSet, nint alias);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_PapyrusEvents_RemoveActiveEffect(nint registrationSet, nint effect);

    [LibraryImport(library)]
    internal static partial void SF_PapyrusEvents_Clear(nint registrationSet);

    [LibraryImport(library)]
    internal static partial int SF_PapyrusEvents_Count(nint registrationSet);

    [LibraryImport(library)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool SF_PapyrusEvents_Send(nint registrationSet, nint arguments);
}
