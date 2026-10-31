<#
.SYNOPSIS
    Builds SkyrimForge, including the native runtime shim that dotnet cannot build.

.DESCRIPTION
    The native project has no Debug configuration because the debug CRT is not redistributable.

.EXAMPLE
    .\Build.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Find-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        throw "vswhere.exe not found at $vswhere. Install the ""Desktop development with C++"" workload in Visual Studio."
    }

    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    if (-not $msbuild) {
        throw "MSBuild not found. Install the ""Desktop development with C++"" workload in Visual Studio."
    }

    return $msbuild
}

$buildStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$stepStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

function Write-Elapsed([string] $name) {
    Write-Host ('{0} took {1:F1} s.' -f $name, $stepStopwatch.Elapsed.TotalSeconds) -ForegroundColor DarkGray
    $stepStopwatch.Restart()
}

$solution = Join-Path $PSScriptRoot 'SkyrimForge.slnx'
$runtimeProject = Join-Path $PSScriptRoot 'src\SkyrimForge.Runtime\SkyrimForge.Runtime.vcxproj'

Write-Host 'Building managed projects...' -ForegroundColor Cyan
& dotnet build $solution -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Managed build failed.' }
Write-Elapsed 'The managed build'

$msbuild = Find-MSBuild
Write-Host "Building native runtime shim using $msbuild" -ForegroundColor Cyan
& $msbuild $runtimeProject -p:Configuration=Release -p:Platform=x64 -nologo -v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }
Write-Elapsed 'The native build'

Write-Host ''
Write-Host 'Native runtime built:' -ForegroundColor Green
Write-Host ('  ' + (Join-Path $PSScriptRoot 'src\SkyrimForge.Runtime\x64\Release'))
Write-Host ('The build took {0:F1} s.' -f $buildStopwatch.Elapsed.TotalSeconds) -ForegroundColor DarkGray
