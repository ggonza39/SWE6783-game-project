param([string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('pixel-campaign-checks-' + [guid]::NewGuid().ToString('N'))), [switch]$PresentationOnly)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$prRepo = Split-Path -Parent $PSScriptRoot
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ConeRenderingStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ConePolishChecks.cs'),
    (Join-Path $PSScriptRoot 'verification/CampaignChecks.cs'))
$prFiles += Get-ChildItem (Join-Path $prRepo 'src/Assets/Scripts') -Filter '*.cs' -File | ForEach-Object FullName
Add-Type -Path $prFiles -CompilerOptions '/define:VERIFY_CONE_MESH' -IgnoreWarnings -ErrorAction Stop
$prGeometry = Get-Content (Join-Path $PSScriptRoot 'crimson_level_geometry.json') -Raw
if ($PresentationOnly) {
    Write-Output ('PASS Crimson presentation: ' + [CampaignChecks]::Presentation($prGeometry,$OutputDirectory))
    return
}
Write-Output ('PASS campaign persistence: ' + [CampaignChecks]::Persistence((Join-Path $OutputDirectory 'persistence')))
Write-Output ('PASS campaign flow: ' + [CampaignChecks]::CampaignFlow($OutputDirectory))
$prGeometry = Get-Content (Join-Path $PSScriptRoot 'crimson_level_geometry.json') -Raw
Write-Output ('PASS Crimson mission: ' + [CampaignChecks]::Mission($prGeometry,$OutputDirectory))
Write-Output ('PASS campaign transition: ' + [CampaignChecks]::Transition($OutputDirectory))
Write-Output ('PASS Crimson patrols: ' + [CampaignChecks]::Patrols($prGeometry,$OutputDirectory))
Write-Output ('PASS campaign menu: ' + [CampaignChecks]::Menus($OutputDirectory))

Write-Output ('PASS Crimson presentation: ' + [CampaignChecks]::Presentation($prGeometry,$OutputDirectory))
