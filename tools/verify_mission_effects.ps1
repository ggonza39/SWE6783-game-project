# Current source and serialized geometry with API stand-ins; human Unity playtest follows.
param([string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('pixel-effects-checks-' + [guid]::NewGuid().ToString('N'))))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$prRepo = Split-Path -Parent $PSScriptRoot
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ConeRenderingStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ConePolishChecks.cs'),
    (Join-Path $PSScriptRoot 'verification/CampaignChecks.cs'),
    (Join-Path $PSScriptRoot 'verification/MissionEffectsChecks.cs'))
$prFiles += Get-ChildItem (Join-Path $prRepo 'src/Assets/Scripts') -Filter '*.cs' -File | ForEach-Object FullName
Add-Type -Path $prFiles -CompilerOptions '/define:VERIFY_CONE_MESH' -IgnoreWarnings -ErrorAction Stop
$prGeometry = Get-Content (Join-Path $PSScriptRoot 'crimson_level_geometry.json') -Raw
Write-Output ('PASS mission effects: ' + [MissionEffectsChecks]::Run($prGeometry,$OutputDirectory))
