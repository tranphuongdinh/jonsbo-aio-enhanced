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

Measured on the user's 20-logical-processor machine with the DS916 native 916H06 theme at 1920 × 462 and 60 FPS: the cached, single-display diagnostic used 2.14% CPU while submitting 59.99 USB frames and 59.99 fresh frames per second. With the window open and its live preview at 59.6 FPS, Studio used 4.20% CPU. With both DS916 and DS339 playing from cache and the window hidden, Studio used 2.80–4.27% CPU across short samples. Other PC load can change the results; the requested below-2% dual-display target has not been reached.

Video is prepared once at source frame rate into a persistent JPEG cache. FFmpeg uses CPU during that first conversion, then exits. For the 227-second DS916 clip, the faster YUV 4:2:0 overlay and lighter prepared JPEG setting reduced the completed cache from 1.16 GB to 589 MB and the observed preparation time from about 76 to 64 seconds. A later hidden-app measurement found no FFmpeg process. Cache files are limited to 8 GB each, with an 8 GB total prune target at startup. The renderer reuses prepared native-orientation frames, limits RAM, spills to disk and paces output against fixed deadlines. Live sensor overlays still require decoding and encoding each displayed frame. Preview JPEG decoding is scaled down before rendering, and preview work stops when the window is hidden. A 60 FPS output target repeats frames for lower-frame-rate sources; it does not invent motion through interpolation. USB submissions measure host delivery, not an optical measurement of panel refresh.

## Build and verify

```powershell
.\build.ps1
.\JonsboStudio.Diagnostics.exe --self-test
.\JonsboStudio.Diagnostics.exe --ui-test
.\Package-Release.ps1 -OutputZip work\new-release.zip
.\Test-PortableRelease.ps1 -Zip work\new-release.zip
```

The portable test extracts into a new path containing spaces with no Data/settings or private themes, disables vendor-folder discovery, restricts PATH to Windows, and launches from an unrelated working directory. It checks actual missing-browser failure/recovery (using an invalid browser path followed by the installed runtime), editor/UI startup, native theme roundtrip and generated 60 FPS video through the bundled FFmpeg. It does not uninstall this PC's runtime, simulate a new USB driver installation, or replace a clean-Windows VM test.

Close the running app before replacing its executable. Alternatively build with `-OutputName JonsboStudio.Update.exe`, gracefully stop the running instance with `JonsboStudio.exe --exit`, and replace the executable and matching config after it exits.

Diagnostics also support `--list-devices`, `--query-device COM12`, `--benchmark THEME.json [COM12]` and `--ui-benchmark THEME.json [COM12]`. Device tests take control of the display and require an unused port.

## Data and limitations

Settings, imported themes, downloads, prepared cache and recoverable removals live under `Data`. Back up that folder before moving or deleting this installation. Public theme packages remain their creators' work.

Advanced vendor animation widgets are preserved in native packages but are not fully rendered or editable. CPU hardware readings use LibreHardwareMonitor when available; unsupported sensors display a dash. Community sign-in and points confirmation remain on the official store; Studio captures completed theme downloads and applies them. It does not bypass account or purchase requirements. COM5 and COM10 belong to Jungle Leopard Display, so they are excluded. Compatibility with every JONSBO firmware, native widget and package variant is not claimed.

See `REVERSE-ENGINEERING.md` for compatibility findings and validation, and `THIRD-PARTY-NOTICES.md` for dependencies.
