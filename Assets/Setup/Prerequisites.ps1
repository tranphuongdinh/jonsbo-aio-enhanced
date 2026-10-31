param([switch]$InstallDriver, [switch]$InstallWebView)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
if ($InstallDriver) {
    & "$env:WINDIR\System32\pnputil.exe" /add-driver (Join-Path $root 'Drivers\DS339\MSUSBDisplay.inf') /install
    if ($LASTEXITCODE -notin 0, 3010) { throw "USB driver installation returned $LASTEXITCODE" }
    exit 0
}
try {
    Add-Type -AssemblyName System.Windows.Forms
    $runtime = @(
        "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application",
        "$env:ProgramFiles\Microsoft\EdgeWebView\Application",
        "$env:LOCALAPPDATA\Microsoft\EdgeWebView\Application"
    ) | Where-Object { Test-Path $_ }
    if ($InstallWebView -or -not $runtime) {
        Write-Host 'Installing Microsoft WebView2 Runtime...'
        $installer = Join-Path $env:TEMP 'JonsboStudio-WebView2Setup.exe'
        Invoke-WebRequest 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile $installer -UseBasicParsing
        $signature = Get-AuthenticodeSignature -LiteralPath $installer
        if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw 'WebView2 installer signature could not be verified.' }
        $process = Start-Process -FilePath $installer -ArgumentList '/silent','/install' -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "WebView2 installation returned $($process.ExitCode)" }
    }
    $device = Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue | Where-Object { $_.InstanceId -like 'USB\VID_345F&PID_9132&MI_03*' }
    if ($device -and ($device.Class -ne 'MSDisplay' -or $device.Status -ne 'OK')) {
        Write-Host 'Installing the DS339 USB driver (Windows will ask for administrator approval)...'
        $process = Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -InstallDriver" -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw 'DS339 driver setup did not complete.' }
    }

} catch {
    Write-Error $_
    exit 1
}
