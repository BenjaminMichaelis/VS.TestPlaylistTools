param([string]$Output = $env:GITHUB_OUTPUT)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$index = Invoke-RestMethod 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json'
$channels = @($index.'releases-index')
if (-not $channels.Count) { throw 'Official runtime release index is empty' }
$stable = @($channels | Where-Object {
    [version]$_.'channel-version' -ge [version]'10.0' -and
    $_.'support-phase' -in @('active', 'maintenance')
} | Sort-Object { [version]$_.'channel-version' })
foreach ($entry in $stable) {
    if ($entry.'latest-runtime' -notmatch '^\d+\.\d+\.\d+$' -or
        ([version]$entry.'latest-runtime').Major -ne ([version]$entry.'channel-version').Major) {
        throw "Invalid stable runtime metadata for $($entry.'channel-version')"
    }
}
if (-not $stable.Count -or $stable.'channel-version' -notcontains '10.0') {
    throw 'Release metadata must contain the supported .NET 10 baseline; review the target framework policy'
}
$nextMajor = ([version]$stable[-1].'channel-version').Major + 1
$preview = @($channels | Where-Object {
    $_.'channel-version' -eq "$nextMajor.0" -and
    $_.'support-phase' -in @('preview', 'go-live')
})
if ($preview.Count -gt 1) { throw 'Ambiguous next-major preview metadata' }
if ($preview.Count -and $preview[0].'latest-runtime' -notmatch "^$nextMajor\.0\.\d+-(preview|rc)\.") {
    throw 'Invalid next-major preview runtime metadata'
}
foreach ($entry in $stable + $preview) {
    Write-Host "$($entry.'channel-version'): $($entry.'latest-runtime') ($($entry.'support-phase'))"
}
$resolved = @{
    stable = @($stable | ForEach-Object { $_.'latest-runtime' })
    preview = @($preview | ForEach-Object { $_.'latest-runtime' })
}
$json = ConvertTo-Json -InputObject $resolved -Compress
if ($Output) { "runtimes=$json" | Add-Content $Output }
$json
