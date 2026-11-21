#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs the packed NuGet packages from a local feed and checks that they work the way users consume them.

.DESCRIPTION
    Tool: installs the 'trx-to-vsplaylist' pointer package with `dotnet tool install`, asserts which
    RID-specific package the SDK resolved for this machine, then launches the installed command:
    `--version` must report the package version and `convert`/`merge` must produce playlists.

    Library (-Library): creates a throwaway console app outside the repo that consumes
    VSTestPlaylistTools.TrxToPlaylistConverter via PackageReference (net8.0 and net10.0), runs the AOT smoke
    test program against it and asserts the transitive VSTestPlaylistTools package resolved to the same version.

    Everything runs with an isolated nuget.config and NuGet package cache, so nothing is picked up from
    nuget.org or a previously populated cache for our own package IDs.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Feed,

    [Parameter(Mandatory)]
    [string]$Version,

    # RID-specific package the SDK is expected to choose on this machine (e.g. linux-x64, or 'any' for the
    # portable fallback). When omitted, the resolved package is only reported.
    [string]$ExpectedRid,

    # Pretend to be a different platform while installing (sets DOTNET_RUNTIME_ID for `dotnet tool install`).
    # Use an unlisted RID such as freebsd-x64 to exercise the portable 'any' fallback package.
    [string]$InstallRuntimeId,

    [switch]$Library,

    [string[]]$LibraryFrameworks = @('net8.0', 'net10.0')
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$toolId = 'trx-to-vsplaylist'
$toolCommand = 'trx-to-vsplaylist'
$Feed = (Resolve-Path $Feed).Path

$workDir = Join-Path ([System.IO.Path]::GetTempPath()) "package-smoke-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $workDir | Out-Null

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "FAIL: $message" }
    Write-Host "  ok: $message"
}

$originalPath = $env:PATH
$originalNuGetPackages = $env:NUGET_PACKAGES
try {
    # Our package IDs may only come from the local feed; third-party dependencies (e.g. TrxLib) from nuget.org.
    $nugetConfig = Join-Path $workDir 'nuget.config'
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$([System.Security.SecurityElement]::Escape($Feed))" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local">
      <package pattern="$toolId" />
      <package pattern="$toolId.*" />
      <package pattern="VSTestPlaylistTools" />
      <package pattern="VSTestPlaylistTools.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -Path $nugetConfig -Encoding utf8
    # Block repo-level MSBuild/CPM files in case the temp directory is ever nested under the repo.
    foreach ($file in 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props') {
        Set-Content -Path (Join-Path $workDir $file) -Value '<Project />' -Encoding utf8
    }
    $env:NUGET_PACKAGES = Join-Path $workDir 'packages'

    # --- Tool --------------------------------------------------------------------------------------
    Write-Host "Installing $toolId $Version from $Feed"
    $toolPath = Join-Path $workDir 'tools'
    $originalRuntimeId = $env:DOTNET_RUNTIME_ID
    if ($InstallRuntimeId) {
        Write-Host "  installing as if on $InstallRuntimeId (DOTNET_RUNTIME_ID)"
        $env:DOTNET_RUNTIME_ID = $InstallRuntimeId
    }
    Push-Location $workDir
    try {
        dotnet tool install $toolId --version $Version --tool-path $toolPath --configfile $nugetConfig
    }
    finally {
        Pop-Location
        $env:DOTNET_RUNTIME_ID = $originalRuntimeId
    }

    $storeDir = Join-Path $toolPath ".store/$toolId/$Version"
    $ridPackages = @(Get-ChildItem -Path $storeDir -Directory | Where-Object Name -like "$toolId.*" | ForEach-Object Name)
    Assert ($ridPackages.Count -eq 1) "exactly one RID-specific package installed (found: $($ridPackages -join ', '))"
    $resolvedRid = $ridPackages[0].Substring($toolId.Length + 1)
    Write-Host "  SDK resolved RID package: $($ridPackages[0])"
    Assert (Test-Path (Join-Path $storeDir "$($ridPackages[0])/$Version")) "resolved RID package version is $Version"
    if ($ExpectedRid) {
        Assert ($resolvedRid -eq $ExpectedRid) "resolved RID '$resolvedRid' is the expected '$ExpectedRid'"
    }

    # Invoke through PATH, exactly as a user would after installing the tool.
    $env:PATH = $toolPath + [System.IO.Path]::PathSeparator + $env:PATH

    Write-Host "Launching $toolCommand"
    $reportedVersion = (& $toolCommand --version | Out-String).Trim()
    Assert ($reportedVersion -eq $Version -or $reportedVersion -like "$Version+*") "'$toolCommand --version' reports '$reportedVersion' matching package version '$Version'"

    $helpOutput = (& $toolCommand --help | Out-String)
    Assert ($helpOutput -match 'convert' -and $helpOutput -match 'merge') "'$toolCommand --help' lists convert and merge commands"

    $dataDir = Join-Path $workDir 'data'
    New-Item -ItemType Directory -Path $dataDir | Out-Null
    $trxPath = Join-Path $dataDir 'results.trx'
    @'
<?xml version="1.0" encoding="UTF-8"?>
<TestRun id="run1" name="TestRun" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <TestDefinitions>
    <UnitTest name="TestMethod1" id="test1">
      <TestMethod className="MyNamespace.MyClass" name="TestMethod1" />
    </UnitTest>
    <UnitTest name="TestMethod2" id="test2">
      <TestMethod className="MyNamespace.MyClass" name="TestMethod2" />
    </UnitTest>
  </TestDefinitions>
  <Results>
    <UnitTestResult testId="test1" testName="TestMethod1" outcome="Passed" />
    <UnitTestResult testId="test2" testName="TestMethod2" outcome="Failed" />
  </Results>
</TestRun>
'@ | Set-Content -Path $trxPath -Encoding utf8

    $failedPlaylist = Join-Path $dataDir 'failed.playlist'
    & $toolCommand convert $trxPath --output $failedPlaylist --outcome Failed | Write-Host
    $failedContent = Get-Content -Raw -Path $failedPlaylist
    Assert ($failedContent -match 'MyNamespace\.MyClass\.TestMethod2' -and $failedContent -notmatch 'TestMethod1') "convert --outcome Failed produced a playlist with only the failed test"

    $passedPlaylist = Join-Path $dataDir 'passed.playlist'
    & $toolCommand convert $trxPath --output $passedPlaylist --outcome Passed | Write-Host

    $mergedPlaylist = Join-Path $workDir 'merged.playlist'
    & $toolCommand merge (Join-Path $dataDir '*.playlist') --output $mergedPlaylist | Write-Host
    $mergedContent = Get-Content -Raw -Path $mergedPlaylist
    Assert ($mergedContent -match 'MyNamespace\.MyClass\.TestMethod1' -and $mergedContent -match 'MyNamespace\.MyClass\.TestMethod2') "merge with a glob produced a playlist with both tests"

    # --- Library -----------------------------------------------------------------------------------
    if ($Library) {
        Write-Host "Consuming VSTestPlaylistTools.TrxToPlaylistConverter $Version via PackageReference"
        $consumerDir = Join-Path $workDir 'consumer'
        New-Item -ItemType Directory -Path $consumerDir | Out-Null
        $smokeProgram = Join-Path $repoRoot 'VSTestPlaylistTools.TrxToPlaylistConverter.AotSmokeTest/Program.cs'
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>$($LibraryFrameworks -join ';')</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="VSTestPlaylistTools.TrxToPlaylistConverter" Version="[$Version]" />
    <Compile Include="$([System.Security.SecurityElement]::Escape($smokeProgram))" Link="Program.cs" />
  </ItemGroup>
</Project>
"@ | Set-Content -Path (Join-Path $consumerDir 'Consumer.csproj') -Encoding utf8

        Push-Location $consumerDir
        try {
            dotnet restore --configfile $nugetConfig
            $assets = Get-Content -Raw -Path (Join-Path $consumerDir 'obj/project.assets.json') | ConvertFrom-Json
            $libraryKeys = @($assets.libraries.PSObject.Properties.Name)
            foreach ($packageId in 'VSTestPlaylistTools.TrxToPlaylistConverter', 'VSTestPlaylistTools') {
                Assert ($libraryKeys -contains "$packageId/$Version") "restore resolved $packageId $Version"
            }
            foreach ($framework in $LibraryFrameworks) {
                dotnet run --no-restore --framework $framework
                Assert $true "consumer ran successfully on $framework"
            }
        }
        finally {
            Pop-Location
        }
    }
}
finally {
    $env:PATH = $originalPath
    $env:NUGET_PACKAGES = $originalNuGetPackages
    Remove-Item -Recurse -Force -Path $workDir -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host 'Package smoke test passed.' -ForegroundColor Green
