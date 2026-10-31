param([Parameter(Mandatory=$true)][string]$Zip)
$ErrorActionPreference = 'Stop'
$zipPath = (Resolve-Path -LiteralPath $Zip).Path
$testRoot = Join-Path $env:TEMP ('Studio clean PC ' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
Expand-Archive -LiteralPath $zipPath -DestinationPath $testRoot
if (Test-Path (Join-Path $testRoot 'Data')) { throw 'Release contains user data/configuration.' }
if (Test-Path (Join-Path $testRoot 'Themes')) { throw 'Release contains private themes.' }
# Same compiled source as the public GUI, with console output for assertions.
Copy-Item (Join-Path $PSScriptRoot 'JonsboStudio.Diagnostics.exe') $testRoot
Copy-Item (Join-Path $PSScriptRoot 'JonsboStudio.Diagnostics.exe.config') $testRoot
$oldVendor = $env:JONSBO_STUDIO_VENDOR_PATH
$oldPath = $env:PATH
try {
    $env:JONSBO_STUDIO_VENDOR_PATH = Join-Path $testRoot 'no-vendor-installation'
    $env:PATH = "$env:WINDIR\System32;$env:WINDIR"
    Push-Location $env:TEMP # Verify nothing depends on the current working directory.
    try {
        $diagnostic = Join-Path $testRoot 'JonsboStudio.Diagnostics.exe'
        foreach ($mode in @('--startup-test','--ui-test','--self-test')) {
            & $diagnostic $mode
            if ($LASTEXITCODE -ne 0) { throw "Portable test failed: $mode" }
        }
        $ffmpeg = Join-Path $testRoot 'Native\FFmpeg\ffmpeg.exe'
        $media = Join-Path $testRoot 'Data\Temp\portable-test.mp4'
        & $ffmpeg -hide_banner -loglevel error -f lavfi -i 'testsrc2=size=640x240:rate=60' -t 2 -c:v mpeg4 -q:v 4 -pix_fmt yuv420p -y $media
        if ($LASTEXITCODE -ne 0) { throw 'Bundled video preparation failed.' }
        $theme = Join-Path $testRoot 'Data\Temp\portable-test.json'
        @{Name='Portable playback';Width=640;Height=240;BackgroundVideoFile=$media;BackgroundImage='';BackgroundFit='fill';DisplayTexts=@();DisplayImages=@()} | ConvertTo-Json | Set-Content -LiteralPath $theme -Encoding UTF8
        & $diagnostic --ui-benchmark $theme
        if ($LASTEXITCODE -ne 0) { throw 'Portable UI playback failed.' }
        & $diagnostic --screenshot (Join-Path $testRoot 'clean-display.png') display
        if ($LASTEXITCODE -ne 0) { throw 'Portable screenshot failed.' }
        Write-Host "PASS: extracted package, no Data/config, unavailable vendor folder, restricted PATH, unrelated working directory, missing-runtime recovery and bundled 60 FPS video. Test folder: $testRoot"
    } finally { Pop-Location }
} finally { $env:JONSBO_STUDIO_VENDOR_PATH = $oldVendor; $env:PATH = $oldPath }
