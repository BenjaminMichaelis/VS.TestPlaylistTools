param(
    [Parameter(Mandatory)][string]$Feed,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$RuntimeVersion,
    [Parameter(Mandatory)][string]$InstallScript,
    [switch]$Preview
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = Join-Path ([IO.Path]::GetTempPath()) "playlist-runtime-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $root | Out-Null
$saved = @{}
foreach ($name in 'DOTNET_ROOT', 'DOTNET_ROLL_FORWARD', 'DOTNET_ROLL_FORWARD_TO_PRERELEASE',
    'DOTNET_MULTILEVEL_LOOKUP', 'DOTNET_HOST_TRACE', 'DOTNET_HOST_TRACEFILE') {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}
try {
    $runtime = Join-Path $root 'runtime'
    if ($IsWindows) {
        & $InstallScript -Runtime dotnet -Version $RuntimeVersion -InstallDir $runtime -NoPath
    }
    else {
        & bash $InstallScript --runtime dotnet --version $RuntimeVersion --install-dir $runtime --no-path
    }
    $hostPath = Join-Path $runtime $(if ($IsWindows) { 'dotnet.exe' } else { 'dotnet' })
    $installed = @(& $hostPath --list-runtimes)
    if ($installed.Count -ne 1 -or $installed[0] -notlike "Microsoft.NETCore.App $RuntimeVersion *") {
        throw "Runtime root is not isolated: $($installed -join ', ')"
    }
    Write-Host "Testing portable package with $($installed[0])"
    $package = Join-Path $root 'package'
    [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path (Resolve-Path $Feed) "trx-to-vsplaylist.any.$Version.nupkg"), $package)
    $dll = @(Get-ChildItem $package -Recurse -Filter trx-to-vsplaylist.dll)
    if ($dll.Count -ne 1) { throw 'Missing or ambiguous portable entry point' }
    $env:DOTNET_ROOT = $runtime
    $env:DOTNET_MULTILEVEL_LOOKUP = '0'
    Remove-Item Env:DOTNET_ROLL_FORWARD -ErrorAction SilentlyContinue
    $env:DOTNET_ROLL_FORWARD_TO_PRERELEASE = if ($Preview) { '1' } else { '0' }
    $env:DOTNET_HOST_TRACE = '1'
    $env:DOTNET_HOST_TRACEFILE = Join-Path $root 'host-trace.txt'
    & (Join-Path $PSScriptRoot 'Test-Cli.ps1') -Executable $hostPath -PrefixArguments @($dll[0].FullName) -Version $Version
    $trace = Get-Content -Raw $env:DOTNET_HOST_TRACEFILE
    if (-not $trace.Contains($RuntimeVersion) -or -not $trace.Contains($runtime)) { throw 'Host trace did not confirm the isolated runtime' }
    Write-Host "Host trace confirmed $RuntimeVersion from $runtime"
}
finally {
    foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
    Remove-Item -LiteralPath $root -Recurse -Force
}
