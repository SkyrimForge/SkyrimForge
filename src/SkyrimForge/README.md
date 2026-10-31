# SkyrimForge

SkyrimForge is a framework for writing Skyrim plugins in C#. A plugin references this package, which contains the framework's API
and the bindings generated from CommonLibSSE-NG's headers.

One build of a plugin runs on Skyrim Special Edition, Anniversary Edition, and Skyrim VR.

## Creating a Plugin

A plugin is a .NET 10 class library that references this package. The
[SkyrimForge.PluginTemplate](https://www.nuget.org/packages/SkyrimForge.PluginTemplate) package creates one:

```powershell
dotnet new install SkyrimForge.PluginTemplate
dotnet new skyrimforge-plugin --name <YourPluginName>
```

Choose a name unique to your mod: by default, the plugin class's full name is its identity, from which its log file and its saved state take their names.

A plugin class implements `IPlugin`, and its one public constructor declares the services it uses:

```csharp
using SkyrimForge;

namespace GreetingPlugin;

public sealed class GreetingPlugin(IGameEvents events, IDebug debug) : IPlugin
{
    public void Start()
    {
        events.OnNewGameOrSaveLoaded.Subscribe(_ => debug.Notification("Hello from GreetingPlugin."));
    }
}
```

The plugin runs in a game with SKSE, the Address Library for the game's edition, the .NET 10 Runtime (x64), and
[SkyrimForge](https://www.nexusmods.com/skyrimspecialedition/mods/196701) installed.

## Building

The package contains the build steps for Papyrus: they write the script source (.psc) for native functions implemented in C#,
generate C# from the plugin's own .psc files, and compile the scripts. The build output has the folder structure of the game's Data folder.
Setting the property `SkyrimForgeDeployDirectory` in `<YourPluginName>.csproj.user` makes each build copy its output into a mod folder.
The `.user` file keeps the machine's path out of the project file.

## Documentation

[docs.skyrimforge.net](https://docs.skyrimforge.net/) covers writing, installing, and debugging a plugin, the services a plugin can use,
and the API reference.

## License

SkyrimForge is licensed under the GNU General Public License, version 3 or later, with the additional permissions in its
[EXCEPTIONS.md](https://github.com/SkyrimForge/SkyrimForge/blob/master/EXCEPTIONS.md), which permit linking it with Skyrim and with SKSE.
A plugin built on SkyrimForge is a combined work with it, so its license must be compatible with the GPL.
