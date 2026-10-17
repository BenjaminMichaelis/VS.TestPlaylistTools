param(
    [Parameter(Mandatory)][string]$Feed,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$ExpectedRid,
    [string]$InstallRuntimeId
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest
$Feed = (Resolve-Path $Feed).Path
$root = Join-Path ([IO.Path]::GetTempPath()) "playlist-install-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $root | Out-Null
$saved = @{}
foreach ($name in 'NUGET_PACKAGES', 'DOTNET_RUNTIME_ID', 'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_ROOT_ARM64', 'PATH') {
    $saved[$name] = [Environment]::GetEnvironmentVariable($name)
}
try {
    $env:NUGET_PACKAGES = Join-Path $root 'cache'
    $config = Join-Path $root 'nuget.config'
    "<configuration><packageSources><clear/><add key=`"artifacts`" value=`"$([Security.SecurityElement]::Escape($Feed))`"/></packageSources></configuration>" | Set-Content $config
    $tools = Join-Path $root 'tools'
    if ($InstallRuntimeId) { $env:DOTNET_RUNTIME_ID = $InstallRuntimeId }
    else { Remove-Item Env:DOTNET_RUNTIME_ID -ErrorAction SilentlyContinue }
    dotnet tool install trx-to-vsplaylist --version $Version --tool-path $tools --configfile $config
    $store = Join-Path $tools ".store\trx-to-vsplaylist\$Version"
    $payloads = @(Get-ChildItem $store -Directory | Where-Object Name -like 'trx-to-vsplaylist.*')
    if ($payloads.Count -ne 1 -or $payloads[0].Name -ne "trx-to-vsplaylist.$ExpectedRid") {
        throw "Wrong installed payload: $($payloads.Name -join ', ')"
    }
    $exe = Join-Path $tools $(if ($IsWindows -and $ExpectedRid -ne 'any') { 'trx-to-vsplaylist.cmd' }
        elseif ($IsWindows) { 'trx-to-vsplaylist.exe' } else { 'trx-to-vsplaylist' })
    if ($ExpectedRid -ne 'any') {
        $empty = Join-Path $root 'no-runtime'
        New-Item -ItemType Directory $empty | Out-Null
        $env:DOTNET_ROOT = $empty
        $env:DOTNET_ROOT_X64 = $empty
        $env:DOTNET_ROOT_ARM64 = $empty
        # Invoke by absolute path with no managed host on PATH.
        $env:PATH = if ($IsWindows) { "$env:SystemRoot\System32;$env:SystemRoot" } else { '/usr/bin:/bin' }
    }
    & (Join-Path $PSScriptRoot 'Test-Cli.ps1') -Executable $exe -Version $Version
}
finally {
    foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name, $saved[$name]) }
    Remove-Item -LiteralPath $root -Recurse -Force
}
