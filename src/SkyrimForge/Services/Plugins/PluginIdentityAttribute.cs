using System;

namespace SkyrimForge;

/// <summary>
/// States the name under which a plugin's saved state and log file are stored. The plugin is an <see cref="IPlugin"/> class or a class marked <c>[PapyrusScript]</c>.
/// </summary>
/// <remarks>
/// A plugin that states no identity is stored under the full name of its type, so renaming the class or its namespace leaves the state in every existing save unread.
/// The identity is unique among the plugins loaded and contains no character a file name excludes.
/// </remarks>
/// <param name="identity">The name, which stays the same in every version of the plugin.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginIdentityAttribute(string identity) : Attribute
{
    /// <summary>The name, which stays the same in every version of the plugin.</summary>
    public string Identity { get; } = identity;
}
