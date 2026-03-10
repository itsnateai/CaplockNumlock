# CapsNumTray

Caps Lock + Num Lock tray indicators for Windows. Shows the current state of both keys as system tray icons with click-to-toggle support.

**[Installation](#installation)** · **[Features](#features)** · **[Configuration](#configuration)** · **[How It Works](#how-it-works)**

## Installation

### Requirements

- Windows 10/11
- [AutoHotkey v2](https://www.autohotkey.com/) (or use the compiled .exe)

### Quick Start

1. Download the latest release (`CapsNumTray.exe`) — no AutoHotkey installation needed, icons are embedded
2. Or clone/download this repo and run `CapsNumTray.ahk` with [AutoHotkey v2](https://www.autohotkey.com/)

## Features

Lightweight tray indicators that let you see and control Caps Lock and Num Lock at a glance:

- **Tray icons** show ON/OFF state for both Caps Lock and Num Lock
- **Left-click** any icon to toggle that key
- **Right-click** for a context menu with toggle, show/hide, and exit options
- **Visibility persistence** — hide either icon and the preference sticks across restarts
- **Graceful fallbacks** — missing icon files fall back to Windows built-in icons

## Configuration

Settings are stored in `CapsNumTray.ini` (auto-created next to the script). Visibility of each icon can be toggled via the right-click context menu.

```ini
[Visibility]
ShowCaps=1
ShowNum=1
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ShowCaps` | `1` | Show the Caps Lock tray icon |
| `ShowNum` | `1` | Show the Num Lock tray icon |

## How It Works

Uses the Win32 `Shell_NotifyIconW` API directly to create independent tray icons (AHK's built-in tray only supports one icon). A 250ms timer polls `GetKeyState` and updates icons accordingly. Click events are handled via a custom window message callback.

## Compilation

To compile to a standalone `.exe` (no AutoHotkey installation needed):

```bash
MSYS_NO_PATHCONV=1 "X:/_Projects/_tools/Ahk2Exe.exe" /in CapsNumTray.ahk /out CapsNumTray.exe /icon CapsLockOn.ico /compress 0 /silent
```

> **Note:** Use `/compress 0` — default compression triggers Windows Defender false positives.

## Files

| File | Purpose |
|------|---------|
| `CapsNumTray.ahk` | Main script |
| `CapsLockOn.ico` | Tray icon — Caps Lock ON |
| `CapsLockOff.ico` | Tray icon — Caps Lock OFF |
| `NumLockOn.ico` | Tray icon — Num Lock ON |
| `NumLockOff.ico` | Tray icon — Num Lock OFF |
| `CapsNumTray.ini` | User config (auto-created, gitignored) |

## License

[MIT](LICENSE)
