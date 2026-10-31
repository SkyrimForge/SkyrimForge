# SkyrimForge.PluginTemplate

The `dotnet new` template for a [SkyrimForge](https://www.nuget.org/packages/SkyrimForge) plugin: a .NET 10 class library that references
the SkyrimForge package, with a plugin class that shows a notification with the player's name each time a new game starts or a save finishes loading.

```powershell
dotnet new install SkyrimForge.PluginTemplate
dotnet new skyrimforge-plugin --name <YourPluginName>
```

The project, its plugin class, and the plugin's messages take the name `--name` gives. Choose a name unique to your mod:
by default, the plugin class's full name is its identity, from which its log file and its saved state take their names.

## Documentation

[docs.skyrimforge.net](https://docs.skyrimforge.net/) covers writing, installing, and debugging a plugin, the services a plugin can use,
and the API reference.

## License

SkyrimForge is licensed under the GNU General Public License, version 3 or later, with the additional permissions in its
[EXCEPTIONS.md](https://github.com/SkyrimForge/SkyrimForge/blob/master/EXCEPTIONS.md), which permit linking it with Skyrim and with SKSE.
A plugin built on SkyrimForge is a combined work with it, so its license must be compatible with the GPL.
