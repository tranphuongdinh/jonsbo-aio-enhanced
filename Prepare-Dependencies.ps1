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
