# Type and isolated integration checks; run Unity tests/playtest for engine verification.
param([string]$DrawOutputDirectory = $env:TEMP)
$ErrorActionPreference = 'Stop'
$prRepository = Split-Path -Parent $PSScriptRoot
$prSource = Join-Path $prRepository 'src/Assets/Scripts'
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ExpandedRuntimeChecks.cs'),
    (Join-Path $PSScriptRoot 'verification/SolidHidingChecks.cs'))
$prFiles += Get-ChildItem -LiteralPath $prSource -Filter '*.cs' -File |
    Where-Object { $_.BaseName -ne 'DetectionConeRenderer' } | ForEach-Object { $_.FullName }
Add-Type -Path $prFiles -IgnoreWarnings -ErrorAction Stop
Write-Output 'PASS runtime source type checks against isolated stand-ins.'
Write-Output ('PASS isolated interactions: ' + [ExpandedRuntimeChecks]::Interactions())
$prGeometry = Get-Content (Join-Path $PSScriptRoot 'expanded_level_geometry.json') -Raw | ConvertFrom-Json
$prBoxes = @()
foreach ($prWall in $prGeometry.walls) {
    if ($prWall.name -ne 'gate') { $prBoxes += @($prWall.x, $prWall.y, $prWall.w, $prWall.h) }
}
[ExpandedRuntimeChecks]::NavigationReturns([float[]]$prBoxes)
Write-Output 'PASS isolated pursuit and return across the saved facility geometry.'
$prRoutes = @()
foreach ($prRoute in $prGeometry.routes) {
    foreach ($prPoint in $prRoute) { $prRoutes += @($prPoint[0], $prPoint[1]) }
}
Write-Output ('PASS isolated solid cover and guard movement: ' + [SolidHidingChecks]::Run([float[]]$prBoxes, [float[]]$prRoutes))
[UnityEngine.Object]::All.Clear()
foreach ($prWall in $prGeometry.walls) {
    $prGo = [UnityEngine.GameObject]::new($(if ($prWall.name -eq 'gate') { 'AccessGate' } else { 'Wall' }))
    $prGo.transform.position = [UnityEngine.Vector3]::new($prWall.x, $prWall.y, 0)
    $prBox = $prGo.AddComponent[UnityEngine.BoxCollider2D]()
    $prBox.size = [UnityEngine.Vector2]::new($prWall.w, $prWall.h)
}
foreach ($prSpec in @(@('Player', -12, -6), @('Terminal', 12, 6), @('ExtractionZone', 11, -6), @('AccessKeycard', -11, 6))) {
    $prGo = [UnityEngine.GameObject]::new($prSpec[0])
    $prGo.transform.position = [UnityEngine.Vector3]::new($prSpec[1], $prSpec[2], 0)
}
foreach ($prPoint in @(@(3, 6), @(4, -6), @(12, 3))) {
    $prGo = [UnityEngine.GameObject]::new('Evidence')
    $prGo.transform.position = [UnityEngine.Vector3]::new($prPoint[0], $prPoint[1], 0)
    $null = $prGo.AddComponent[EvidenceCollectible]()
}
foreach ($prRoute in $prGeometry.routes) {
    $prGo = [UnityEngine.GameObject]::new('Guard')
    $prGo.transform.position = [UnityEngine.Vector3]::new($prRoute[0][0], $prRoute[0][1], 0)
    $null = $prGo.AddComponent[GuardDetection]()
}
$prBriefing = [UXMissionBriefingScreen]::new(3, $true)
foreach ($prSize in @(@(960, 540), @(1280, 720), @(1920, 1080))) {
    [UnityEngine.Screen]::width = $prSize[0]; [UnityEngine.Screen]::height = $prSize[1]
    [UnityEngine.GUI]::Calls.Clear(); $null = $prBriefing.Draw()
    foreach ($prCall in [UnityEngine.GUI]::Calls) {
        if ($prCall.X -lt -1 -or $prCall.Y -lt -1 -or
            ($prCall.X + $prCall.W) -gt ($prSize[0] + 1) -or
            ($prCall.Y + $prCall.H) -gt ($prSize[1] + 1)) { throw ('Briefing overflow: ' + $prCall.Text) }
    }
    if ($DrawOutputDirectory) {
        $prOutput = Join-Path $DrawOutputDirectory ('expanded_briefing_' + $prSize[0] + 'x' + $prSize[1] + '.json')
        [UnityEngine.GUI]::Calls | ConvertTo-Json -Depth 4 | Set-Content $prOutput
    }
    Write-Output ('PASS briefing bounds: ' + $prSize[0] + 'x' + $prSize[1])
}
