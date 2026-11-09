# Third-party notices

Android Sync Control is licensed under the [MIT License](LICENSE). The binaries it redistributes
contain the third-party components listed below, each under its own license. Full license texts
referenced here are in the [`licenses/`](licenses) folder, which is shipped next to the
application in the release archive.

## FFmpeg

- Project: https://ffmpeg.org
- License: GNU Lesser General Public License v2.1 or later (LGPL-2.1-or-later) —
  [`licenses/LGPL-2.1.txt`](licenses/LGPL-2.1.txt)
- Copyright (c) the FFmpeg developers; their copyright notices are retained.
- Shipped as: `avcodec-*.dll`, `avdevice-*.dll`, `avfilter-*.dll`, `avformat-*.dll`,
  `avutil-*.dll`, `swresample-*.dll`, `swscale-*.dll`, next to `AndroidSyncControl.exe`.
- Build scripts and sources: https://github.com/tqk2811/FFmpegBuild

The DLLs come unmodified from the NuGet packages `TqkLibrary.FFmpeg.Lgpl2.Native.Win.x64` / `.x86`.
Those builds are configured **without** `--enable-gpl` and contain neither x264 nor x265.
Android Sync Control links them dynamically only: they are separate files, so — as permitted by
LGPL-2.1 section 6 — you may replace them with your own build of the same FFmpeg major versions
and keep using the application.

Those FFmpeg builds statically link further third-party libraries of their own (libass, opus,
vorbis, dav1d, libvpx and others). Their configuration and notices live in the
[FFmpegBuild](https://github.com/tqk2811/FFmpegBuild) repository; this file does not repeat them.

## scrcpy server (`scrcpy-server.jar`)

- Project: https://github.com/Genymobile/scrcpy
- License: Apache License 2.0 — [`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt)
- Copyright (c) Genymobile (Romain Vimont) and contributors.
- Shipped as: `scrcpy-server.jar`, pushed to the Android device and executed there as a separate
  program; it is not linked into the application.

## Android Debug Bridge (adb)

- Project: https://android.googlesource.com/platform/packages/modules/adb
- License: Apache License 2.0 — [`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt)
- Copyright (c) The Android Open Source Project.
- Shipped as: `adb.exe`, `AdbWinApi.dll`, `AdbWinUsbApi.dll` (from the NuGet package
  `TqkLibrary.Adb.Runtimes`), launched as a separate process.

## SpicyTaco.AutoGrid

- Project: https://github.com/kmcginnes/SpicyTaco.AutoGrid
- License: MIT
- Copyright (c) 2017 Kris McGinnes
- Shipped as: `SpicyTaco.AutoGrid.dll`

## Newtonsoft.Json

- Project: https://github.com/JamesNK/Newtonsoft.Json
- License: MIT
- Copyright (c) 2007 James Newton-King
- Shipped as: `Newtonsoft.Json.dll`

## System.Drawing.Common

- Project: https://github.com/dotnet/runtime
- License: MIT
- Copyright (c) .NET Foundation and Contributors
- Shipped as: `System.Drawing.Common.dll`

## .NET Runtime, ASP.NET Core and Windows Desktop Runtime

- Project: https://github.com/dotnet
- License: MIT
- Copyright (c) .NET Foundation and Contributors
- Shipped as: the .NET runtime files next to `AndroidSyncControl.exe` (self-contained publish).

## ModelContextProtocol C# SDK

- Project: https://github.com/modelcontextprotocol/csharp-sdk
- License: Apache License 2.0 — [`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt)
- Copyright (c) the Model Context Protocol contributors.
- Shipped as: `ModelContextProtocol.dll`, `ModelContextProtocol.Core.dll`,
  `ModelContextProtocol.AspNetCore.dll`

## TqkLibrary.*

- Project: https://github.com/tqk2811
- License: MIT
- Copyright (c) 2022-2026 tqk2811
- Shipped as: `TqkLibrary.Scrcpy.dll`, `TqkLibrary.Scrcpy.Wpf.dll`, `TqkLibrary.ScrcpyNative.dll`,
  `TqkLibrary.AdbDotNet.dll`, `TqkLibrary.AudioPlayer.XAudio2.dll`,
  `TqkLibrary.AudioPlayer.XAudio2.Native.dll`, `TqkLibrary.Wpf.Interop.DirectX.dll`,
  `TqkLibrary.Wpf.Interop.DirectX.Native.dll`, `TqkLibrary.WpfUi.dll`

The MIT License text of these components is the same as [`LICENSE`](LICENSE), with the copyright
line of the respective holder.
