# Third-party notices

JONSBO Studio is an independent compatibility application, not an official JONSBO product. JONSBO names and public theme content belong to their respective owners.

The app icon, wordmark and backdrop in `Assets/Icon` and `Assets/Brand` are adapted from the installed JONSBO-AIO software's `Resource/qsb` assets (`qsbLogo.png`, `LeftTopLogo.png` and its background TIFF). The `.ico` is built at Windows icon sizes from that official PNG. These visual assets and JONSBO trademarks remain JONSBO's property; their inclusion does not imply endorsement or a trademark license.

Runtime dependencies and their notices:

- Newtonsoft.Json: MIT; `Licenses/Newtonsoft.Json-LICENSE.md`.
- SharpZipLib: MIT; `Licenses/SharpZipLib-LICENSE.txt`.
- libjpeg-turbo / TurboJPEG: applicable BSD-style, IJG and other notices; `Licenses/libjpeg-turbo-LICENSE.md`.
- Microsoft WebView2 SDK: Microsoft license and third-party notices; `Licenses/WebView2-LICENSE.txt` and `Licenses/WebView2-NOTICE.txt`.
- Geist font: SIL Open Font License 1.1; `Licenses/Geist-OFL.txt`, https://github.com/vercel/geist-font.
- Inter fallback font: SIL Open Font License 1.1; `Licenses/Inter-OFL.txt` and `Assets/Fonts/OFL.txt`.
- LibreHardwareMonitorLib 0.9.6: MPL 2.0; `Licenses/LibreHardwareMonitor-MPL-2.0.txt`. Source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor.
- Microsoft System.Memory, System.Numerics.Vectors and System.Runtime.CompilerServices.Unsafe runtime libraries: MIT; source packages are available on NuGet. These support the hardware sensor library.

Microsoft Edge WebView2 Runtime and .NET Framework are Microsoft components. The signed Microsoft Evergreen WebView2 x64 offline installer is bundled under Native/WebView2 and is run only when needed. First-run setup verifies its Microsoft Authenticode signature before running it. Source and pinned hash are recorded in Native/dependencies.json; Microsoft runtime terms apply. A Microsoft-hosted bootstrapper is used only as a fallback when the bundled installer is absent.

FFmpeg is bundled as a separate LGPL v3 shared executable with replaceable DLLs. See `Licenses/FFmpeg-LICENSE.txt`, `Licenses/GPL-3.0.txt` for the incorporated GPL v3 text, and `Native/dependencies.json` for binary provenance, matching upstream source revision and build scripts. No FFmpeg source modifications were made. FFmpeg source: https://github.com/FFmpeg/FFmpeg/tree/8dca1586f4. Build scripts and dependency sources: https://github.com/BtbN/FFmpeg-Builds.

DS339 uses the bundled MacroSilicon `MSDISPLAYSDKWRRAPER.dll` obtained from the official JONSBO distribution; this vendor binary remains its owner's work. The signed MSUSBDisplay driver package and libusb0 runtime originate from that distribution. libusb-win32 runtime is LGPL and its kernel driver is GPL; licenses are included in `Licenses/libusb-win32-LGPL.txt` and `Licenses/libusb-win32-GPL.txt`. Upstream source: https://github.com/mcuee/libusb-win32. The INF is included unchanged to preserve the signed catalog. The official application itself is not required.

Theme thumbnails and packages referenced in `Assets/public-themes.json` come from the public vendor theme distribution. Availability through a public URL does not transfer ownership or grant unrestricted redistribution rights. Downloaded packages are cached locally under `Data/Downloads`; downloaded packages and proprietary reverse-engineering artifacts are excluded from source control. The 29 official built-in artwork previews under `Assets/ThemeThumbs` are bundled with the gallery and remain their creators' work. Community downloads use the official website's sign-in and acquisition flow. Consult the creators' terms before redistributing theme content.
