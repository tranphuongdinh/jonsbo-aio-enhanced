# JONSBO Studio

A dark desktop controller and theme editor for compatible JONSBO USB displays. The connected DS916 (462 × 1920) and DS339 (376 × 960) have both been tested; the owner confirmed upright playback on each.

The app uses the official JONSBO icon, wordmark and backdrop from the installed software, with controls styled to match its black, charcoal and gold interface. JONSBO Studio remains an independent compatibility application.

## Run

“Start with Windows” registers an immediate user sign-in task with above-normal process priority and starts Studio in the tray. It replaces the older Run registry entry. Windows controls the exact order of startup applications, so being first is not guaranteed.

Extract the complete standalone ZIP, then double-click `JonsboStudio.exe`. First launch automatically checks Microsoft WebView2 and the connected DS339 USB driver, showing a setup progress window only when installation is needed. The signed Microsoft WebView2 offline installer is bundled in `Native/WebView2`; DS339 driver installation requests Windows administrator approval. If a newly connected DS339 needs its driver, reopen Studio. Current Windows 10/11 x64 with .NET Framework 4.8 is required.

Studio also checks actual browser creation: a stale runtime registration triggers one setup/retry. Startup errors show a recovery screen and save details to `%LOCALAPPDATA%/JonsboStudio/Logs/startup.log` instead of leaving an unexplained blank window. Keep the full ZIP contents together; the EXE alone is not the complete portable package.

On first launch, each connected display receives compatible default output settings. Choose a theme for each display from Online themes or import a complete local package; personal profiles and media are not included in the release. The app shows guidance when a theme is missing and reports playback errors visibly.

The vendor application does not need to be installed. Video preparation uses `Native/FFmpeg`; DS339 playback uses `Native/DS339` and the separately installed USB driver in `Drivers/DS339`. Launch `JonsboStudio.exe` directly. Keep all bundled folders together. Existing JONSBO themes are discovered only when that optional installation is present; otherwise import themes or download them from Online themes.

Exit the official JONSBO application before connecting, because the serial port requires exclusive access. Select a theme and choose Apply. Auto rotation includes the correction verified on the connected VMAXB panel. Manual rotation is also available.

Closing the window keeps playback running in the tray. Exit from the tray stops both displays. Each device keeps its own theme, resolution, frame rate, quality, rotation and resume state in `Data/Settings/devices.json`; running displays resume together on the next launch. Start with Windows is available in Settings. Device-side playback after all host processes exit has not been established for these JONSBO devices.

## Features

- Display control, output settings, small live preview, preparation progress and playback metrics.
- Editor background sizing: Fit keeps the whole image, Cover fills and crops, Stretch fills without preserving aspect ratio, and Select region crops a chosen area of the source. The saved choice also controls video preparation and image playback.
- DS916 and DS339 appear separately in the sidebar. Each can play a different theme at the same time. Select a device to edit its settings or theme; the other keeps playing. Stop affects the selected display. Selecting DS339 uses 960 × 376 logical output; the DS916 uses 1920 × 462. Each transport checks the device response before sending frames.
- Native `.vmax`, `.JONSBO`, ZIP and JSON theme import; native encrypted ZIP export. Unknown native fields and source assets are retained where possible.
- Local library with thumbnails, search, edit, apply and recoverable removal. Installed themes are read-only; editor changes are saved as private copies.
- Editor with background media, text and image layers, position/size/font/color controls, live bindings, duplicate/remove, undo/redo and keyboard nudging. Native `Setting.txt` PNG layers and bundled fonts are rendered for compatible Type 1 themes. Ctrl+S saves.
- Online catalog with 29 public built-in packages and bundled full-artwork previews. Public packages download and apply directly. Community Download & apply opens the official store inside Studio: sign in there, approve any store-required points, then the completed ZIP/VMAX/JONSBO download is imported and applied to the selected display automatically. The store Import button uses the same authenticated download route; vendor software is not launched.
- Live CPU load, package temperature, P-core clock and package power bindings; memory, time and GPU readings. Bound text has a unit dropdown with Celsius/Fahrenheit, percent, MHz/GHz, watts and GB as appropriate. Fahrenheit and GHz values are converted, and the editor shows the current sensor reading.
- A matching icon is embedded in the executable, window and system tray. Regenerate its PNG and multi-resolution ICO with `Assets/Icon/Generate-Icon.ps1`.

## Performance

Measured on an Intel i5-14600KF (20 logical processors) with the user's saved DS916 video and DS339 GPU_CUSTOM theme, both outputs delivered approximately 60 fresh frames and 60 USB submissions per second. In 15-second full-app samples, CPU use was 0.72–0.98% with the window open and a 59.6–59.9 FPS preview, 0.42–0.62% in the tray, and 0.62–0.69% in the tray using efficiency cores. These are warm-cache measurements on this PC, not a universal CPU limit. More complex themes, initial preparation and other system load can exceed 2%.

DS339 live frames now remain in native BGRA memory through composition and USB submission, avoiding a full-frame JPEG encode/decode round trip and an extra GDI color conversion. DS916 JPEG output also accepts BGRA directly. Sensor overlays use cached premultiplied pixels and draw only their occupied bounds. Video-only themes do not start unused sensor samplers. Background frame selection and output share one paced worker per display. High-resolution waitable timers preserve deadlines while the app is hidden; they avoid the coarser timing Windows 11 may apply to occluded windows.

The app preview is limited to 320 × 180 while preserving aspect ratio, orientation and the output FPS target. Native display resolution is unchanged. A direct pixel sampler handles DS339 preview rotation/scaling without the expensive full-size GDI transformation. Preview work sleeps while the window is hidden or another page is selected.

**Settings -> Use efficiency cores** assigns Studio threads to the lower-efficiency-class CPU sets reported by Windows. It detects CPU topology at runtime, including this PC's eight E-cores, and falls back to Windows scheduling on homogeneous or unsupported processors. It is optional and off by default: E-cores can reduce competition with foreground applications, but can increase measured CPU time or slow complex themes. It does not hide FFmpeg or reduce the amount of rendering work by itself.

Video is prepared once at source frame rate into a persistent JPEG cache. FFmpeg uses CPU during that first conversion, then exits. Frames are reused across launches; RAM is bounded by the selected cache limit and remaining frames stay on disk. A 60 FPS target repeats lower-frame-rate sources rather than inventing motion. USB submissions measure host delivery, not an optical measurement of panel refresh.


## Build and verify

```powershell
.\build.ps1
.\JonsboStudio.Diagnostics.exe --self-test
.\JonsboStudio.Diagnostics.exe --ui-test
.\Package-Release.ps1 -OutputZip work\new-release.zip
.\Test-PortableRelease.ps1 -Zip work\new-release.zip
```

The portable test extracts into a new path containing spaces with no Data/settings or private themes, disables vendor-folder discovery, restricts PATH to Windows, and launches from an unrelated working directory. UI startup is also tested against a genuinely absent Windows scheduled task with no legacy-startup fallback; this does not delete or modify the existing Studio startup task. Startup-status read errors are non-fatal, and disabling an absent task is harmless. The tests also check actual missing-browser failure/recovery (using an invalid browser path followed by the installed runtime), editor/UI startup, native theme roundtrip and generated 60 FPS video through the bundled FFmpeg. They do not uninstall this PC's runtime, simulate a new USB driver installation, or replace a clean-Windows VM test.

Close the running app before replacing its executable. Alternatively build with `-OutputName JonsboStudio.Update.exe`, gracefully stop the running instance with `JonsboStudio.exe --exit`, and replace the executable and matching config after it exits.

Before publishing, upload the candidate ZIP to an unpublished draft release and run `.github/workflows/fresh-install.yml` with its asset ID and SHA256. This provisions a new Windows VM, verifies the exact ZIP and runs the shipped EXE plus the portable test suite with no Studio task, Run entry or vendor installation. Logs and screenshots are preserved as Actions artifacts. Publish only through `Publish-Release.ps1 -Zip CANDIDATE.zip -CleanWindowsRunId RUN_ID`; it refuses a missing/failed run, a different source commit or ZIP hash, or missing test evidence. A fresh directory on the development PC is not a clean-machine test. The hosted Windows Server image does not establish Windows 10/11 USB-driver or physical-display compatibility.

Diagnostics also support `--list-devices`, `--query-device COM12`, `--benchmark THEME.json [COM12]` and `--ui-benchmark THEME.json [COM12]`. Device tests take control of the display and require an unused port.

## Data and limitations

Settings, imported themes, downloads, prepared cache and recoverable removals live under `Data`. Back up that folder before moving or deleting this installation. Public theme packages remain their creators' work.

Native theme counters `MemoryUseInt`, `MemoryUsageSize`, `MemorySize`, `MemoryCanUsageSize`, `MemoryCanUsage`, `UpNetSpeed` and `DownNetSpeed` are supported directly. RAM counters use Windows physical-memory statistics; network speeds use changes in active gateway-connected interface byte counters. These work without administrator access. RAM values are in usable GiB (shown as GB for compatibility), network base values in KiB/s. The editor offers native-compatible GB/MB, KB/s/MB/s/Mbps, V/mV and temperature units.

For CPU temperature, power, clock and voltage, choose **Settings → Enable hardware sensors**. Windows asks for administrator approval, Studio closes and resumes with elevated access, and the bundled signed PawnIO installer runs if needed. The hardware-access preference is remembered; an existing Windows startup task is updated to retain hardware access. An administrator can be required because CPU sensors use low-level processor access. No Windows security protections are disabled. Intel package/core and AMD Tdie/Tctl names are recognized; missing or unsupported temperatures remain a dash. Drive temperatures are sampled only for themes that request them, and may be unavailable on unsupported storage controllers. Network and RAM collection continue independently.

The official vendor app uses CPUID SDK and requests administrator privileges in its manifest. Studio uses the independent LibreHardwareMonitor/PawnIO stack rather than depending on that proprietary SDK or its installed files. The driver installer hash, signed publisher and source are recorded in `Native/dependencies.json`. Diagnostic `--sensor-test` checks native aliases and live counters. Clean Windows VM validation additionally tests the signed driver's installation; a VM cannot establish physical CPU-temperature accuracy.

Advanced vendor animation widgets are preserved in native packages but are not fully rendered or editable. CPU hardware readings use LibreHardwareMonitor when available; unsupported sensors display a dash. Community sign-in and points confirmation remain on the official store; Studio captures completed theme downloads and applies them. It does not bypass account or purchase requirements. COM5 and COM10 belong to Jungle Leopard Display, so they are excluded. Compatibility with every JONSBO firmware, native widget and package variant is not claimed.

See `REVERSE-ENGINEERING.md` for compatibility findings and validation, and `THIRD-PARTY-NOTICES.md` for dependencies.
