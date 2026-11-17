$ErrorActionPreference = 'Stop'
$prRepository = Split-Path -Parent $PSScriptRoot
$prSource = Join-Path $prRepository 'src/Assets/Scripts'
$prEditor = Join-Path $prRepository 'src/Assets/Editor'
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/EditorSceneProtectionStubs.cs'))
$prFiles += Get-ChildItem -LiteralPath $prSource -Filter '*.cs' -File |
    Where-Object { $_.BaseName -ne 'DetectionConeRenderer' } | ForEach-Object { $_.FullName }
foreach ($prName in @('EvidenceSetup','ExtractionSetup','GuardSetup','GuardDetectionSetup','SecurityCameraSetup','HidingSetup','WallColliderSetup','UXCampaignSceneSetup')) {
    $prFiles += Join-Path $prEditor ($prName + '.cs')
}
Add-Type -Path $prFiles -CompilerOptions /define:UNITY_EDITOR -IgnoreWarnings -ErrorAction Stop
Write-Output ('PASS editor callbacks: ' + [EditorSceneProtectionChecks]::Run() + ' skip all authored scenes; legacy setup remains available.')

$prEditorFixture = Join-Path ([IO.Path]::GetTempPath()) ('pixel-campaign-editor-' + [guid]::NewGuid().ToString('N'))
try {
    [CampaignEditorChecks]::Run($prEditorFixture)
    Write-Output 'PASS campaign build registration: correct entry order, other scenes retained, backup, no-op and Play-mode guards.'
} finally { if (Test-Path $prEditorFixture) { Remove-Item $prEditorFixture -Recurse -Force } }
