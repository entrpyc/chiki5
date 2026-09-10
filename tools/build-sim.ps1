# Builds the Chiki.Sim rules library and copies the DLL into the Unity client.
# Re-run every time the simulation changes; the client never compiles simulation source directly.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet build sim/Chiki.Sim -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
    $target = Join-Path $root 'client/Assets/_Project/Plugins/Chiki.Sim'
    New-Item -ItemType Directory -Force $target | Out-Null
    Copy-Item sim/Chiki.Sim/bin/Release/netstandard2.1/Chiki.Sim.dll $target -Force
    Write-Host "Copied Chiki.Sim.dll to $target"
}
finally {
    Pop-Location
}
