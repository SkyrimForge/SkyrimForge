using SkyrimForge.Game;
using System;

namespace SkyrimForge.Papyrus;

/// <summary>Marks a class whose public methods implement the native functions of the Papyrus script of the same name.</summary>
/// <remarks>
/// <para>
/// The framework constructs the class as it constructs an <see cref="IPlugin"/> class, and registers each public method once the game's data has loaded.
/// A parameter or return value is int, float, bool, string, a form view for which Papyrus has a script type, such as Actor, or an array of one of those.
/// A method is a <c>global native</c> function unless its first parameter is marked <see cref="PapyrusSelfAttribute"/>.
/// </para>
/// <para>
/// Like an <see cref="IPlugin"/> class, the class has a log and saved state of its own. An exception a method throws is written to that log, and the script receives None.
/// A failed registration, such as for a script whose compiled .pex is not installed, is written to that log too.
/// </para>
/// <para>
/// Papyrus script names share one namespace across every mod, so include the mod's name in the class name.
/// Use <see cref="IPapyrusFunctionRegistrar"/> for a function that makes the calling script wait.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [PapyrusScript]
/// public sealed class MyModNatives(GameTarget gameTarget)
/// {
///     public int GetPlayerGold() => gameTarget.Player.GetGoldAmount();
/// }
/// </code>
/// The build writes Scripts\Source\MyModNatives.psc:
/// <code>
/// Scriptname MyModNatives Hidden
///
/// int Function GetPlayerGold() global native
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PapyrusScriptAttribute : Attribute
{
    /// <summary>
    /// The form view whose Papyrus script type the script extends, such as <see cref="ObjectReference"/>. Null for a script that extends none.
    /// </summary>
    /// <remarks>A script that extends none is declared <c>Hidden</c> and cannot be attached to a form.</remarks>
    public Type? Extends { get; init; }
}
