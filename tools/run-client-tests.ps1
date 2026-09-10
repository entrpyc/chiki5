# Runs the Unity PlayMode tests headlessly (docs/plan.md command). The editor must be closed.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe'
$project = Join-Path $root 'client'
$results = Join-Path $root 'results.xml'

if (Test-Path (Join-Path $project 'Temp/UnityLockfile')) {
    throw 'The Unity editor has this project open. Close it before running headless tests.'
}
if (Test-Path $results) { Remove-Item $results -Force }

& $unity -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testResults $results -logFile (Join-Path $root 'client-tests.log') | Out-Null
$exit = $LASTEXITCODE

if (Test-Path $results) {
    [xml]$xml = Get-Content $results
    $run = $xml.'test-run'
    Write-Host ("PlayMode tests: {0} total, {1} passed, {2} failed, {3} skipped" -f $run.total, $run.passed, $run.failed, $run.skipped)
}
else {
    Write-Host 'No results.xml was produced; see client-tests.log.'
}
exit $exit
