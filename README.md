# CapsNumTray

Caps Lock, Num Lock, and Scroll Lock tray indicators for Windows. Shows the current state of each key as system tray icons with click-to-toggle support.

**[Installation](#installation)** · **[Features](#features)** · **[Configuration](#configuration)** · **[How It Works](#how-it-works)**

## Installation

### Requirements

- Windows 10/11
- .NET 8 Runtime (or use the self-contained single-file `.exe` release)

### Quick Start

1. Download the latest release (`CapsNumTray.exe`) — self-contained, no dependencies needed
2. Or clone this repo and build from source:

```bash
cd CapsNumTray
dotnet build -c Release
dotnet run -c Release
```

### Publish

To produce a standalone single-file `.exe`:

```bash
cd CapsNumTray
dotnet publish -c Release --self-contained true -p:PublishSingleFile=true
```

## Features

Lightweight tray indicators that let you see and control Caps Lock, Num Lock, and Scroll Lock at a glance:

- **Tray icons** show ON/OFF state for Caps Lock, Num Lock, and Scroll Lock (opt-in)
- **Left-click** any icon to toggle that key
- **Right-click** for a context menu with toggle, show/hide, settings, and exit options
- **Settings GUI** — full settings dialog with checkboxes for all options, plus GitHub and Help buttons
- **Visibility persistence** — hide any icon and the preference sticks across restarts
- **OSD tooltip** — brief floating notification when toggling a key (configurable)
- **Optional beep** — audible feedback on toggle (off by default)
- **Run at startup** — toggle via Settings dialog, managed via Start Menu shortcut
- **Per-monitor DPI** — icons scale correctly on mixed-DPI multi-monitor setups
- **Dark/light theme detection** — reads system theme setting at startup
- **Help window** — resizable help dialog with full usage guide
- **Graceful fallbacks** — missing icon files fall back to embedded resources or Windows built-in icons

## Configuration

Settings are stored in `CapsNumTray.ini` (auto-created next to the executable). Visibility of each icon can be toggled via the right-click context menu.

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

Uses the Win32 `Shell_NotifyIconW` API directly (via P/Invoke) to create independent tray icons. A 250ms timer polls `GetKeyState` and updates icons on state change only. Click events are handled via NOTIFYICON_VERSION_4 custom window message callback.

## Files

| File | Purpose |
|------|---------|
| `CapsNumTray/` | C# (.NET 8 WinForms) source — 10 files |
| `CapsNumTray.ahk` | Legacy AHK v2 script |
| `icons/*.ico` | 9 icon files (On/Off for each key, plus light-theme OFF variants) |
| `CapsNumTray.ini` | User config (auto-created, gitignored) |

## License

[MIT](LICENSE)
