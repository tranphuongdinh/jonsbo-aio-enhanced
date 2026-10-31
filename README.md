# JONSBO Studio

A dark desktop controller and theme editor for compatible JONSBO USB displays. The connected DS916 (462 × 1920) and DS339 (376 × 960) have both been tested; the owner confirmed upright playback on each.

## Run

Open `JonsboStudio.exe` from this folder. Keep its DLLs and Assets folder together; the `Runtime` folder is used when rebuilding from source. Windows x64, .NET Framework 4.8 and Microsoft Edge WebView2 Runtime are required. Video preparation currently uses FFmpeg from the installed JONSBO-AIO installation (or a locally supplied executable). DS339 playback uses the MacroSilicon display driver and SDK DLL from that installed application. This is not yet a standalone installer.

For a release ZIP, extract all files to one folder before launching `JonsboStudio.exe`. The ZIP includes the runtime DLLs and UI assets but does not bundle FFmpeg, the JONSBO-AIO SDK, vendor theme media, or personal settings.

Exit the official JONSBO-AIO application before connecting, because the serial port requires exclusive access. Select a theme and choose Apply. Auto rotation includes the correction verified on the connected VMAXB panel. Manual rotation is also available.

Closing the window keeps playback running in the tray. Exit from the tray stops both displays. Each device keeps its own theme, resolution, frame rate, quality, rotation and resume state in `Data/Settings/devices.json`; running displays resume together on the next launch. Start with Windows is available in Settings. Device-side playback after all host processes exit has not been established for these JONSBO devices.

## Features

- Display control, output settings, small live preview, preparation progress and playback metrics.
- Editor background sizing: Fit keeps the whole image, Cover fills and crops, Stretch fills without preserving aspect ratio, and Select region crops a chosen area of the source. The saved choice also controls video preparation and image playback.
- DS916 and DS339 appear separately in the sidebar. Each can play a different theme at the same time. Select a device to edit its settings or theme; the other keeps playing. Stop affects the selected display. Selecting DS339 uses 960 × 376 logical output; the DS916 uses 1920 × 462. Each transport checks the device response before sending frames.
- Native `.vmax`, `.JONSBO`, ZIP and JSON theme import; native encrypted ZIP export. Unknown native fields and source assets are retained where possible.
- Local library with thumbnails, search, edit, apply and recoverable removal. Installed themes are read-only; editor changes are saved as private copies.
- Editor with background media, text and image layers, position/size/font/color controls, live bindings, duplicate/remove, undo/redo and keyboard nudging. Native `Setting.txt` PNG layers and bundled fonts are rendered for compatible Type 1 themes. Ctrl+S saves.
- Online catalog with 29 verified public built-in packages, thumbnails, download and apply. The official community store is linked for items requiring its own account or download flow: https://theme.screen-aio.com/theme/
- Live CPU load, package temperature, P-core clock and package power bindings; memory, time and GPU readings. Bound text has a unit dropdown with Celsius/Fahrenheit, percent, MHz/GHz, watts and GB as appropriate. Fahrenheit and GHz values are converted, and the editor shows the current sensor reading.
- A matching icon is embedded in the executable, window and system tray. Regenerate its PNG and multi-resolution ICO with `Assets/Icon/Generate-Icon.ps1`.

## Performance

Measured on the user's machine, across short samples; CPU is normalized across all 20 logical processors.

| Measurement | Installed app | JONSBO Studio |
| --- | ---: | ---: |
| Total CPU, visible app | 6.87% | 2.13% including WebView processes |
| Private memory | 776.3 MB | 411.9 MB including WebView processes |
| USB frame submissions | Not instrumented | About 59.7–60.0 FPS |
| App preview | Not instrumented | About 58–60 FPS |

The minimal playback host alone used about 0.06% CPU for cached video and 0.86% for the tested sensor theme. These are different workloads from the visible UI comparison. Aggregate working sets can count shared pages multiple times, so private memory is the more useful comparison here.

Video is prepared once at source frame rate into a persistent JPEG cache. The renderer reuses prepared native-orientation frames, limits RAM, spills to disk and paces output against fixed deadlines. Live overlays compose on a separate worker. Preview resolution is reduced and preview work stops when hidden. A 60 FPS output target repeats frames for lower-frame-rate sources; it does not invent motion through interpolation. USB submissions measure host delivery, not an optical measurement of panel refresh.

## Build and verify

```powershell
.\build.ps1
.\JonsboStudio.Diagnostics.exe --self-test
.\JonsboStudio.Diagnostics.exe --ui-test
```

Close the running app before replacing its executable. Alternatively build with `-OutputName JonsboStudio.Update.exe`, gracefully stop the running instance with `JonsboStudio.exe --exit`, and replace the executable and matching config after it exits.

Diagnostics also support `--list-devices`, `--query-device COM12`, `--benchmark THEME.json [COM12]` and `--ui-benchmark THEME.json [COM12]`. Device tests take control of the display and require an unused port.

## Data and limitations

Settings, imported themes, downloads, prepared cache and recoverable removals live under `Data`. Back up that folder before moving or deleting this installation. Public theme packages remain their creators' work.

Advanced vendor animation widgets are preserved in native packages but are not fully rendered or editable. CPU hardware readings use LibreHardwareMonitor when available; unsupported sensors display a dash. Community items requiring authentication or points are opened through the official store rather than downloaded automatically. COM5 and COM10 belong to Jungle Leopard Display, so they are excluded. Compatibility with every JONSBO firmware, native widget and package variant is not claimed.

See `REVERSE-ENGINEERING.md` for compatibility findings and validation, and `THIRD-PARTY-NOTICES.md` for dependencies.
