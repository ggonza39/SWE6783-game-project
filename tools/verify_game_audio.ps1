# Network-free real source/event checks. Uses an inert metrics stand-in.
param([string]$OutputDirectory=(Join-Path ([IO.Path]::GetTempPath()) ('pixel-audio-checks-'+[guid]::NewGuid().ToString('N'))))
$ErrorActionPreference='Stop'
[IO.Directory]::CreateDirectory($OutputDirectory)|Out-Null
$repo=Split-Path -Parent $PSScriptRoot
$names=@('UnityVerificationStubs.cs','ConeRenderingStubs.cs','ConePolishChecks.cs','CampaignChecks.cs','GameAudioChecks.cs')
$files=@($names|ForEach-Object{Join-Path (Join-Path $PSScriptRoot 'verification') $_})
# Non-recursive by design: never compile or execute Analytics/GameplayMetrics.cs.
$files+=@(Get-ChildItem (Join-Path $repo 'src/Assets/Scripts') -Filter '*.cs' -File|ForEach-Object FullName)
Add-Type -Path $files -CompilerOptions '/define:VERIFY_CONE_MESH' -IgnoreWarnings
$geometry=Get-Content (Join-Path $PSScriptRoot 'crimson_level_geometry.json') -Raw
Write-Output ('PASS game audio: '+[GameAudioChecks]::Run($geometry,$OutputDirectory))
