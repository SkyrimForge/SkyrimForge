<#
.SYNOPSIS
    Builds SkyrimForge and copies it into a mod folder.

.PARAMETER Root
    The SKSE\Plugins folder to which to deploy in a mod manager's mod folder or the game's Data folder.
    Defaults to the SkyrimForge mod of Mod Organizer 2's global Skyrim Special Edition instance.

.PARAMETER Examples
    Also deploys the example plugins and their Papyrus scripts, which require the Creation Kit's Papyrus compiler.
    Without -Examples, any deployed copy is deleted.

.PARAMETER NoBuild
    Copies the last build instead of running Build.ps1.

.EXAMPLE
    .\Deploy.ps1

.EXAMPLE
    .\Deploy.ps1 -Root "C:\Modding\MO2\mods\SkyrimForge\SKSE\Plugins"
#>
[CmdletBinding()]
param(
    [string] $Root = "$env:LOCALAPPDATA\ModOrganizer\Skyrim Special Edition\mods\SkyrimForge\SKSE\Plugins",
    [switch] $Examples,
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'

$running = Get-Process SkyrimSE, SkyrimVR -ErrorAction SilentlyContinue
if ($running) {
    throw 'Skyrim is running. Close it before deploying, or the loaded DLLs cannot be replaced.'
}

$modRoot = Split-Path (Split-Path $Root -Parent) -Parent
$mods = Split-Path $modRoot -Parent

# The default assumes Mod Organizer 2's global instance. An explicit -Root can name a folder that does not exist yet.
if (-not $PSBoundParameters.ContainsKey('Root') -and -not (Test-Path $mods)) {
    throw "$mods does not exist. Pass the SKSE\Plugins folder to deploy to as -Root."
}

if (-not $NoBuild) {
    & (Join-Path $PSScriptRoot 'Build.ps1')
}

$framework = Join-Path $Root 'SkyrimForge'
$examplePlugin = Join-Path $framework 'Plugins\SkyrimForge.ExamplePlugins'
$scripts = Join-Path $modRoot 'Scripts'
$src = Join-Path $PSScriptRoot 'src'
$output = 'bin\Release\net10.0'
$exampleOutput = "$src\SkyrimForge.ExamplePlugins\$output"

$files = @(
    @{ From = "$src\SkyrimForge.Runtime\x64\Release\SkyrimForge.Runtime.dll"; To = $Root }
    @{ From = "$src\SkyrimForge\$output\SkyrimForge.dll"; To = $framework }
    @{ From = "$src\SkyrimForge.PluginHost\$output\SkyrimForge.PluginHost.dll"; To = $framework }
    @{ From = "$src\SkyrimForge.PluginHost\$output\SkyrimForge.PluginHost.deps.json"; To = $framework }
    @{ From = "$src\SkyrimForge.PluginHost\$output\SkyrimForge.PluginHost.runtimeconfig.json"; To = $framework }
    @{ From = "$src\SkyrimForge.Infrastructure\$output\SkyrimForge.Infrastructure.dll"; To = $framework }
    # Beside the host so they load once, into its context. A copy in the plugins folder loads once per plugin, which duplicates process-wide state.
    @{ From = "$src\SkyrimForge.Game\$output\SkyrimForge.Game.dll"; To = $framework }
    @{ From = "$src\SkyrimForge.CommonLib\$output\SkyrimForge.CommonLib.dll"; To = $framework }
)
$exampleFiles = @(
    @{ From = "$exampleOutput\SKSE\Plugins\SkyrimForge\Plugins\SkyrimForge.ExamplePlugins\SkyrimForge.ExamplePlugins.dll"; To = $examplePlugin }
    @{ From = "$exampleOutput\SKSE\Plugins\SkyrimForge\Plugins\SkyrimForge.ExamplePlugins\SkyrimForge.ExamplePlugins.deps.json"; To = $examplePlugin }
)
foreach ($source in Get-ChildItem "$src\SkyrimForge.ExamplePlugins\Scripts\Source" -Filter *.psc) {
    $exampleFiles += @{ From = "$exampleOutput\Scripts\$($source.BaseName).pex"; To = $scripts }
}
if ($Examples) {
    $files += $exampleFiles
}

Write-Host "Deploying to $modRoot" -ForegroundColor Cyan
$failed = 0
foreach ($file in $files) {
    if (-not (Test-Path $file.From)) {
        throw "$($file.From) is missing. Run .\Build.ps1 first. The build compiles a .pex only where the Creation Kit's Papyrus compiler is installed."
    }

    $name = Split-Path $file.From -Leaf
    New-Item -ItemType Directory -Force -Path $file.To | Out-Null
    Copy-Item $file.From -Destination $file.To -Force

    if ((Get-FileHash $file.From).Hash -eq (Get-FileHash (Join-Path $file.To $name)).Hash) {
        Write-Host "  ok   $name"
    }
    else {
        Write-Host "  FAIL $name" -ForegroundColor Red
        $failed++
    }
}

if ($failed -gt 0) {
    throw "$failed file(s) differ from the build output."
}

if (-not $Examples) {
    if (Test-Path $examplePlugin) {
        Remove-Item $examplePlugin -Recurse -Force
        Write-Host "  gone $(Split-Path $examplePlugin -Leaf)" -ForegroundColor DarkGray
    }
    foreach ($file in $exampleFiles) {
        $name = Split-Path $file.From -Leaf
        $deployed = Join-Path $file.To $name
        if (Test-Path $deployed) {
            Remove-Item $deployed -Force
            Write-Host "  gone $name" -ForegroundColor DarkGray
        }
    }
}

Write-Host ''
Write-Host 'Deployed.' -ForegroundColor Green
