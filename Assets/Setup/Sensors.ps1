$ErrorActionPreference = 'Stop'
try {
    $root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
    $manifest = Get-Content (Join-Path $root 'Native\dependencies.json') -Raw | ConvertFrom-Json
    $installer = Join-Path $root 'Native\Sensors\PawnIO_setup.exe'
    if ((Get-FileHash -LiteralPath $installer).Hash -ne $manifest.pawnio.sha256) { throw 'The bundled sensor installer failed its hash check.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $installer
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'CN=namazso.eu') { throw 'The sensor installer signature is invalid.' }
    # Official signed edition, using the same installation command as LibreHardwareMonitor.
    $process = Start-Process -FilePath $installer -ArgumentList '-install' -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -notin 0,3010) { throw "PawnIO setup returned $($process.ExitCode)." }
    exit $process.ExitCode
} catch { Write-Error $_; exit 1 }
