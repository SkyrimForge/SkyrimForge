namespace SkyrimForge;

/// <summary>A destination for log lines from the framework or a plugin.</summary>
/// <remarks>
/// A plugin's log is the file <c>SKSE\Plugins\SkyrimForge\Plugins\&lt;identity&gt;.log</c> under the game's Data folder,
/// named for the identity <see cref="PluginIdentityAttribute"/> states or else for the plugin type's full name.
/// Mod Organizer 2 writes the file to its overwrite folder instead.
/// Members can be called on any thread.
/// </remarks>
public interface ILog
{
    /// <summary>Writes one line, stamped with the time it is written. An implementation records its own failures and does not throw.</summary>
    void Write(string message);
}
