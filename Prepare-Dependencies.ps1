$ErrorActionPreference = 'Stop'
$manifest = Get-Content (Join-Path $PSScriptRoot 'Native\dependencies.json') -Raw | ConvertFrom-Json
$work = Join-Path $PSScriptRoot ('work\dependencies-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force | Out-Null
$archive = Join-Path $work 'ffmpeg.zip'
Invoke-WebRequest -Uri $manifest.ffmpeg.url -OutFile $archive -UseBasicParsing
if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne $manifest.ffmpeg.sha256) {
    throw 'FFmpeg download differs from the pinned dependency manifest. Review the new upstream build before updating the manifest.'
}
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $work 'extracted')
$bundle = Get-ChildItem (Join-Path $work 'extracted') -Directory | Select-Object -First 1
$destination = Join-Path $PSScriptRoot 'Native\FFmpeg'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $bundle.FullName 'bin\ffmpeg.exe') -Destination $destination -Force
Get-ChildItem (Join-Path $bundle.FullName 'bin') -Filter '*.dll' | Copy-Item -Destination $destination -Force
Write-Host 'Standalone video runtime is ready.'
$webViewFolder = Join-Path $PSScriptRoot 'Native\WebView2'
New-Item -ItemType Directory -Path $webViewFolder -Force | Out-Null
$installer = Join-Path $webViewFolder 'MicrosoftEdgeWebView2RuntimeInstallerX64.exe'
Invoke-WebRequest -Uri $manifest.webview2.url -OutFile $installer -UseBasicParsing
if ((Get-FileHash $installer -Algorithm SHA256).Hash -ne $manifest.webview2.sha256) { throw 'WebView2 installer changed. Review the signed upstream release before updating the pinned hash.' }
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw 'Invalid WebView2 installer signature.' }
Write-Host 'Standalone WebView2 installer is ready.'
$sensorFolder = Join-Path $PSScriptRoot 'Native\Sensors'
New-Item -ItemType Directory -Path $sensorFolder -Force | Out-Null
$sensorInstaller = Join-Path $sensorFolder 'PawnIO_setup.exe'
Invoke-WebRequest $manifest.pawnio.url -OutFile $sensorInstaller -UseBasicParsing
if ((Get-FileHash $sensorInstaller).Hash -ne $manifest.pawnio.sha256) { throw 'PawnIO installer differs from the pinned manifest.' }
$sensorSignature = Get-AuthenticodeSignature $sensorInstaller
if ($sensorSignature.Status -ne 'Valid' -or $sensorSignature.SignerCertificate.Subject -notmatch 'CN=namazso.eu') { throw 'Invalid PawnIO installer signature.' }
Write-Host 'Signed hardware sensor installer is ready.'
