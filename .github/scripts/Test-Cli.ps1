param(
    [Parameter(Mandatory)][string]$Executable,
    [string[]]$PrefixArguments = @(),
    [string]$Version
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$work = Join-Path ([IO.Path]::GetTempPath()) "playlist-cli-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $work | Out-Null
function Invoke-Cli([string[]]$Arguments, [switch]$Failure) {
    $info = [Diagnostics.ProcessStartInfo]::new($Executable)
    $info.UseShellExecute = $false
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    if ($IsWindows -and $Executable.EndsWith('.cmd')) {
        $info.FileName = $env:ComSpec
        $quoted = @(@($PrefixArguments) + $Arguments | ForEach-Object { '"' + $_.Replace('"', '""') + '"' })
        $info.Arguments = '/d /s /c ""' + $Executable + '" ' + ($quoted -join ' ') + '"'
    }
    else {
        foreach ($argument in @($PrefixArguments) + $Arguments) { $info.ArgumentList.Add($argument) }
    }
    $process = [Diagnostics.Process]::Start($info)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(60000)) {
        Stop-Process -Id $process.Id -Force
        throw "CLI timed out: $($Arguments -join ' ')"
    }
    $text = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
    if (($Failure -and $process.ExitCode -eq 0) -or (-not $Failure -and $process.ExitCode -ne 0)) {
        throw "Unexpected exit $($process.ExitCode) for '$($Arguments -join ' ')': $text"
    }
    Write-Host $text.Trim()
    $process.Dispose()
    return $text
}
function Assert-Tests([string]$Path, [string[]]$Names) {
    [xml]$xml = Get-Content -Raw $Path
    $actual = @($xml.SelectNodes('/Playlist/Add') | ForEach-Object { $_.GetAttribute('Test') } | Sort-Object)
    $expected = @($Names | Sort-Object)
    if (($actual -join '|') -cne ($expected -join '|')) {
        throw "Unexpected tests in ${Path}: $($actual -join ', '); expected $($expected -join ', ')"
    }
}
try {
    $help = Invoke-Cli @('--help')
    if ($help -notmatch 'convert' -or $help -notmatch 'merge') { throw 'Missing commands in help' }
    if ($Version) {
        $reported = (Invoke-Cli @('--version')).Trim()
        if ($reported -ne $Version -and $reported -notlike "$Version+*") { throw "Wrong version: $reported" }
    }
    $trx = Join-Path $work 'first.trx'
    @'
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <TestDefinitions>
    <UnitTest name="One" id="one"><TestMethod className="Ns.Tests" name="One" /></UnitTest>
    <UnitTest name="Two" id="two"><TestMethod className="Ns.Tests" name="Two" /></UnitTest>
  </TestDefinitions>
  <Results>
    <UnitTestResult testId="one" testName="One" outcome="Passed" />
    <UnitTestResult testId="two" testName="Two" outcome="Failed" />
  </Results>
</TestRun>
'@ | Set-Content $trx
    $second = Join-Path $work 'second.trx'
    Copy-Item $trx $second
    $all = Join-Path $work 'all.playlist'
    $failed = Join-Path $work 'failed.playlist'
    $merged = Join-Path $work 'merged.playlist'
    Invoke-Cli @('convert', $trx, '--output', $all) | Out-Null
    Assert-Tests $all @('Ns.Tests.One', 'Ns.Tests.Two')
    Invoke-Cli @('convert', $trx, '--outcome', 'Failed', '--output', $failed) | Out-Null
    Assert-Tests $failed @('Ns.Tests.Two')
    Invoke-Cli @('convert', $trx, $second, '--output', $merged) | Out-Null
    Assert-Tests $merged @('Ns.Tests.One', 'Ns.Tests.Two')
    $separate = Join-Path $work 'separate'
    Invoke-Cli @('convert', $trx, $second, '--separate', '--output', $separate) | Out-Null
    Assert-Tests (Join-Path $separate 'first.playlist') @('Ns.Tests.One', 'Ns.Tests.Two')
    Assert-Tests (Join-Path $separate 'second.playlist') @('Ns.Tests.One', 'Ns.Tests.Two')
    $empty = Join-Path $work 'empty.playlist'
    Invoke-Cli @('convert', $trx, '--outcome', 'NotExecuted', '--skip-empty', '--output', $empty) | Out-Null
    if (Test-Path $empty) { throw 'skip-empty created an output file' }
    Invoke-Cli @('merge', (Join-Path $separate '*.playlist'), '--output', $merged) | Out-Null
    Assert-Tests $merged @('Ns.Tests.One', 'Ns.Tests.Two')
    Invoke-Cli @('convert', (Join-Path $work 'missing.trx')) -Failure | Out-Null
    Invoke-Cli @('convert', $trx, '--outcome', 'InvalidOutcome') -Failure | Out-Null
    Invoke-Cli @('--not-an-option') -Failure | Out-Null
    $bad = Join-Path $work 'bad.trx'
    '<TestRun><invalid' | Set-Content $bad
    # TrxLib currently treats malformed TRX as an empty run; preserve that behavior.
    Invoke-Cli @('convert', $bad, '--output', $empty) | Out-Null
    Assert-Tests $empty @()
    Write-Host 'CLI artifact smoke scenarios passed.'
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force
}
