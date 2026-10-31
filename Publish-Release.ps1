param([Parameter(Mandatory=$true)][string]$Zip,[Parameter(Mandatory=$true)][long]$CleanWindowsRunId)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $zipPath = (Resolve-Path -LiteralPath $Zip).Path
    $hash = (Get-FileHash $zipPath).Hash
    $commit = (& git rev-parse HEAD).Trim()
    & git diff --quiet HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Commit the candidate source and release scripts before publishing.' }
    $fields = @{}
    $lines = @('protocol=https','host=github.com','') | git -c credential.interactive=never credential fill
    foreach ($line in $lines) { $parts=$line -split '=',2; if ($parts.Count -eq 2) { $fields[$parts[0]]=$parts[1] } }
    $headers = @{Authorization='Bearer '+$fields['password'];Accept='application/vnd.github+json';'X-GitHub-Api-Version'='2022-11-28';'User-Agent'='JonsboStudio-Release'}
    $base = 'https://api.github.com/repos/tranphuongdinh/jonsbo-aio-enhanced'
    $run = Invoke-RestMethod "$base/actions/runs/$CleanWindowsRunId" -Headers $headers
    if ($run.conclusion -ne 'success' -or $run.status -ne 'completed' -or $run.head_sha -ne $commit -or $run.path -ne '.github/workflows/fresh-install.yml' -or $run.display_title -ne "Fresh install / $hash") {
        throw 'PUBLISH BLOCKED: a successful clean Windows VM run matching this exact ZIP hash and source commit is required.'
    }
    $jobs = Invoke-RestMethod "$base/actions/runs/$CleanWindowsRunId/jobs" -Headers $headers
    if (-not ($jobs.jobs | Where-Object { $_.conclusion -eq 'success' -and ($_.steps | Where-Object { $_.name -eq 'Test fresh installation on this new Windows VM' -and $_.conclusion -eq 'success' }) })) { throw 'PUBLISH BLOCKED: clean-install test step did not pass.' }
    $artifacts = Invoke-RestMethod "$base/actions/runs/$CleanWindowsRunId/artifacts" -Headers $headers
    if (-not ($artifacts.artifacts | Where-Object { $_.name -eq 'fresh-windows-install-evidence' -and -not $_.expired })) { throw 'PUBLISH BLOCKED: clean-install evidence is missing.' }
    Write-Host "Gate passed: $($run.html_url)"
    $release = Invoke-RestMethod "$base/releases/tags/v1.0.0" -Headers $headers
    $name = 'JonsboStudio-v1.0.0-win-x64.zip'
    $old = $release.assets | Where-Object name -eq $name
    if (-not $old) { throw 'Existing v1.0.0 portable asset not found.' }
    $temp = 'JonsboStudio-v1.0.0-validated-'+$hash.Substring(0,12)+'.zip'
    $new = Invoke-RestMethod "https://uploads.github.com/repos/tranphuongdinh/jonsbo-aio-enhanced/releases/$($release.id)/assets?name=$temp" -Headers $headers -Method Post -ContentType application/zip -InFile $zipPath
    if ($new.size -ne (Get-Item $zipPath).Length) { throw 'Uploaded package size mismatch.' }
    $oldTag = ((& git ls-remote origin refs/tags/v1.0.0) -split "`t")[0]
    & git tag -fa v1.0.0 -m 'JONSBO Studio 1.0.0 - validated fresh Windows installation'
    if ($LASTEXITCODE -ne 0) { throw 'Could not update local release tag.' }
    & git -c credential.interactive=never push "--force-with-lease=refs/tags/v1.0.0:$oldTag" origin v1.0.0
    if ($LASTEXITCODE -ne 0) { throw 'Could not update remote release tag.' }
    $null = Invoke-RestMethod "$base/releases/assets/$($old.id)" -Headers $headers -Method Patch -ContentType application/json -Body (@{name='JonsboStudio-v1.0.0-previous-'+$old.id+'.zip'}|ConvertTo-Json)
    try { $new = Invoke-RestMethod "$base/releases/assets/$($new.id)" -Headers $headers -Method Patch -ContentType application/json -Body (@{name=$name}|ConvertTo-Json) }
    catch { $null = Invoke-RestMethod "$base/releases/assets/$($old.id)" -Headers $headers -Method Patch -ContentType application/json -Body (@{name=$name}|ConvertTo-Json); throw }
    $body = @"
JONSBO Studio 1.0.0 for Windows 10/11 x64.

Download **JonsboStudio-v1.0.0-win-x64.zip**, extract ALL files into a writable folder, then run **JonsboStudio.exe**. No CMD launcher or official vendor application is required. Requires .NET Framework 4.8; the signed Microsoft WebView2 offline installer and DS339 USB driver are bundled.

This refresh fixes first-launch failure in StartupRegistration.IsEnabled (FileNotFoundException / 0x80070002) when a new computer has no Studio scheduled task. Startup status is optional and non-fatal, absent startup tasks report disabled, and disabling startup when no task exists succeeds. Earlier fresh-folder tests had inherited the development PC's existing startup task; this release now requires a test on a newly provisioned Windows VM.

All 29 built-in theme previews are bundled. Community Sign in & apply opens the official store inside Studio; completed theme downloads import and apply automatically. Store account/points requirements remain in effect. DS916 and DS339 keep separate themes and settings; private media/profiles are not included.

Clean Windows VM evidence: $($run.html_url)

The exact ZIP was tested on a newly provisioned GitHub-hosted Windows Server VM with no Studio startup task, legacy Run entry, vendor installation or personal profiles. Tests cover the shipped EXE, first launch, missing-task handling, prerequisites, editor/theme roundtrip, all gallery previews, controlled store download/import and generated video playback. Hosted VM tests do not prove Windows 10/11 USB driver installation, physical screen playback or live signed-in community acquisition.

ZIP SHA256: $hash

Source commit: $commit
"@
    $null = Invoke-RestMethod "$base/releases/$($release.id)" -Headers $headers -Method Patch -ContentType application/json -Body (@{body=$body;target_commitish=$commit}|ConvertTo-Json)
    $verified = Join-Path $PSScriptRoot ('work\verified-public-'+$hash.Substring(0,12)+'.zip')
    Invoke-WebRequest $new.browser_download_url -OutFile $verified -UseBasicParsing
    if ((Get-FileHash $verified).Hash -ne $hash) { throw 'Public download hash mismatch; previous asset retained for recovery.' }
    $null = Invoke-RestMethod "$base/releases/assets/$($old.id)" -Headers $headers -Method Delete
    Write-Output (@{release=$release.html_url;run=$run.html_url;sha256=$hash;assetId=$new.id}|ConvertTo-Json -Compress)
} finally { Pop-Location }
