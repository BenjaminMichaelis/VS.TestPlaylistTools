param(
    [Parameter(Mandatory)][string]$Feed,
    [Parameter(Mandatory)][string]$Version,
    [switch]$Partial
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$Feed = (Resolve-Path $Feed).Path
$project = Join-Path $PSScriptRoot '..\..\trx-to-vsplaylist\trx-to-vsplaylist.csproj'
$rids = ((dotnet msbuild $project -getProperty:ToolPackageRuntimeIdentifiers) | Out-String).Trim().Split(';')
$nativeRids = ((dotnet msbuild $project -getProperty:NativeAotRuntimeIdentifiers) | Out-String).Trim().Split(';')
if ($rids.Count -ne 9 -or $nativeRids.Count -ne 6) { throw 'Unexpected RID inventory; review the release matrix' }
$id = 'trx-to-vsplaylist'
$expected = @("$id.$Version.nupkg") + @($rids | ForEach-Object { "$id.$_.$Version.nupkg" })
$actual = @(Get-ChildItem $Feed -Filter "$id*.nupkg" | ForEach-Object Name)
if (-not $actual.Count -or @($actual | Where-Object { $_ -notin $expected }).Count -or
    (-not $Partial -and @(Compare-Object $expected $actual).Count)) {
    throw "Tool package inventory mismatch: $($actual -join ', ')"
}
$root = Join-Path ([IO.Path]::GetTempPath()) "playlist-inventory-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $root | Out-Null
try {
    foreach ($rid in @('pointer') + $rids) {
        $packageId = if ($rid -eq 'pointer') { $id } else { "$id.$rid" }
        if ($Partial -and "$packageId.$Version.nupkg" -notin $actual) { continue }
        $dir = Join-Path $root $rid
        [IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $Feed "$packageId.$Version.nupkg"), $dir)
        [xml]$nuspec = Get-Content -Raw (Join-Path $dir "$packageId.nuspec")
        if ($nuspec.package.metadata.id -ne $packageId -or $nuspec.package.metadata.version -ne $Version) {
            throw "Incorrect package identity/version: $packageId"
        }
        $expectedType = if ($rid -eq 'pointer') { 'DotnetTool' } else { 'DotnetToolRidPackage' }
        if ($nuspec.package.metadata.packageTypes.packageType.name -ne $expectedType) {
            throw "Incorrect package type: $packageId"
        }
        $settings = @(Get-ChildItem $dir -Recurse -Filter DotnetToolSettings.xml)
        if ($settings.Count -ne 1) { throw "Invalid tool settings in $packageId" }
        [xml]$xml = Get-Content -Raw $settings[0].FullName
        $command = $xml.DotNetCliTool.Commands.Command
        if ($command.Name -ne $id) { throw "Incorrect command in $packageId" }
        $toolDir = $settings[0].Directory.FullName
        if ($rid -eq 'pointer') {
            $refs = @($xml.DotNetCliTool.RuntimeIdentifierPackages.RuntimeIdentifierPackage)
            if (@(Compare-Object $rids @($refs.RuntimeIdentifier)).Count) { throw 'Pointer RID list mismatch' }
            foreach ($ref in $refs) {
                if ($ref.Id -ne "$id.$($ref.RuntimeIdentifier)" -or
                    ($ref.HasAttribute('Version') -and $ref.Version -ne $Version)) { throw 'Incorrect pointer reference' }
            }
            if (@(Get-ChildItem $toolDir -Recurse -File | Where-Object Extension -in '.dll', '.exe', '').Count) {
                throw 'Pointer contains binaries'
            }
            continue
        }
        if ($nativeRids -contains $rid) {
            if (Test-Path (Join-Path $Feed "$packageId.$Version.snupkg")) {
                throw "Native debug symbols must not be published as a Portable PDB package: $rid"
            }
            $entry = if ($rid.StartsWith('win-')) { "$id.exe" } else { $id }
            if ($command.Runner -ne 'executable' -or $command.EntryPoint -ne $entry) { throw "Not executable: $rid" }
            $binary = Join-Path $toolDir $entry
            if (-not (Test-Path $binary)) { throw "Missing native binary: $rid" }
            if (@(Get-ChildItem $toolDir -Filter '*.dll').Count -or
                (Test-Path (Join-Path $toolDir "$id.runtimeconfig.json"))) { throw "Managed payload advertised as AOT: $rid" }
            $bytes = [IO.File]::ReadAllBytes($binary)
            if ($rid.StartsWith('win-')) {
                $pe = [BitConverter]::ToInt32($bytes, 0x3c)
                $machine = [BitConverter]::ToUInt16($bytes, $pe + 4)
                $expectedMachine = if ($rid.EndsWith('arm64')) { 0xaa64 } else { 0x8664 }
                if ($bytes[0] -ne 0x4d -or $machine -ne $expectedMachine) { throw "Incorrect PE architecture: $rid" }
                if ([BitConverter]::ToUInt32($bytes, $pe + 24 + 112 + 14 * 8) -ne 0) { throw "Managed PE: $rid" }
            }
            elseif ($rid.StartsWith('linux-')) {
                $machine = [BitConverter]::ToUInt16($bytes, 18)
                $expectedMachine = if ($rid.EndsWith('arm64')) { 183 } else { 62 }
                if ($bytes[0] -ne 0x7f -or $bytes[1] -ne 0x45 -or $machine -ne $expectedMachine) { throw "Incorrect ELF architecture: $rid" }
            }
            else {
                $cpu = [BitConverter]::ToUInt32($bytes, 4)
                $expectedCpu = if ($rid.EndsWith('arm64')) { 0x0100000c } else { 0x01000007 }
                if ([BitConverter]::ToUInt32($bytes, 0) -ne 0xfeedfacfu -or $cpu -ne $expectedCpu) { throw "Incorrect Mach-O architecture: $rid" }
            }
        }
        else {
            $config = Get-Content -Raw (Join-Path $toolDir "$id.runtimeconfig.json") | ConvertFrom-Json
            if ($rid -eq 'any') {
                if ($command.Runner -ne 'dotnet' -or $command.EntryPoint -ne "$id.dll" -or
                    $config.runtimeOptions.rollForward -ne 'Major' -or
                    $config.runtimeOptions.framework.name -ne 'Microsoft.NETCore.App' -or
                    $config.runtimeOptions.tfm -ne 'net10.0') { throw 'Incorrect portable fallback configuration' }
            }
            elseif (-not $config.runtimeOptions.includedFrameworks) { throw "musl payload must remain self-contained: $rid" }
        }
        Write-Host "$packageId checked ($((Get-Item (Join-Path $Feed "$packageId.$Version.nupkg")).Length) bytes)"
    }
}
finally { Remove-Item -LiteralPath $root -Recurse -Force }
