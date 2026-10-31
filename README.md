# SkyrimForge

SkyrimForge is a framework for writing Skyrim plugins in C#. A native SKSE plugin hosts .NET inside the game, and managed plugins call the game
through bindings generated from CommonLibSSE-NG's headers, alongside the Papyrus script functions and the game's events.

## Editions

One build runs on Skyrim Special Edition (1.5.97), Anniversary Edition (1.6 and later), and Skyrim VR (1.4.15).

## Download

Players install SkyrimForge from [Nexus Mods](https://www.nexusmods.com/skyrimspecialedition/mods/196701).

## Requirements to Run

- [SKSE](https://skse.silverlock.org/)
- [Address Library for SKSE Plugins](https://www.nexusmods.com/skyrimspecialedition/mods/32444)
(or [VR Address Library for SKSEVR](https://www.nexusmods.com/skyrimspecialedition/mods/58101))
- .NET 10 Runtime (x64)

## Writing a Plugin

A plugin is a .NET 10 class library that references the SkyrimForge NuGet package. The plugin template creates one:

```powershell
dotnet new install SkyrimForge.PluginTemplate
dotnet new skyrimforge-plugin --name <YourPluginName>
```

[SkyrimForge.PrismaUi](https://github.com/SkyrimForge/SkyrimForge.PrismaUi) lets a plugin create and script [PrismaUI](https://github.com/PrismaUI-SKSE/framework) views in C#.

[docs.skyrimforge.net](https://docs.skyrimforge.net/) covers writing a plugin and the services it can use. A plugin needs none of the steps below,
which build SkyrimForge itself.

## Building

Requirements:

- .NET 10 SDK
- Visual Studio with the "Desktop development with C++" workload
- [vcpkg](https://github.com/microsoft/vcpkg), with `VCPKG_ROOT` set to its folder

Then run:

```powershell
.\Build.ps1
```

`Deploy.ps1` builds SkyrimForge and copies it into a mod folder. Add `-NoBuild` to copy the last build without building again.

```powershell
.\Deploy.ps1 -Root "<mod folder>\SKSE\Plugins"
```

To also deploy the example plugins, add `-Examples`. The build compiles their Papyrus scripts with the Creation Kit's compiler,
so the Creation Kit must be installed.

```powershell
.\Deploy.ps1 -Root "<mod folder>\SKSE\Plugins" -Examples
```

`Package.ps1` builds and writes the archive players install (`artifacts\SkyrimForge-<version>.zip`),
the NuGet package that plugins reference (`artifacts\packages\SkyrimForge.<version>.nupkg`),
and the package of the `dotnet new` plugin template (`artifacts\packages\SkyrimForge.PluginTemplate.<version>.nupkg`).

## Documentation

[docs.skyrimforge.net](https://docs.skyrimforge.net/) covers writing a plugin, the services a plugin can use, and the API reference.
`src/SkyrimForge.ExamplePlugins` contains small plugins, each showing one part of the framework.

## License

SkyrimForge is licensed under the [GNU General Public License, version 3 or later](LICENSE), with the additional permissions in
[EXCEPTIONS.md](EXCEPTIONS.md), which permit linking it with Skyrim and with SKSE. A plugin built on SkyrimForge is a combined work with it,
so its license must be compatible with the GPL. See [NOTICE](NOTICE) and
[THIRD-PARTY-NOTICES](THIRD-PARTY-NOTICES) for the notices that copies must retain.
