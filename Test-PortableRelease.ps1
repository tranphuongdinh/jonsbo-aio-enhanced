param([Parameter(Mandatory=$true)][string]$Zip,[switch]$RequireCleanMachine,[string]$ReportDirectory='',[switch]$TestSensorDriverSetup)
$ErrorActionPreference = 'Stop'
$zipPath = (Resolve-Path -LiteralPath $Zip).Path
if ($ReportDirectory) { $ReportDirectory = [IO.Path]::GetFullPath($ReportDirectory) }
$sourceCommit = if ($env:GITHUB_SHA) { $env:GITHUB_SHA } else { (& git -C $PSScriptRoot rev-parse HEAD).Trim() }
if ($RequireCleanMachine) {
    if ($env:OS -ne 'Windows_NT') { throw 'A clean Windows machine is required.' }
    if (Get-ScheduledTask -TaskName 'JONSBO Studio' -ErrorAction SilentlyContinue) { throw 'This machine already has the Studio startup task.' }
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\Microsoft\Windows\CurrentVersion\Run')
    try { $legacy = if ($key) { $key.GetValue('JonsboStudio') } else { $null } } finally { if ($key) { $key.Dispose() } }
    if ($null -ne $legacy) { throw 'This machine already has the Studio Run entry.' }
    if (Test-Path (Join-Path $env:LOCALAPPDATA 'JONSBO-AIO')) { throw 'The vendor app is already present.' }
    Write-Host 'CLEAN MACHINE: no vendor installation, startup task or legacy startup entry.'
}
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
        $shipped = Start-Process -FilePath (Join-Path $testRoot 'JonsboStudio.exe') -ArgumentList '--startup-test' -WorkingDirectory $env:TEMP -WindowStyle Hidden -Wait -PassThru
        if ($shipped.ExitCode -ne 0) { throw "The shipped GUI executable failed first-launch testing: $($shipped.ExitCode)" }
        Write-Host 'PASS: shipped JonsboStudio.exe first launch and absent startup task.'
        foreach ($mode in @('--startup-test','--ui-test','--self-test','--sensor-test')) {
            & $diagnostic $mode
            if ($LASTEXITCODE -ne 0) { throw "Portable test failed: $mode" }
        }
        Add-Type -TypeDefinition 'using System.Text;using System.Runtime.InteropServices;public static class StudioReleasePaths{[DllImport("kernel32.dll",CharSet=CharSet.Unicode)]public static extern uint GetShortPathName(string path,StringBuilder buffer,uint size);}'
        $buffer = New-Object Text.StringBuilder 4096
        $length = [StudioReleasePaths]::GetShortPathName($testRoot,$buffer,4096)
        if ($length -gt 0 -and $buffer.ToString() -ne $testRoot) {
            & (Join-Path $buffer.ToString() 'JonsboStudio.Diagnostics.exe') --ui-test
            if ($LASTEXITCODE -ne 0) { throw 'Theme saving/import failed when launched through a Windows short-path alias.' }
            Write-Host 'PASS: theme saving/import from a Windows 8.3 path alias.'
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
        & $diagnostic --screenshot (Join-Path $testRoot 'clean-online.png') online
        if ($LASTEXITCODE -ne 0) { throw 'Online gallery screenshot failed.' }
        & $diagnostic --screenshot (Join-Path $testRoot 'clean-settings.png') settings
        if ($LASTEXITCODE -ne 0) { throw 'Sensor settings screenshot failed.' }
        if ($TestSensorDriverSetup) {
            if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Sensor driver installation tests require the disposable CI VM.' }
            & $diagnostic --sensor-install-test
            if ($LASTEXITCODE -ne 0) { throw 'Sensor driver setup failed on the clean VM.' }
            & $diagnostic --sensor-test
            if ($LASTEXITCODE -ne 0) { throw 'Sensor sampling failed after driver installation.' }
        }
        if ($ReportDirectory) {
            New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null
            Copy-Item (Join-Path $testRoot 'clean-*.png') $ReportDirectory
            @{passed=$true;cleanMachineRequired=[bool]$RequireCleanMachine;sha256=(Get-FileHash $zipPath).Hash;os=(Get-CimInstance Win32_OperatingSystem).Caption;machine=$env:COMPUTERNAME;testRoot=$testRoot;sourceCommit=$sourceCommit} | ConvertTo-Json | Set-Content (Join-Path $ReportDirectory 'result.json')
        }
        Write-Host "PASS: extracted package, no Data/config, unavailable vendor folder, restricted PATH, unrelated working directory, absent Windows startup task, missing-runtime recovery and bundled 60 FPS video. Test folder: $testRoot"
    } finally { Pop-Location }
} finally { $env:JONSBO_STUDIO_VENDOR_PATH = $oldVendor; $env:PATH = $oldPath }
