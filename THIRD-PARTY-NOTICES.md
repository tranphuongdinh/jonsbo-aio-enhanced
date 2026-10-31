# Third-party notices

JONSBO Studio is an independent compatibility application, not an official JONSBO product. JONSBO names and public theme content belong to their respective owners.

Runtime dependencies and their notices:

- Newtonsoft.Json: MIT; `Licenses/Newtonsoft.Json-LICENSE.md`.
- SharpZipLib: MIT; `Licenses/SharpZipLib-LICENSE.txt`.
- libjpeg-turbo / TurboJPEG: applicable BSD-style, IJG and other notices; `Licenses/libjpeg-turbo-LICENSE.md`.
- Microsoft WebView2 SDK: Microsoft license and third-party notices; `Licenses/WebView2-LICENSE.txt` and `Licenses/WebView2-NOTICE.txt`.
- Inter font: SIL Open Font License 1.1; `Licenses/Inter-OFL.txt` and `Assets/Fonts/OFL.txt`.
- LibreHardwareMonitorLib 0.9.6: MPL 2.0; `Licenses/LibreHardwareMonitor-MPL-2.0.txt`. Source: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor.
- Microsoft System.Memory, System.Numerics.Vectors and System.Runtime.CompilerServices.Unsafe runtime libraries: MIT; source packages are available on NuGet. These support the hardware sensor library.

Microsoft Edge WebView2 Runtime and .NET Framework are separately installed Microsoft components. FFmpeg is used as an external executable from the user's existing installation or locally supplied copy; its license obligations depend on that executable's build. No FFmpeg executable is included as a newly built Studio component.

DS339 output calls the `MSDISPLAYSDKWRRAPER.dll` SDK in the user's installed JONSBO-AIO directory. The SDK and driver remain part of that installation and are not included in this project's source or runtime folder.

Theme thumbnails and packages referenced in `Assets/public-themes.json` come from the public vendor theme distribution. Availability through a public URL does not transfer ownership or grant unrestricted redistribution rights. Downloaded packages are cached locally under `Data/Downloads`; they, vendor thumbnails under `Assets/ThemeThumbs`, and proprietary reverse-engineering artifacts are excluded from source control. The online gallery shows resolution placeholders until thumbnails are locally available. Consult the creators' terms before redistributing theme content.
