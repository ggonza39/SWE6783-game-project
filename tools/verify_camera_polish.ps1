param([string]$OutputDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('pixel-cone-checks-' + [guid]::NewGuid().ToString('N'))))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$prRepo = Split-Path -Parent $PSScriptRoot
$prFiles = @((Join-Path $PSScriptRoot 'verification/UnityVerificationStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ConeRenderingStubs.cs'),
    (Join-Path $PSScriptRoot 'verification/ConePolishChecks.cs'))
$prFiles += Get-ChildItem (Join-Path $prRepo 'src/Assets/Scripts') -Filter '*.cs' -File | ForEach-Object FullName
Add-Type -Path $prFiles -CompilerOptions '/define:VERIFY_CONE_MESH' -IgnoreWarnings -ErrorAction Stop
[UnityEngine.Application]::persistentDataPath = Join-Path $OutputDirectory ('isolated-profile-' + [guid]::NewGuid().ToString('N'))
Write-Output ('PASS camera lifecycle: ' + [ConePolishChecks]::CameraLifecycle())
Write-Output ('PASS cone geometry: ' + [ConePolishChecks]::MeshAndOcclusion($OutputDirectory))
Write-Output ('PASS presentation: ' + [ConePolishChecks]::Presentation($OutputDirectory))
