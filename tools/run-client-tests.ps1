# Runs the Unity client suites headlessly (docs/plan.md P1.1): EditMode first, then PlayMode,
# in two batch-mode invocations. Each writes its own results file and log; one summary line is
# printed per platform. The editor must be closed. Exits non-zero when either platform fails or
# writes no results.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
$project = Join-Path $root 'client'

if (Test-Path (Join-Path $project 'Temp/UnityLockfile')) {
    throw 'The Unity editor has this project open. Close it before running headless tests.'
}

$platforms = @(
    @{ Name = 'EditMode'; Results = (Join-Path $root 'results-editmode.xml'); Log = (Join-Path $root 'client-tests-editmode.log') },
    @{ Name = 'PlayMode'; Results = (Join-Path $root 'results-playmode.xml'); Log = (Join-Path $root 'client-tests-playmode.log') }
)

$failed = $false
foreach ($platform in $platforms) {
    if (Test-Path $platform.Results) { Remove-Item $platform.Results -Force }

    & $unity -batchmode -nographics -projectPath $project -runTests -testPlatform $platform.Name -testResults $platform.Results -logFile $platform.Log | Out-Null
    $exit = $LASTEXITCODE

    if (Test-Path $platform.Results) {
        [xml]$xml = Get-Content $platform.Results
        $run = $xml.'test-run'
        Write-Host ("{0} tests: {1} total, {2} passed, {3} failed, {4} skipped, {5} inconclusive" -f $platform.Name, $run.total, $run.passed, $run.failed, $run.skipped, $run.inconclusive)
        # A test that is not green is a failure of the platform: skipped and inconclusive included.
        if ([int]$run.failed -gt 0 -or [int]$run.skipped -gt 0 -or [int]$run.inconclusive -gt 0) { $failed = $true }
    }
    else {
        Write-Host ("{0} tests: no results were produced; see {1}." -f $platform.Name, $platform.Log)
        $failed = $true
    }

    if ($exit -ne 0) { $failed = $true }
}

if ($failed) { exit 1 }
exit 0
