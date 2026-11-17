# Compiles the real telemetry source with stand-ins; does not send network requests.
param([string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('pixel-campaign-analytics-' + [guid]::NewGuid().ToString('N'))))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$prRepo = Split-Path -Parent $PSScriptRoot
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ConeRenderingStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/AnalyticsStubs.cs'))
$prFiles += Get-ChildItem (Join-Path $prRepo 'src/Assets/Scripts') -Filter '*.cs' -File | ForEach-Object FullName
$prFiles += Join-Path $prRepo 'src/Assets/Scripts/Analytics/GameplayMetrics.cs'
Add-Type -Path $prFiles -CompilerOptions '/define:VERIFY_ANALYTICS,VERIFY_CONE_MESH' -IgnoreWarnings -ErrorAction Stop
[UnityEngine.Application]::persistentDataPath = $OutputDirectory
$prMetrics = [UnityEngine.GameObject]::new('Metrics').AddComponent[GameplayMetrics]()
$prMetrics.GetType().GetMethod('Awake', [Reflection.BindingFlags]'NonPublic,Instance').Invoke($prMetrics,$null)
[GameplayMetrics]::SelectedDifficulty = 'Hard'
$prMetrics.StartSession()
$prFirstSession = $prMetrics.SessionID
[UXCampaign]::Begin([UXCampaign]::CrimsonLevelId,[UXDifficulty]::Hard) | Out-Null
[UXCampaign]::BindTelemetrySession()
[GameplayMetrics]::SelectedDifficulty = 'Easy'
[UnityEngine.Time]::deltaTime = 2
$prMetrics.GetType().GetMethod('Update', [Reflection.BindingFlags]'NonPublic,Instance').Invoke($prMetrics,$null)
[UXCampaign]::Tick(2)
$prMetrics.RecordGuardDetection(); $prMetrics.RecordCameraDetection(); $prMetrics.RecordEvidence(1)
if (-not [UXCampaign]::SecureSector(1)) { throw 'Checkpoint save failed.' }
[UXCampaign]::RecordDeath()
$prMetrics.EndSession('Lost')
$prUpload = [UnityEngine.JsonUtility]::LastJson | ConvertFrom-Json
if ($prUpload.Difficulty -ne 'Hard' -or $prUpload.SessionID -ne $prFirstSession -or
    $prUpload.TimeSeconds -ne 2 -or $prUpload.GuardDetections -ne 1 -or
    $prUpload.CameraDetections -ne 1 -or $prUpload.EvidenceCollected -ne 1) { throw 'Upload data did not retain mission context/counts.' }
$prMetrics.EndSession('Lost')
$prRows = @(Import-Csv (Join-Path $OutputDirectory 'gameplay_metrics.csv'))
if ($prRows.Count -ne 1 -or $prRows[0].Difficulty -ne 'Hard' -or $prRows[0].TimeSeconds -ne '2.00') { throw 'Main telemetry format/count changed.' }
[UXPlayerProgress].GetField('store',[Reflection.BindingFlags]'NonPublic,Static').SetValue($null,$null)
$prMetrics.StartSession()
$prCheckpoint = [UXCampaign]::Begin([UXCampaign]::CrimsonLevelId,[UXDifficulty]::Hard)
[UXCampaign]::BindTelemetrySession()
$prMetrics.RecordGuardDetection()
if ($prCheckpoint -ne 1 -or [UXCampaign]::TotalDetections -ne 3 -or [UXCampaign]::RunSeconds -ne 2) { throw 'Retry lost or doubled prior detections/time.' }
[UXCampaign]::SaveClock()
$prSaved = [UXPlayerProgress]::Data.FindCampaign([UXDifficulty]::Hard)
if ($prSaved.detections -ne 3) { throw 'Cumulative detections were not saved.' }
[UXCampaign]::RestartLevel([UXCampaign]::CrimsonLevelId,[UXDifficulty]::Hard)
$prMetrics.StartSession()
[UXCampaign]::Begin([UXCampaign]::CrimsonLevelId,[UXDifficulty]::Hard) | Out-Null
if ([UXCampaign]::TotalDetections -ne 0) { throw 'Restart Level retained previous detection totals.' }
$prContext = @(Import-Csv (Join-Path $OutputDirectory 'campaign_events.csv'))
$prFirst = $prContext | Where-Object { $_.Event -eq 'SectorSecured' } | Select-Object -First 1
if ($prFirst.SessionID -ne $prFirstSession -or $prFirst.PlayerID -ne $prMetrics.PlayerID -or
    $prFirst.LevelId -ne 'crimson_02' -or $prFirst.Difficulty -ne 'Hard' -or $prFirst.Checkpoint -ne '1' -or
    $prFirst.RunDetections -ne '2') { throw 'Local campaign events do not join to the session correctly.' }
Write-Output 'PASS real telemetry source: unchanged CSV/upload fields, captured mode, matching session IDs, no duplicate results; checkpoint detections survive reload and reset correctly.'
