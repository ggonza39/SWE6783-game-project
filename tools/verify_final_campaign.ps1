# Offline source/map checks; Unity physics/rendering and executable acceptance are separate.
param([string]$OutputDirectory=(Join-Path ([IO.Path]::GetTempPath()) ('pixel-final-checks-'+[guid]::NewGuid().ToString('N'))))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force|Out-Null
$prRepo=Split-Path -Parent $PSScriptRoot
$prFiles=@('UnityVerificationStubs.cs','ConeRenderingStubs.cs','ConePolishChecks.cs','CampaignChecks.cs','FinalCampaignChecks.cs')|ForEach-Object {Join-Path (Join-Path $PSScriptRoot 'verification') $_}
# Deliberately non-recursive: uses the empty GameplayMetrics stand-in and excludes upload-capable Analytics/GameplayMetrics.cs.
$prFiles+=Get-ChildItem (Join-Path $prRepo 'src/Assets/Scripts') -Filter '*.cs' -File|ForEach-Object FullName
Add-Type -Path $prFiles -CompilerOptions '/define:VERIFY_CONE_MESH' -IgnoreWarnings -ErrorAction Stop
$prGeometry=Get-Content (Join-Path $PSScriptRoot 'crimson_level_geometry.json') -Raw
Write-Output ('PASS final campaign: '+[FinalCampaignChecks]::Run($prGeometry,$OutputDirectory))
