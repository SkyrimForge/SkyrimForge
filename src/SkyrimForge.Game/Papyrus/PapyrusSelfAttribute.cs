using System;

namespace SkyrimForge.Papyrus;

/// <summary>
/// Marks the first parameter of a method in a <see cref="PapyrusScriptAttribute"/> class as the form the script is attached to, making the method a non-global native function.
/// </summary>
/// <remarks>
/// The parameter's type is the class's <see cref="PapyrusScriptAttribute.Extends"/> type. The .psc declares the function without this parameter,
/// and a script calls it on a form, as in <c>(target as MyModNatives).Describe()</c>.
/// </remarks>
/// <example>
/// <code>
/// [PapyrusScript(Extends = typeof(ObjectReference))]
/// public sealed class MyModNatives
/// {
///     public string Describe([PapyrusSelf] ObjectReference self) => self.GetDisplayFullName();
/// }
/// </code>
/// The build writes Scripts\Source\MyModNatives.psc:
/// <code>
/// Scriptname MyModNatives extends ObjectReference
///
/// string Function Describe() native
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class PapyrusSelfAttribute : Attribute;
