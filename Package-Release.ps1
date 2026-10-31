param([string]$OutputZip = 'work\JonsboStudio-v1.0.0-win-x64.zip')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & .\build.ps1 -OutputName JonsboStudio.Update.exe
    $stage = Join-Path $PSScriptRoot ('work\release-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    Copy-Item JonsboStudio.Update.exe (Join-Path $stage 'JonsboStudio.exe')
    Copy-Item JonsboStudio.Update.exe.config (Join-Path $stage 'JonsboStudio.exe.config')
    Get-ChildItem Runtime -Filter '*.dll' | Copy-Item -Destination $stage
    foreach ($name in @('README.md','LICENSE','THIRD-PARTY-NOTICES.md','Licenses','Drivers')) { Copy-Item -LiteralPath $name -Destination $stage -Recurse }
    foreach ($name in @('DS339','FFmpeg','WebView2')) {
        $source = Join-Path 'Native' $name
        if (-not (Test-Path $source)) { throw "Missing dependency: $source. Run Prepare-Dependencies.ps1." }
        New-Item -ItemType Directory -Path (Join-Path $stage 'Native') -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination (Join-Path $stage 'Native') -Recurse
    }
    Copy-Item Native\dependencies.json (Join-Path $stage 'Native')
    $installer = Join-Path $stage 'Native\WebView2\MicrosoftEdgeWebView2RuntimeInstallerX64.exe'
    $manifest = Get-Content Native\dependencies.json -Raw | ConvertFrom-Json
    $signature = Get-AuthenticodeSignature $installer
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation' -or (Get-FileHash $installer).Hash -ne $manifest.webview2.sha256) { throw 'Invalid offline WebView2 installer.' }
    New-Item -ItemType Directory -Path (Join-Path $stage 'Assets') -Force | Out-Null
    foreach ($name in @('Fonts','Icon','Web','Brand','Setup')) { Copy-Item -LiteralPath (Join-Path 'Assets' $name) -Destination (Join-Path $stage 'Assets') -Recurse }
    Copy-Item Assets\public-themes.json (Join-Path $stage 'Assets')
    $output = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputZip))
    if (Test-Path -LiteralPath $output) { throw 'Choose a new output path; existing packages are not overwritten.' }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $output -CompressionLevel Optimal
    Write-Host "Package: $output"
    Write-Host "Clean stage: $stage"
    Get-FileHash $output | Format-List
} finally { Pop-Location }
