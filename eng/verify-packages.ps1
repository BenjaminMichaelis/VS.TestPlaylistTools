#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Statically verifies the packed NuGet packages before they are installed or published.

.DESCRIPTION
    Asserts that the feed contains exactly the expected packages and that:
      * every package (and every packaged assembly) carries the expected version;
      * library dependencies between our packages are pinned to the same version;
      * library packages contain the expected target frameworks;
      * the tool's top-level pointer package references exactly the RID-specific packages that the
        csproj declares (ToolPackageRuntimeIdentifiers) and that those packages exist in the feed;
      * native payloads have no managed CLI assembly; musl is self-contained CoreCLR and any is framework-dependent.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Feed,

    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Split-Path -Parent $PSScriptRoot
$toolProject = Join-Path $repoRoot 'trx-to-vsplaylist/trx-to-vsplaylist.csproj'
$toolId = 'trx-to-vsplaylist'
$toolCommand = 'trx-to-vsplaylist'
$libraryTfms = @('netstandard2.1', 'net8.0', 'net10.0')
$libraries = [ordered]@{
    'VSTestPlaylistTools'                        = @{ Assemblies = @('VSTestPlaylistTools.dll'); Dependencies = @() }
    'VSTestPlaylistTools.TrxToPlaylistConverter' = @{ Assemblies = @('VSTestPlaylistTools.TrxToPlaylistConverter.dll'); Dependencies = @('VSTestPlaylistTools') }
}
# Assemblies we build that must carry the package version wherever they are packaged.
$ownAssemblies = @("$toolId.dll", 'VSTestPlaylistTools.dll', 'VSTestPlaylistTools.TrxToPlaylistConverter.dll')

$failures = [System.Collections.Generic.List[string]]::new()
function Fail([string]$message) { $failures.Add($message); Write-Host "  FAIL: $message" -ForegroundColor Red }
function Pass([string]$message) { Write-Host "  ok: $message" }

$Feed = (Resolve-Path $Feed).Path
$extractRoot = Join-Path ([System.IO.Path]::GetTempPath()) "verify-packages-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $extractRoot | Out-Null

function Expand-Package([string]$id) {
    $nupkg = Join-Path $Feed "$id.$Version.nupkg"
    $destination = Join-Path $extractRoot $id
    [System.IO.Compression.ZipFile]::ExtractToDirectory($nupkg, $destination)
    return $destination
}

function Get-Nuspec([string]$packageDir) {
    $nuspecPath = Get-ChildItem -Path $packageDir -Filter '*.nuspec' | Select-Object -First 1
    [xml](Get-Content -Raw -Path $nuspecPath.FullName)
}

function Test-AssemblyVersions([string]$packageDir, [string]$packageId) {
    $found = Get-ChildItem -Path $packageDir -Recurse -File | Where-Object { $ownAssemblies -contains $_.Name }
    if (-not $found) { Fail "$packageId contains none of our assemblies"; return }
    foreach ($assembly in $found) {
        $productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($assembly.FullName).ProductVersion
        # The informational version may carry SourceLink commit metadata (e.g. 1.2.3+abcdef).
        if ($productVersion -eq $Version -or $productVersion -like "$Version+*") {
            Pass "$packageId/$($assembly.Directory.Name)/$($assembly.Name) informational version $productVersion"
        }
        else {
            Fail "$packageId/$($assembly.Directory.Name)/$($assembly.Name) informational version '$productVersion' does not match package version '$Version'"
        }
    }
}

try {
    Write-Host "Verifying packages in $Feed for version $Version"

    # --- Expected package set ------------------------------------------------------------------
    $ridProperty = (dotnet msbuild $toolProject -getProperty:ToolPackageRuntimeIdentifiers).Trim()
    $rids = @($ridProperty -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $nativeRids = @(((dotnet msbuild $toolProject -getProperty:NativeAotRuntimeIdentifiers) | Out-String).Trim() -split ';')
    if ($rids.Count -eq 0) { throw "Could not read ToolPackageRuntimeIdentifiers from $toolProject" }
    Write-Host "Tool RIDs from csproj: $($rids -join ', ')"

    $expectedIds = @($libraries.Keys) + @($toolId) + @($rids | ForEach-Object { "$toolId.$_" })
    $actualFiles = @(Get-ChildItem -Path $Feed -Filter '*.nupkg' | ForEach-Object Name)
    $expectedFiles = @($expectedIds | ForEach-Object { "$_.$Version.nupkg" })

    Write-Host 'Package set'
    foreach ($file in $expectedFiles) {
        if ($actualFiles -contains $file) { Pass "found $file" } else { Fail "missing $file" }
    }
    foreach ($file in $actualFiles | Where-Object { $expectedFiles -notcontains $_ }) {
        Fail "unexpected package $file"
    }
    if ($failures.Count -gt 0) { throw 'Package set is incorrect; skipping content checks.' }

    # --- Library packages ----------------------------------------------------------------------
    foreach ($libraryId in $libraries.Keys) {
        Write-Host "Library package $libraryId"
        $dir = Expand-Package $libraryId
        $nuspec = Get-Nuspec $dir
        $metadata = $nuspec.package.metadata

        if ($metadata.version -eq $Version) { Pass "nuspec version $($metadata.version)" }
        else { Fail "$libraryId nuspec version '$($metadata.version)' != '$Version'" }

        foreach ($tfm in $libraryTfms) {
            foreach ($assembly in $libraries[$libraryId].Assemblies) {
                if (Test-Path (Join-Path $dir "lib/$tfm/$assembly")) { Pass "lib/$tfm/$assembly" }
                else { Fail "$libraryId is missing lib/$tfm/$assembly" }
            }
        }

        foreach ($dependencyId in $libraries[$libraryId].Dependencies) {
            $groups = @($metadata.dependencies.group)
            if ($groups.Count -ne $libraryTfms.Count) {
                Fail "$libraryId has $($groups.Count) dependency groups; expected $($libraryTfms.Count)"
            }
            foreach ($group in $groups) {
                $dependency = @($group.dependency) | Where-Object { $_.id -eq $dependencyId }
                if (-not $dependency) { Fail "$libraryId ($($group.targetFramework)) does not depend on $dependencyId" }
                elseif ($dependency.version -ne $Version) {
                    Fail "$libraryId ($($group.targetFramework)) depends on $dependencyId $($dependency.version); expected $Version"
                }
                else { Pass "$($group.targetFramework) depends on $dependencyId $($dependency.version)" }
            }
        }

        Test-AssemblyVersions $dir $libraryId
    }

    # --- Tool pointer package -----------------------------------------------------------------
    Write-Host "Tool pointer package $toolId"
    $pointerDir = Expand-Package $toolId
    $pointerNuspec = Get-Nuspec $pointerDir
    if ($pointerNuspec.package.metadata.version -eq $Version) { Pass "nuspec version $Version" }
    else { Fail "$toolId nuspec version '$($pointerNuspec.package.metadata.version)' != '$Version'" }

    $pointerType = @($pointerNuspec.package.metadata.packageTypes.packageType)[0].name
    if ($pointerType -eq 'DotnetTool') { Pass 'package type DotnetTool' } else { Fail "$toolId package type '$pointerType' != 'DotnetTool'" }

    $pointerBinaries = @(Get-ChildItem -Path (Join-Path $pointerDir 'tools') -Recurse -File | Where-Object Extension -in '.dll', '.exe', '')
    if ($pointerBinaries.Count -eq 0) { Pass 'pointer package contains no binaries' }
    else { Fail "pointer package unexpectedly contains binaries: $($pointerBinaries.Name -join ', ')" }

    $pointerSettingsFile = @(Get-ChildItem -Path (Join-Path $pointerDir 'tools') -Recurse -Filter 'DotnetToolSettings.xml')
    if ($pointerSettingsFile.Count -ne 1) { throw "Expected exactly one DotnetToolSettings.xml in $toolId, found $($pointerSettingsFile.Count)" }
    $pointerSettings = [xml](Get-Content -Raw $pointerSettingsFile[0].FullName)
    $pointerCommand = @($pointerSettings.DotNetCliTool.Commands.Command)[0].Name
    if ($pointerCommand -eq $toolCommand) { Pass "command name $pointerCommand" } else { Fail "pointer command name '$pointerCommand' != '$toolCommand'" }

    $references = @($pointerSettings.DotNetCliTool.RuntimeIdentifierPackages.RuntimeIdentifierPackage)
    $referencedRids = @($references | ForEach-Object RuntimeIdentifier)
    foreach ($rid in $rids) {
        if ($referencedRids -notcontains $rid) { Fail "pointer does not reference RID '$rid'" }
    }
    foreach ($reference in $references) {
        $rid = $reference.RuntimeIdentifier
        $expectedId = "$toolId.$rid"
        # A missing Version attribute means "same version as the pointer package".
        $referencedVersion = if ($reference.HasAttribute('Version')) { $reference.Version } else { $Version }
        if ($rids -notcontains $rid) { Fail "pointer references unexpected RID '$rid'"; continue }
        if ($reference.Id -ne $expectedId) { Fail "pointer maps RID '$rid' to '$($reference.Id)'; expected '$expectedId'"; continue }
        if ($referencedVersion -ne $Version) { Fail "pointer references $expectedId $referencedVersion; expected $Version"; continue }
        if (-not (Test-Path (Join-Path $Feed "$expectedId.$referencedVersion.nupkg"))) { Fail "pointer references $expectedId $referencedVersion which is not in the feed"; continue }
        Pass "pointer: $rid -> $expectedId $referencedVersion"
    }

    # --- RID-specific tool packages -------------------------------------------------------------
    foreach ($rid in $rids) {
        $id = "$toolId.$rid"
        Write-Host "Tool RID package $id"
        $dir = Expand-Package $id
        $nuspec = Get-Nuspec $dir
        if ($nuspec.package.metadata.version -eq $Version) { Pass "nuspec version $Version" }
        else { Fail "$id nuspec version '$($nuspec.package.metadata.version)' != '$Version'" }

        $packageType = @($nuspec.package.metadata.packageTypes.packageType)[0].name
        if ($packageType -eq 'DotnetToolRidPackage') { Pass 'package type DotnetToolRidPackage' }
        else { Fail "$id package type '$packageType' != 'DotnetToolRidPackage'" }

        $settingsFile = @(Get-ChildItem -Path (Join-Path $dir 'tools') -Recurse -Filter 'DotnetToolSettings.xml')
        if ($settingsFile.Count -ne 1) { Fail "$id should contain exactly one DotnetToolSettings.xml; found $($settingsFile.Count)"; continue }
        $toolDir = $settingsFile[0].Directory.FullName
        $command = @(([xml](Get-Content -Raw $settingsFile[0].FullName)).DotNetCliTool.Commands.Command)[0]

        if ($command.Name -ne $toolCommand) { Fail "$id command name '$($command.Name)' != '$toolCommand'" }

        if ($nativeRids -contains $rid) {
            $entry = if ($rid -like 'win-*') { "$toolId.exe" } else { $toolId }
            if ($command.Runner -ne 'executable' -or $command.EntryPoint -ne $entry -or
                -not (Test-Path (Join-Path $toolDir $entry))) { Fail "$id is missing its native executable" }
            if (Test-Path (Join-Path $toolDir "$toolId.dll")) { Fail "$id unexpectedly contains a managed CLI assembly" }
            continue
        }

        $runtimeConfig = Get-Content -Raw (Join-Path $toolDir "$toolId.runtimeconfig.json") | ConvertFrom-Json
        $isSelfContained = $null -ne $runtimeConfig.runtimeOptions.PSObject.Properties['includedFrameworks']

        if ($rid -eq 'any') {
            if ($command.Runner -eq 'dotnet' -and $command.EntryPoint -eq "$toolId.dll") { Pass "runner dotnet, entry point $($command.EntryPoint)" }
            else { Fail "$id should use Runner=dotnet with EntryPoint=$toolId.dll; got Runner=$($command.Runner) EntryPoint=$($command.EntryPoint)" }
            if (-not $isSelfContained) { Pass 'framework-dependent' } else { Fail "$id should be framework-dependent but is self-contained" }
        }
        else {
            $expectedEntryPoint = if ($rid -like 'win-*') { "$toolId.exe" } else { $toolId }
            if ($command.Runner -eq 'executable' -and $command.EntryPoint -eq $expectedEntryPoint) { Pass "runner executable, entry point $expectedEntryPoint" }
            else { Fail "$id should use Runner=executable with EntryPoint=$expectedEntryPoint; got Runner=$($command.Runner) EntryPoint=$($command.EntryPoint)" }
            if (Test-Path (Join-Path $toolDir $expectedEntryPoint)) { Pass "apphost $expectedEntryPoint present" } else { Fail "$id is missing apphost $expectedEntryPoint" }
            if ($isSelfContained) { Pass 'self-contained' } else { Fail "$id should be self-contained but is framework-dependent" }
        }

        Test-AssemblyVersions $dir $id
    }
}
finally {
    Remove-Item -Recurse -Force -Path $extractRoot -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    Write-Host ''
    Write-Host "Package verification failed with $($failures.Count) problem(s):" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}

Write-Host ''
Write-Host 'Package verification passed.' -ForegroundColor Green
