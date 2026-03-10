# CLAUDE.md — CaplockNumlock

## Overview
CapsNumTray is a lightweight AHK v2 utility that adds system tray icons showing the current state of Caps Lock and Num Lock. Left-click toggles the key, right-click provides a menu to toggle, show/hide icons, or exit. Visibility preferences persist via INI file.

## Tech Stack
- **Language:** AutoHotkey v2
- **Dependencies:** None (uses Win32 Shell_NotifyIconW directly)
- **Platform:** Windows 10/11

## Build & Run

```bash
# Run (requires AHK v2 installed)
"C:/Users/swift/.xn/_Projects/_tools/AutoHotkey64.exe" CapsNumTray.ahk

# Compile to standalone .exe
MSYS_NO_PATHCONV=1 "X:/_Projects/_tools/Ahk2Exe.exe" /in CapsNumTray.ahk /out CapsNumTray.exe /icon CapsLockOn.ico /compress 0 /silent
```

## Key Files

| File | Purpose |
|------|---------|
| `CapsNumTray.ahk` | Main script (~210 lines) |
| `CapsLockOn.ico` / `CapsLockOff.ico` | Caps Lock tray icons |
| `NumLockOn.ico` / `NumLockOff.ico` | Num Lock tray icons |
| `CapsNumTray.ini` | User visibility prefs (gitignored, auto-created) |

## Architecture
Single-file script with these sections:
1. **Globals** — icon handles, INI path, visibility flags
2. **Core functions** — `SyncIcons()` polls key state every 250ms, `Toggle*Lock()` toggles keys
3. **Visibility management** — `SetIconVisible()` shows/hides icons and persists to INI
4. **Tray message handler** — `OnTrayMsg()` handles left-click (toggle) and right-click (context menu)
5. **Shell_NotifyIconW helpers** — `BuildNID()`, `TrayAdd()`, `TrayModify()`, `TrayRemove()` wrap the Win32 API
6. **Icon loading** — `LoadIco()` loads .ico files with Windows built-in fallbacks
7. **Cleanup** — `Cleanup()` removes tray icons and destroys icon handles on exit

## Conventions
- Uses raw Win32 `Shell_NotifyIconW` instead of AHK's built-in tray (allows multiple independent tray icons)
- Icon IDs: Caps Lock = 10, Num Lock = 11
- Custom tray callback message: `0x8010`
- Visibility prefs stored in `[Visibility]` section of INI

## Known Issues
- None currently tracked — run `/project-audit` to populate
