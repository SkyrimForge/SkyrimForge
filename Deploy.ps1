<#
.SYNOPSIS
    Builds SkyrimForge and copies it into a mod folder.

.PARAMETER Root
    The SKSE\Plugins folder to deploy to, in a mod manager's mod folder or the game's Data folder.

.PARAMETER Examples
    Also deploys the example plugins and their Papyrus scripts, which require the Creation Kit's Papyrus compiler.
    Without -Examples, any deployed copy is deleted.

.PARAMETER NoBuild
    Copies the last build instead of running Build.ps1.

.EXAMPLE
    .\Deploy.ps1 -Root "$env:LOCALAPPDATA\ModOrganizer\Skyrim Special Edition\mods\SkyrimForge\SKSE\Plugins"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Root,
    [switch] $Examples,
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'

$gameProcesses = Get-Process SkyrimSE, SkyrimVR -ErrorAction SilentlyContinue
if ($gameProcesses) {
    throw 'Skyrim is running. Close it before deploying, or the loaded DLLs cannot be replaced.'
}

if (-not $NoBuild) {
    & (Join-Path $PSScriptRoot 'Build.ps1')
}

$framework = Join-Path $Root 'SkyrimForge'
$plugins = Join-Path $framework 'Plugins'
$managedOutput = 'bin\Release\net10.0'

# Data\Scripts is two levels above Data\SKSE\Plugins.
$scripts = Join-Path (Split-Path (Split-Path $Root -Parent) -Parent) 'Scripts'

# Copy-Item to a missing folder writes a file under that name instead of failing.
foreach ($folder in $Root, $framework, $plugins) {
    if (-not (Test-Path $folder)) {
        New-Item -ItemType Directory -Force -Path $folder | Out-Null
    }
}

$frameworkFiles = @(
    @{ From = "src\SkyrimForge.Runtime\x64\Release\SkyrimForge.Runtime.dll"; To = $Root }
    @{ From = "src\SkyrimForge\$managedOutput\SkyrimForge.dll"; To = $framework }
    @{ From = "src\SkyrimForge.PluginHost\$managedOutput\SkyrimForge.PluginHost.dll"; To = $framework }
    @{ From = "src\SkyrimForge.PluginHost\$managedOutput\SkyrimForge.PluginHost.deps.json"; To = $framework }
    @{ From = "src\SkyrimForge.PluginHost\$managedOutput\SkyrimForge.PluginHost.runtimeconfig.json"; To = $framework }
    @{ From = "src\SkyrimForge.Infrastructure\$managedOutput\SkyrimForge.Infrastructure.dll"; To = $framework }
    # Beside the host so they load once, into its context. A copy in the plugins folder loads once per plugin, which duplicates process-wide state.
    @{ From = "src\SkyrimForge.Game\$managedOutput\SkyrimForge.Game.dll"; To = $framework }
    @{ From = "src\SkyrimForge.CommonLib\$managedOutput\SkyrimForge.CommonLib.dll"; To = $framework }
)

$exampleOutput = "src\SkyrimForge.ExamplePlugins\$managedOutput"
$exampleFolder = Join-Path $plugins 'SkyrimForge.ExamplePlugins'
$examplePlugins = @(
    @{ From = "$exampleOutput\SKSE\Plugins\SkyrimForge\Plugins\SkyrimForge.ExamplePlugins\SkyrimForge.ExamplePlugins.dll"; To = $exampleFolder }
    @{ From = "$exampleOutput\SKSE\Plugins\SkyrimForge\Plugins\SkyrimForge.ExamplePlugins\SkyrimForge.ExamplePlugins.deps.json"; To = $exampleFolder }
)

$exampleScripts = @(Get-ChildItem -Path (Join-Path $PSScriptRoot 'src\SkyrimForge.ExamplePlugins\Scripts\Source') -Filter *.psc |
    ForEach-Object { @{ From = "$exampleOutput\Scripts\$($_.BaseName).pex"; To = $scripts } })

$files = $frameworkFiles
if ($Examples) {
    foreach ($folder in $exampleFolder, $scripts) {
        if (-not (Test-Path $folder)) {
            New-Item -ItemType Directory -Force -Path $folder | Out-Null
        }
    }
    $files = $files + $examplePlugins + $exampleScripts
}

Write-Host "Deploying to $Root" -ForegroundColor Cyan
$failureCount = 0
foreach ($file in $files) {
    $source = Join-Path $PSScriptRoot $file.From
    if (-not (Test-Path $source)) {
        throw "$source is missing. Run .\Build.ps1 first. The build compiles a .pex only where the Creation Kit's Papyrus compiler is installed."
    }

    $name = Split-Path $source -Leaf
    Copy-Item $source -Destination $file.To -Force

    $sourceHash = Get-FileHash $source
    $deployedHash = Get-FileHash (Join-Path $file.To $name)
    if ($sourceHash.Hash -eq $deployedHash.Hash) {
        Write-Host "  ok   $name"
    }
    else {
        Write-Host "  FAIL $name" -ForegroundColor Red
        $failureCount++
    }
}

if ($failureCount -gt 0) {
    throw "$failureCount deployed file(s) differ from the files they were copied from."
}

if (-not $Examples) {
    if (Test-Path $exampleFolder) {
        Remove-Item $exampleFolder -Recurse -Force
        Write-Host "  gone $(Split-Path $exampleFolder -Leaf)" -ForegroundColor DarkGray
    }
    foreach ($file in $exampleScripts) {
        $name = Split-Path $file.From -Leaf
        $deployedPath = Join-Path $file.To $name
        if (Test-Path $deployedPath) {
            Remove-Item $deployedPath -Force
            Write-Host "  gone $name" -ForegroundColor DarkGray
        }
    }
}

Write-Host ''
Write-Host 'Deployed.' -ForegroundColor Green
