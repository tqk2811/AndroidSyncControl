# Android Sync Control

> Mirror and control **multiple Android devices at once** — right from your Windows PC.

**English** · [Tiếng Việt](README-vi.md)

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-0078D6)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Based on scrcpy](https://img.shields.io/badge/based%20on-scrcpy-3DDC84)
![License](https://img.shields.io/badge/license-MIT-blue)

Android Sync Control is a lightweight Windows desktop app that mirrors and controls many
Android devices simultaneously. Tick **Sync** on the main device and every touch and key
press is forwarded to all the other devices at the same time — do it once, run it everywhere.
Built on top of [scrcpy](https://github.com/Genymobile/scrcpy).

![Android Sync Control](docs/screenshot-dark.png)

## Features

- **Synchronized control** — Enable **Sync** on the main device and every tap and
  keystroke is mirrored to all connected devices at once.
- **Multi-device mirroring** — Display and control many phones side by side in a grid.
  Devices are auto-detected through ADB and can be added or removed on the fly.
- **Audio to PC speakers** — Hear the audio from a device straight through your PC speakers
  (XAudio2). Mute/unmute each device individually without dropping the connection.
- **GPU acceleration** — Hardware video decoding via Direct3D 11 (D3D11VA) for smooth,
  low-CPU mirroring. Tune the max FPS and max resolution to balance quality and load.
- **Auto-reconnect** — If a device reboots or drops, the app waits for it to come back and
  reconnects automatically — no manual steps.
- **Light / Dark theme** — Three modes: follow system, Light, or Dark. Keeps the device
  screen awake and shows touches.

## Screenshots

| Light | Dark |
|-------|------|
| ![Light theme](docs/screenshot-light.png) | ![Dark theme](docs/screenshot-dark.png) |

## Requirements

- Windows 10/11 (**x64**)
- An Android device with **USB debugging** enabled (Developer options)
- USB connection (or ADB over Wi‑Fi). ADB is bundled with the app.

## Download

Grab the latest build from the [**Releases**](https://github.com/tqk2811/AndroidSyncControl/releases)
page (`AndroidSyncControl-<version>-net8.0-windows.zip`). No installation required — unzip and run
`AndroidSyncControl.exe`.

## Usage

1. On the phone, enable **Developer options → USB debugging**, then connect it via USB and
   accept the debugging prompt.
2. Launch `AndroidSyncControl.exe`. Connected devices appear in the **Device** dropdown.
3. Pick a device and choose how to show it:
   - **Show Main** — put the selected device in the large main panel (fully controllable).
   - **Show ListView** — add the selected device to the grid on the right.
   - **Show All in ListView** — add every detected device to the grid at once.
4. Tick **Sync** on the main device to control all grid devices simultaneously.
5. Adjust the toolbar to taste.

### Toolbar reference

| Control | What it does |
|---------|--------------|
| **Size** | Scale of the device previews (1–150%). |
| **MaxFps** | Caps the video frame rate (1–120). Applied on the next connection. |
| **MaxSize** | Caps the longest video edge in pixels (multiple of 8). Below 360 = native size. Applied on reconnect. |
| **GPU** | On: hardware decode via D3D11VA. Off: CPU decode. Applied on reconnect. |
| **Audio** | On: receive & decode the device audio stream. Applied on reconnect. |
| **Sync** | Forward the main device's input to all other devices (main panel only). |
| **Speaker** | Play that device's audio through the PC speakers (per device). |
| **Disconnect** | Disconnect that device. |

## Build from source

Requirements: **.NET 8 SDK**, Windows, Visual Studio 2022 (optional).

```bash
git clone https://github.com/tqk2811/AndroidSyncControl.git
cd AndroidSyncControl
dotnet build AndroidSyncControl.sln -c Release
```

The project targets `net8.0-windows`, **x64 only**.

## Tech stack

.NET 8 · WPF · [scrcpy](https://github.com/Genymobile/scrcpy) · ADB · Direct3D 11 · XAudio2 · FFmpeg

## Credits & License

This project stands on the shoulders of great open-source work:

- **[scrcpy](https://github.com/Genymobile/scrcpy)** by Genymobile — licensed under the
  **Apache License 2.0**. Android Sync Control uses scrcpy for device mirroring and control.
- **[FFmpeg](https://ffmpeg.org)** — used for video/audio decoding. The shipped build is licensed
  under the **GNU Lesser General Public License v2.1 or later (LGPL-2.1-or-later)**.

Android Sync Control itself is released under the **MIT License** — see [LICENSE](LICENSE).
The FFmpeg libraries are shipped as separate DLLs (`av*.dll`, `sw*.dll`) next to the executable
and are only dynamically linked, so they keep their own LGPL-2.1-or-later terms: you may replace
them with your own compatible build.
The bundled third-party components and their license texts are listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Made by [tqk2811](https://github.com/tqk2811).
