# CapsNumTray

Caps Lock, Num Lock, and Scroll Lock tray indicators for Windows. Shows the current state of each key as system tray icons with click-to-toggle support.

**[Installation](#installation)** · **[Features](#features)** · **[Configuration](#configuration)** · **[How It Works](#how-it-works)**

## Installation

### Requirements

- Windows 10/11
- [AutoHotkey v2](https://www.autohotkey.com/) (or use the compiled .exe)

### Quick Start

1. Download the latest release (`CapsNumTray.exe`) — no AutoHotkey installation needed, icons are embedded
2. Or clone/download this repo and run `CapsNumTray.ahk` with [AutoHotkey v2](https://www.autohotkey.com/)

## Features

Lightweight tray indicators that let you see and control Caps Lock, Num Lock, and Scroll Lock at a glance:

- **Tray icons** show ON/OFF state for Caps Lock, Num Lock, and Scroll Lock (opt-in)
- **Left-click** any icon to toggle that key
- **Right-click** for a context menu with toggle, show/hide, settings, and exit options
- **Settings GUI** — full settings dialog with checkboxes for all options, plus GitHub and Help buttons
- **Visibility persistence** — hide any icon and the preference sticks across restarts
- **OSD tooltip** — brief floating notification when toggling a key (configurable)
- **Optional beep** — audible feedback on toggle (off by default)
- **Run at startup** — toggle via Settings dialog, managed via registry
- **Per-monitor DPI** — icons scale correctly on mixed-DPI multi-monitor setups
- **Dark/light theme detection** — reads system theme setting at startup
- **Help window** — resizable help dialog with full usage guide
- **Graceful fallbacks** — missing icon files fall back to embedded resources (compiled) or Windows built-in icons

## Configuration

Settings are stored in `CapsNumTray.ini` (auto-created next to the script). Visibility of each icon can be toggled via the right-click context menu.

```ini
[Visibility]
ShowCaps=1
ShowNum=1
ShowScroll=0

[General]
ShowOSD=1
BeepOnToggle=0
```

| Setting | Default | Description |
|---------|---------|-------------|
| `ShowCaps` | `1` | Show the Caps Lock tray icon |
| `ShowNum` | `1` | Show the Num Lock tray icon |
| `ShowScroll` | `0` | Show the Scroll Lock tray icon (opt-in, disabled by default) |
| `ShowOSD` | `1` | Show floating tooltip when toggling a key |
| `BeepOnToggle` | `0` | Play a beep sound when toggling a key |

All settings can be configured via the Settings dialog (right-click any icon → Settings...).

## How It Works

Uses the Win32 `Shell_NotifyIconW` API directly to create independent tray icons (AHK's built-in tray only supports one icon). A 250ms timer polls `GetKeyState` and updates icons accordingly. Click events are handled via a custom window message callback.

## Compilation

To compile to a standalone `.exe` (no AutoHotkey installation needed):

```bash
MSYS_NO_PATHCONV=1 "X:/_Projects/_.claude/_tools/Ahk2Exe.exe" /in CapsNumTray.ahk /out CapsNumTray.exe /icon icons/CapsLockOn.ico /compress 0 /silent
```

> **Note:** Use `/compress 0` — default compression triggers Windows Defender false positives.

## Files

| File | Purpose |
|------|---------|
| `CapsNumTray.ahk` | Main script |
| `icons/CapsLockOn.ico` | Tray icon — Caps Lock ON |
| `icons/CapsLockOff.ico` | Tray icon — Caps Lock OFF |
| `icons/NumLockOn.ico` | Tray icon — Num Lock ON |
| `icons/NumLockOff.ico` | Tray icon — Num Lock OFF |
| `icons/ScrollLockOn.ico` | Tray icon — Scroll Lock ON |
| `icons/ScrollLockOff.ico` | Tray icon — Scroll Lock OFF |
| `CapsNumTray.ini` | User config (auto-created, gitignored) |

## License

[MIT](LICENSE)
