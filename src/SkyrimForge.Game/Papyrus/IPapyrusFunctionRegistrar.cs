using SkyrimForge.Game;
using System;
using System.Collections.Generic;

namespace SkyrimForge.Papyrus;

/// <summary>Provides a C# implementation for a native Papyrus function, so scripts can call into a plugin.</summary>
/// <remarks>
/// The plugin ships a compiled script that declares the function as native. Register from <see cref="IGameEvents.OnDataLoaded"/>.
/// Registering during <see cref="IPlugin.Start"/> throws, because the virtual machine does not exist.
/// Registering also throws if the script does not exist or does not declare the function with these types, or if the function is already registered.
/// Each call a script makes to a native registered here takes one frame.
/// </remarks>
public interface IPapyrusFunctionRegistrar
{
    /// <summary>Registers an implementation for a native function the script declares <c>global</c>.</summary>
    /// <remarks>
    /// <paramref name="returnType"/> is <see cref="PapyrusType.None"/> for a function with no return value.
    /// <paramref name="parameterTypes"/> has at most eleven entries, the game's limit.
    /// An exception <paramref name="body"/> throws is written to the log of the plugin that registered it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="className"/>, <paramref name="functionName"/>, <paramref name="parameterTypes"/>, or <paramref name="body"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="className"/> or <paramref name="functionName"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">The function was not registered, or no script call would reach it. The message states the reason.</exception>
    void RegisterStatic(string className, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Action<PapyrusCall> body);

    /// <summary>Registers a <c>global</c> native that stops the calling script until the plugin returns a value.</summary>
    /// <remarks>
    /// <paramref name="returnType"/> is the type the script receives once the value arrives, or <see cref="PapyrusType.None"/> for a function with no return value.
    /// <paramref name="parameterTypes"/> has at most eleven entries, the game's limit.
    /// <paramref name="body"/> returns true if the work started and the script waits. False, or an exception, returns None to the script and resumes it.
    /// The body must return quickly, since the game is stopped while it runs.
    /// The function's return value goes through <see cref="PapyrusLatentCall"/>, and each <c>Return</c> of <see cref="PapyrusCall"/> throws.
    /// A save taken while a script waits loads with that script still stopped, because the waiting stack is not written into the co-save.
    /// A <see cref="PapyrusLatentCall"/> the plugin retains across that load resumes it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="className"/>, <paramref name="functionName"/>, <paramref name="parameterTypes"/>, or <paramref name="body"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="className"/> or <paramref name="functionName"/> is empty, or <paramref name="returnType"/> is an array.
    /// </exception>
    /// <exception cref="InvalidOperationException">The function was not registered, or no script call would reach it. The message states the reason.</exception>
    void RegisterLatentStatic(string className, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Func<PapyrusCall, PapyrusLatentCall, bool> body);

    /// <summary>
    /// Registers an implementation for a native function a script declares without <c>global</c>.
    /// The receiver arrives as <see cref="PapyrusCall.Self"/>, and <see cref="PapyrusScriptDispatcher.Bind"/> puts an instance on a form.
    /// </summary>
    /// <remarks>
    /// <paramref name="selfType"/> is the type the script extends rather than the script itself. <see cref="PapyrusCall.Self"/> reads None if the receiver does not resolve as it.
    /// <paramref name="returnType"/> is <see cref="PapyrusType.None"/> for a function with no return value.
    /// <paramref name="parameterTypes"/> has at most eleven entries, the game's limit, and the receiver is not among them.
    /// An exception <paramref name="body"/> throws is written to the log of the plugin that registered it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selfClassName"/>, <paramref name="functionName"/>, <paramref name="parameterTypes"/>, or <paramref name="body"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="selfClassName"/> or <paramref name="functionName"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">The function was not registered, or no script call would reach it. The message states the reason.</exception>
    void RegisterInstance(string selfClassName, FormType selfType, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Action<PapyrusCall> body);

    /// <summary><see cref="RegisterLatentStatic"/> for a native declared without <c>global</c>.</summary>
    /// <remarks>
    /// <paramref name="selfType"/> is the type the script extends rather than the script itself. <see cref="PapyrusCall.Self"/> reads None if the receiver does not resolve as it.
    /// The receiver is not among <paramref name="parameterTypes"/>.
    /// <inheritdoc cref="RegisterLatentStatic" path="/remarks/node()"/>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selfClassName"/>, <paramref name="functionName"/>, <paramref name="parameterTypes"/>, or <paramref name="body"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="selfClassName"/> or <paramref name="functionName"/> is empty, or <paramref name="returnType"/> is an array.
    /// </exception>
    /// <exception cref="InvalidOperationException">The function was not registered, or no script call would reach it. The message states the reason.</exception>
    void RegisterLatentInstance(string selfClassName, FormType selfType, PapyrusType returnType, string functionName, IReadOnlyCollection<PapyrusType> parameterTypes, Func<PapyrusCall, PapyrusLatentCall, bool> body);
}
