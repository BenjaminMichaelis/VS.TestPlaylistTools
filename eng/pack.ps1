#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs libraries, the tool pointer, or one RID payload.

.DESCRIPTION
    Native AOT payloads must be packed separately on matching OS/architecture runners.

    -p:Version (not just -p:PackageVersion) is used so the assembly/informational version and the
    tool's `--version` output match the package version.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$Output,

    [Parameter(Mandatory)]
    [ValidateSet('Libraries', 'Pointer', 'Payload')]
    [string]$Mode,

    [string]$RuntimeIdentifier,

    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Split-Path -Parent $PSScriptRoot
$tool = Join-Path $repoRoot 'trx-to-vsplaylist\trx-to-vsplaylist.csproj'
if (($Mode -eq 'Payload') -ne [bool]$RuntimeIdentifier) {
    throw 'Specify RuntimeIdentifier exactly when Mode is Payload.'
}
$arguments = @('--configuration', $Configuration, '--output', $Output,
    "-p:Version=$Version", '-p:ContinuousIntegrationBuild=true')
if ($Mode -eq 'Libraries') {
    foreach ($project in 'VSTestPlaylistTools', 'VSTestPlaylistTools.TrxToPlaylistConverter') {
        dotnet pack (Join-Path $repoRoot "$project\$project.csproj") @arguments
    }
}
else {
    if ($RuntimeIdentifier) { $arguments += @('-r', $RuntimeIdentifier) }
    dotnet pack $tool @arguments
}

Write-Host "Packed version $Version to $Output"
Get-ChildItem -Path $Output -Filter '*.nupkg' | Sort-Object Name | ForEach-Object { Write-Host "  $($_.Name)" }
