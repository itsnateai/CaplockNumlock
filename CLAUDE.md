# CLAUDE.md — CaplockNumlock

## Overview
CapsNumTray is a lightweight AHK v2 utility that adds system tray icons showing the current state of Caps Lock, Num Lock, and Scroll Lock. Left-click toggles the key, right-click provides a menu to toggle, show/hide icons, or exit. Visibility preferences persist via INI file. Scroll Lock icon is opt-in (disabled by default).

## Tech Stack
- **Language:** AutoHotkey v2
- **Dependencies:** None (uses Win32 Shell_NotifyIconW directly)
- **Platform:** Windows 10/11

## Build & Run

```bash
# Run (requires AHK v2 installed)
"C:/Users/swift/.xn/_Projects/_.claude/_tools/Ahk/AutoHotkey64.exe" CapsNumTray.ahk

# Compile to standalone .exe
MSYS_NO_PATHCONV=1 "X:/_Projects/_.claude/_tools/Ahk/Ahk2Exe.exe" /in CapsNumTray.ahk /out CapsNumTray.exe /icon icons/CapsLockOn.ico /compress 0 /silent
```

## Key Files

| File | Purpose |
|------|---------|
| `CapsNumTray.ahk` | Main script (~575 lines) |
| `icons/*.ico` | Tray icons (CapsLockOn/Off, NumLockOn/Off, ScrollLockOn/Off, plus _Light OFF variants) |
| `CapsNumTray.ini` | User settings (gitignored, auto-created) |

## Architecture
Single-file script with these sections:
1. **Globals** — icon handles, INI path, visibility/settings flags
2. **Core functions** — `SyncIcons()` polls key state every 250ms, `Toggle*Lock()` toggles keys
3. **Visibility management** — `SetIconVisible()` shows/hides icons and persists to INI (guards against hiding both)
4. **Tray message handler** — `OnTrayMsg()` handles left-click (toggle) and right-click (context menu)
5. **Settings GUI** — `ShowSettingsGUI()`, `ApplySettingsGUI()`, `CloseSettingsGUI()` with full dialog
6. **Help window** — `ShowHelpWindow()` with resizable scrollable text
7. **Settings toggles** — `ToggleOSD()`, `ToggleBeep()` with INI persistence
8. **Startup management** — `IsStartupEnabled()`, `ToggleStartup()` via registry Run key
9. **Shell_NotifyIconW helpers** — `BuildNID()`, `TrayAdd()`, `TrayModify()`, `TrayRemove()` wrap the Win32 API
10. **Icon loading** — `LoadIco()` loads .ico files with 3-stage fallback (disk → PE resource → system)
11. **Cleanup** — `Cleanup()` removes tray icons and destroys owned icon handles on exit

## Conventions
- Uses raw Win32 `Shell_NotifyIconW` instead of AHK's built-in tray (allows multiple independent tray icons)
- Icon IDs: Caps Lock = 10, Num Lock = 11, Scroll Lock = 12
- Custom tray callback message: `0x8010`
- Visibility prefs stored in `[Visibility]` section of INI

## Known Issues
- See `AUDIT_TASKS.md` for current findings and `CHANGELOG.md` for resolved items

## Status

**v1.4.1 — Light-theme icon support (2026-03-13)**

Light-theme OFF icon variants added. All deferred tasks resolved. See FINAL_REPORT.md for v1.4.0 summary.
