<#
.SYNOPSIS
    Builds SkyrimForge, writes the player archive and the NuGet packages to artifacts, and builds a plugin from the template package.

.EXAMPLE
    .\Package.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'Build.ps1')

$propertiesPath = Join-Path $PSScriptRoot 'Directory.Build.props'
$versionNode = ([xml](Get-Content $propertiesPath -Raw)).SelectSingleNode('/Project/PropertyGroup/Version')
if ($null -eq $versionNode) {
    throw "$propertiesPath declares no Version."
}
$version = $versionNode.InnerText
$staging = Join-Path $PSScriptRoot "artifacts\SkyrimForge-$version"
$archivePath = "$staging.zip"

if (Test-Path -LiteralPath $staging) {
    Remove-Item -LiteralPath $staging -Recurse -Force
}
$pluginsRoot = Join-Path $staging 'SKSE\Plugins'
& (Join-Path $PSScriptRoot 'Deploy.ps1') -Root $pluginsRoot -NoBuild

$framework = Join-Path $pluginsRoot 'SkyrimForge'
# At the Data root these files would conflict with other mods' files of the same name.
foreach ($name in 'LICENSE', 'EXCEPTIONS.md', 'NOTICE', 'THIRD-PARTY-NOTICES') {
    Copy-Item (Join-Path $PSScriptRoot $name) -Destination $framework
}

# Compress-Archive in Windows PowerShell 5.1 separates entry folders with a backslash, and the zip format requires a forward slash.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
$archive = [System.IO.Compression.ZipFile]::Open($archivePath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem $staging -File -Recurse) {
        $entryName = $file.FullName.Substring($staging.Length + 1).Replace('\', '/')
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName,
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    $archive.Dispose()
}

Write-Host ''
Write-Host "Packaged $archivePath" -ForegroundColor Green
$archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    foreach ($entry in $archive.Entries) {
        Write-Host "  $($entry.FullName)"
    }
}
finally {
    $archive.Dispose()
}

$packages = Join-Path $PSScriptRoot 'artifacts\packages'
if (Test-Path -LiteralPath $packages) {
    Remove-Item -LiteralPath $packages -Recurse -Force
}
foreach ($project in 'SkyrimForge', 'SkyrimForge.PluginTemplate') {
    & dotnet pack (Join-Path $PSScriptRoot "src\$project\$project.csproj") -c Release --no-build -o $packages --nologo
    if ($LASTEXITCODE -ne 0) { throw "Packing $project failed." }
}

Write-Host ''
Write-Host "Packed into $packages" -ForegroundColor Green

# --packages stops restore from reusing a SkyrimForge of the same version from the NuGet cache.
# ImportDirectoryBuildProps=false excludes this repository's Directory.Build.props from the plugin's build.
$templateTest = Join-Path $PSScriptRoot 'artifacts\template-test'
if (Test-Path -LiteralPath $templateTest) {
    Remove-Item -LiteralPath $templateTest -Recurse -Force
}
$hive = Join-Path $templateTest 'hive'
$plugin = Join-Path $templateTest 'TemplateTest'
# dotnet new writes an empty line to standard error, which Windows PowerShell 5.1 turns into a terminating error under Stop when the output is redirected.
& {
    $ErrorActionPreference = 'Continue'
    & dotnet new install (Join-Path $packages "SkyrimForge.PluginTemplate.$version.nupkg") --debug:custom-hive $hive
    if ($LASTEXITCODE -ne 0) { throw 'Installing the template failed.' }
    & dotnet new skyrimforge-plugin --name TemplateTest --output $plugin --debug:custom-hive $hive
    if ($LASTEXITCODE -ne 0) { throw 'Creating a project from the template failed.' }
    & dotnet restore $plugin --source $packages --packages (Join-Path $templateTest 'packages') -p:ImportDirectoryBuildProps=false
    if ($LASTEXITCODE -ne 0) { throw 'Restoring the project the template created failed.' }
    & dotnet build $plugin -c Release --no-restore -warnaserror -p:ImportDirectoryBuildProps=false --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Building the project the template created failed.' }
}

Write-Host ''
Write-Host "Built a plugin from the template in $plugin" -ForegroundColor Green
