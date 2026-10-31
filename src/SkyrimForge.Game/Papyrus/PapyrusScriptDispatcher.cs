using SkyrimForge.Game;
using SkyrimForge.Game.BSScript;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SkyrimForge.Papyrus;

/// <summary>Calls the functions and accesses the properties of one Papyrus script, for a script with no generated binding.</summary>
/// <remarks>
/// A string or an array read as a result or a property is never <c>null</c>. Papyrus has no None string, and a None array reads as one with no entries.
/// Every task here completes from work queued on <see cref="ITaskQueue"/>, under the terms that interface documents.
/// </remarks>
public sealed class PapyrusScriptDispatcher
{
    /// <summary>Creates a dispatcher for the script <paramref name="scriptName"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="scriptName"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="scriptName"/> is empty.</exception>
    public PapyrusScriptDispatcher(string scriptName)
    {
        ArgumentException.ThrowIfNullOrEmpty(scriptName);
        ScriptName = scriptName;
    }

    /// <summary>The script these calls name.</summary>
    public string ScriptName { get; }

    private static PapyrusDispatcher Dispatcher => PapyrusDispatcher.Instance;

    /// <summary>Calls a function of this script on a form, reading the return value as <typeparamref name="TValue"/>.</summary>
    /// <returns>
    /// A task with the return value. It faults if the value cannot be read as <typeparamref name="TValue"/>,
    /// and is canceled if the call was destroyed without running.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="arguments"/> was passed to a call already. The exception is stored in the returned task instead if no Papyrus virtual machine exists,
    /// the script or the function does not exist, the arguments do not match the parameters the function declares,
    /// or the form has no instance of the script bound, which <see cref="Bind"/> makes.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="functionName"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="functionName"/> is empty.</exception>
    public Task<TValue?> CallAsync<TValue>(IForm receiver, string functionName, PapyrusArguments? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        arguments?.Consume();
        return Dispatcher.DispatchFunctionCallAsync<TValue>(receiver, ScriptName, functionName, arguments);
    }

    /// <summary>Calls a function whose return value is of no interest.</summary>
    /// <inheritdoc cref="CallAsync{TValue}(IForm, string, PapyrusArguments?)" path="/exception"/>
    public Task CallAsync(IForm receiver, string functionName, PapyrusArguments? arguments = null)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        arguments?.Consume();
        return Dispatcher.DispatchFunctionCallAsync(receiver, ScriptName, functionName, arguments);
    }

    /// <summary>Calls a global (static) function of this script.</summary>
    /// <inheritdoc cref="CallAsync{TValue}(IForm, string, PapyrusArguments?)" path="/returns"/>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="arguments"/> was passed to a call already. The exception is stored in the returned task instead if no Papyrus virtual machine exists,
    /// the script or the function does not exist, or the arguments do not match the parameters the function declares.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="functionName"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="functionName"/> is empty.</exception>
    public Task<TValue?> CallAsync<TValue>(string functionName, PapyrusArguments? arguments = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        arguments?.Consume();
        return Dispatcher.DispatchStaticCallAsync<TValue>(ScriptName, functionName, arguments);
    }

    /// <summary>Calls a global function whose return value is of no interest.</summary>
    /// <inheritdoc cref="CallAsync{TValue}(string, PapyrusArguments?)" path="/exception"/>
    public Task CallAsync(string functionName, PapyrusArguments? arguments = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(functionName);
        arguments?.Consume();
        return Dispatcher.DispatchStaticCallAsync(ScriptName, functionName, arguments);
    }

    /// <summary>Reads an Auto property of this script from a form, for one with no generated binding.</summary>
    /// <remarks>Read a full property through <see cref="CallGetterAsync"/>. Call on the game's thread.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The property could not be read.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty.</exception>
    public TValue? GetProperty<TValue>(IForm receiver, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        return PapyrusPropertyAccessor.Get<TValue>(receiver, ScriptName, propertyName);
    }

    /// <summary>Writes an int into an Auto property of this script on a form.</summary>
    /// <remarks>Write a full property through <see cref="CallSetter(IForm, string, int)"/>. Call on the game's thread.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The property could not be written.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty.</exception>
    public void SetProperty(IForm receiver, string propertyName, int value) => SetProperty(receiver, propertyName, variable => variable.SetSInt(value));

    /// <summary>Writes a float into an Auto property of this script on a form.</summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/exception"/>
    public void SetProperty(IForm receiver, string propertyName, float value) => SetProperty(receiver, propertyName, variable => variable.SetFloat(value));

    /// <summary>Writes a bool into an Auto property of this script on a form.</summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/exception"/>
    public void SetProperty(IForm receiver, string propertyName, bool value) => SetProperty(receiver, propertyName, variable => variable.SetBool(value));

    /// <summary>Writes a string into an Auto property of this script on a form. Null is written as the empty string.</summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/exception"/>
    public void SetProperty(IForm receiver, string propertyName, string? value) => SetProperty(receiver, propertyName, variable => variable.SetString(value ?? ""));

    /// <summary>
    /// Writes a form into an Auto form property of this script on a form. Null writes None.
    /// A literal <c>null</c> matches several overloads and does not compile, so pass <c>(IForm?)null</c>.
    /// </summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/exception"/>
    public void SetProperty(IForm receiver, string propertyName, IForm? value)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        PapyrusPropertyAccessor.SetForm(receiver, ScriptName, propertyName, value);
    }

    /// <summary>Writes an array of ints into an Auto array property of this script on a form. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="values"/> has more than 128 entries.</exception>
    /// <exception cref="InvalidOperationException">The property could not be written.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty.</exception>
    public void SetProperty(IForm receiver, string propertyName, IReadOnlyCollection<int>? values) =>
        SetPropertyArray(receiver, propertyName, values, (entry, value) => entry.SetSInt(value));

    /// <summary>Writes an array of floats into an Auto array property of this script on a form. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void SetProperty(IForm receiver, string propertyName, IReadOnlyCollection<float>? values) =>
        SetPropertyArray(receiver, propertyName, values, (entry, value) => entry.SetFloat(value));

    /// <summary>Writes an array of bools into an Auto array property of this script on a form. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void SetProperty(IForm receiver, string propertyName, IReadOnlyCollection<bool>? values) =>
        SetPropertyArray(receiver, propertyName, values, (entry, value) => entry.SetBool(value));

    /// <summary>
    /// Writes an array of strings into an Auto array property of this script on a form.
    /// Null writes None, not an empty array, and a <c>null</c> entry is written as the empty string.
    /// </summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void SetProperty(IForm receiver, string propertyName, IReadOnlyCollection<string?>? values) =>
        SetPropertyArray(receiver, propertyName, values, (entry, value) => entry.SetString(value ?? ""));

    /// <summary>Writes an array of forms into an Auto array property of this script on a form. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="SetProperty(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="SetProperty(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void SetProperty<TForm>(IForm receiver, string propertyName, IReadOnlyCollection<TForm>? values) where TForm : IForm
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        PapyrusArguments.ThrowIfArrayTooLong(values);
        PapyrusPropertyAccessor.SetFormArray(receiver, ScriptName, propertyName, values);
    }

    internal void SetProperty(IForm receiver, string propertyName, Action<Variable> setPropertyValue)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        PapyrusPropertyAccessor.Set(receiver, ScriptName, propertyName, setPropertyValue);
    }

    private void SetPropertyArray<T>(IForm receiver, string propertyName, IReadOnlyCollection<T>? values, Action<Variable, T> setEntry)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        PapyrusArguments.ThrowIfArrayTooLong(values);
        PapyrusPropertyAccessor.SetArray(receiver, ScriptName, propertyName, values, setEntry);
    }

    /// <summary>Runs the getter of a full property, which is a Papyrus function and not a stored variable.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The getter could not be called. The exception is stored in the returned task.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty.</exception>
    public Task<TValue?> CallGetterAsync<TValue>(IForm receiver, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        return Dispatcher.DispatchPropertyGetAsync<TValue>(receiver, ScriptName, propertyName);
    }

    /// <summary>Runs a full property's setter with an int.</summary>
    /// <remarks>Returns once the write is accepted. The setter runs a frame or two later.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The setter could not be called.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty.</exception>
    public void CallSetter(IForm receiver, string propertyName, int value) => CallSetter(receiver, propertyName, variable => variable.SetSInt(value));

    /// <summary>Runs a full property's setter with a float.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/exception"/>
    public void CallSetter(IForm receiver, string propertyName, float value) => CallSetter(receiver, propertyName, variable => variable.SetFloat(value));

    /// <summary>Runs a full property's setter with a bool.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/exception"/>
    public void CallSetter(IForm receiver, string propertyName, bool value) => CallSetter(receiver, propertyName, variable => variable.SetBool(value));

    /// <summary>Runs a full property's setter with a string. Null is written as the empty string.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/remarks"/>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/exception"/>
    public void CallSetter(IForm receiver, string propertyName, string? value) => CallSetter(receiver, propertyName, variable => variable.SetString(value ?? ""));

    /// <summary>Runs a full form property's setter with a form. Null writes None.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/remarks"/>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The setter could not be called, or the property is not declared as a form.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty.</exception>
    public void CallSetter(IForm receiver, string propertyName, IForm? value)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        FormType formClass = PapyrusPropertyAccessor.GetDeclaredFormClass(ScriptName, propertyName, "write");
        nint formAddress = value?.Address ?? 0;
        Dispatcher.SetProperty(receiver, ScriptName, propertyName, variable => NativePapyrus.SF_Papyrus_SetVariableForm(variable.Address, formAddress, formClass));
    }

    /// <summary>Runs a full array property's setter with an array of ints. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, int)" path="/remarks"/>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> or <paramref name="propertyName"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="values"/> has more than 128 entries.</exception>
    /// <exception cref="InvalidOperationException">The setter could not be called, or the property is not declared as an array.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is empty.</exception>
    public void CallSetter(IForm receiver, string propertyName, IReadOnlyCollection<int>? values) =>
        CallSetterArray(receiver, propertyName, values, (entry, value) => entry.SetSInt(value));

    /// <summary>Runs a full array property's setter with an array of floats. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/remarks"/>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void CallSetter(IForm receiver, string propertyName, IReadOnlyCollection<float>? values) =>
        CallSetterArray(receiver, propertyName, values, (entry, value) => entry.SetFloat(value));

    /// <summary>Runs a full array property's setter with an array of bools. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/remarks"/>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void CallSetter(IForm receiver, string propertyName, IReadOnlyCollection<bool>? values) =>
        CallSetterArray(receiver, propertyName, values, (entry, value) => entry.SetBool(value));

    /// <summary>
    /// Runs a full array property's setter with an array of strings.
    /// Null writes None, not an empty array, and a <c>null</c> entry is written as the empty string.
    /// </summary>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/remarks"/>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void CallSetter(IForm receiver, string propertyName, IReadOnlyCollection<string?>? values) =>
        CallSetterArray(receiver, propertyName, values, (entry, value) => entry.SetString(value ?? ""));

    /// <summary>Runs a full array property's setter with an array of forms. Null writes None, not an empty array.</summary>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/remarks"/>
    /// <inheritdoc cref="CallSetter(IForm, string, IReadOnlyCollection{int})" path="/exception"/>
    public void CallSetter<TForm>(IForm receiver, string propertyName, IReadOnlyCollection<TForm>? values) where TForm : IForm
    {
        PapyrusArguments.ThrowIfArrayTooLong(values);
        CallSetter(receiver, propertyName, argument =>
        {
            if (values != null)
            {
                PapyrusPropertyAccessor.WriteFormArray(ScriptName, propertyName, argument, values);
            }
        });
    }

    private void CallSetter(IForm receiver, string propertyName, Action<Variable> setPropertyValue)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);
        Dispatcher.SetProperty(receiver, ScriptName, propertyName, setPropertyValue);
    }

    private void CallSetterArray<T>(IForm receiver, string propertyName, IReadOnlyCollection<T>? values, Action<Variable, T> setEntry)
    {
        PapyrusArguments.ThrowIfArrayTooLong(values);
        CallSetter(receiver, propertyName, argument =>
        {
            // Left unset for None. An empty array is a different value and is allocated.
            if (values != null)
            {
                PapyrusPropertyAccessor.WriteArray(ScriptName, propertyName, argument, values, setEntry);
            }
        });
    }

    /// <summary>Binds an instance of this script to a form. Does nothing if one is bound already.</summary>
    /// <remarks>
    /// Call on the game's thread. An instance bound here starts at the defaults its script declares, because no Creation Kit record supplies its property values.
    /// A save records the binding and the property values, so the script must stay installed while those saves are played.
    /// The Papyrus VM raises OnInit on the new instance within a frame of the bind.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="receiver"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Nothing was bound.</exception>
    public void Bind(IForm receiver)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        int status = NativePapyrus.SF_Papyrus_BindScript(receiver.Address, ScriptName);
        if (status != 0)
        {
            throw new InvalidOperationException(status switch
            {
                1 => $"There is no Papyrus virtual machine, so {ScriptName} was not bound.",
                2 => $"The Papyrus virtual machine has no handle for that form, so {ScriptName} cannot be bound to it.",
                3 => $"The Papyrus virtual machine has no script called {ScriptName}. Install the compiled {ScriptName}.pex in the game's Data\\Scripts folder.",
                4 => $"The Papyrus virtual machine did not create an instance of {ScriptName}.",
                5 => $"The bind of {ScriptName} to that form ran, but reading it back found nothing bound.",
                6 => $"The Papyrus virtual machine did not bind {ScriptName} to that form. A script binds only to a form of the class its Scriptname extends.",
                _ => $"Binding {ScriptName} returned the unrecognized status {status}."
            });
        }
    }
}

internal static partial class NativePapyrus
{
    [LibraryImport(library, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SF_Papyrus_BindScript(nint receiver, string className);
}
