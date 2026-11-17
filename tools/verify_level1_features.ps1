param([string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('pixel-level1-checks-' + [guid]::NewGuid().ToString('N'))))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$prRepo = Split-Path -Parent $PSScriptRoot
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/Level1FeatureChecks.cs'))
$prFiles += Get-ChildItem (Join-Path $prRepo 'src/Assets/Scripts') -Filter '*.cs' -File |
    Where-Object BaseName -ne 'DetectionConeRenderer' | ForEach-Object FullName
Add-Type -Path $prFiles -IgnoreWarnings -ErrorAction Stop
$prSession = Join-Path $OutputDirectory ('isolated-' + [guid]::NewGuid().ToString('N'))
[UnityEngine.Application]::persistentDataPath = Join-Path $prSession 'runtime-profile'
Write-Output ('PASS save files: ' + [Level1FeatureChecks]::Persistence((Join-Path $prSession 'save-tests')))
$prGeometry = Get-Content (Join-Path $PSScriptRoot 'expanded_level_geometry.json') -Raw | ConvertFrom-Json
$prBoxes = @()
foreach ($prWall in $prGeometry.walls) {
    if ($prWall.name -ne 'gate') { $prBoxes += @($prWall.x,$prWall.y,$prWall.w,$prWall.h) }
}
Write-Output ('PASS Hard mission: ' + [Level1FeatureChecks]::HardMission([float[]]$prBoxes))
Write-Output ('PASS guard pressure: ' + [Level1FeatureChecks]::GuardPressure())
Write-Output ('PASS menu: ' + [Level1FeatureChecks]::Menu($OutputDirectory))
