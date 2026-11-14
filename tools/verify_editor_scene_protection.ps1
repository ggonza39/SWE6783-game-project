$ErrorActionPreference = 'Stop'
$prRepository = Split-Path -Parent $PSScriptRoot
$prSource = Join-Path $prRepository 'src/Assets/Scripts'
$prEditor = Join-Path $prRepository 'src/Assets/Editor'
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/EditorSceneProtectionStubs.cs'))
$prFiles += Get-ChildItem -LiteralPath $prSource -Filter '*.cs' -File |
    Where-Object { $_.BaseName -ne 'DetectionConeRenderer' } | ForEach-Object { $_.FullName }
foreach ($prName in @('EvidenceSetup','ExtractionSetup','GuardSetup','GuardDetectionSetup','SecurityCameraSetup','HidingSetup','WallColliderSetup')) {
    $prFiles += Join-Path $prEditor ($prName + '.cs')
}
Add-Type -Path $prFiles -IgnoreWarnings -ErrorAction Stop
Write-Output ('PASS editor callbacks: ' + [EditorSceneProtectionChecks]::Run() + ' skip the expanded scene; legacy setup remains available.')
