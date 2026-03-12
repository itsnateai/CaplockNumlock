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
"<path-to>/AutoHotkey64.exe" CapsNumTray.ahk

# Compile to standalone .exe
MSYS_NO_PATHCONV=1 "<path-to>/Ahk2Exe.exe" /in CapsNumTray.ahk /out CapsNumTray.exe /icon icons/CapsLockOn.ico /compress 0 /silent
```

## Key Files

| File | Purpose |
|------|---------|
| `CapsNumTray.ahk` | Main script (~500 lines) |
| `icons/*.ico` | Tray icons (CapsLockOn/Off, NumLockOn/Off) |
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
- Icon IDs: Caps Lock = 10, Num Lock = 11
- Custom tray callback message: `0x8010`
- Visibility prefs stored in `[Visibility]` section of INI

## Gotchas & Audit Notes
- Version string is defined in TWO places: header comment (line 3) and `g_version` global (line 22) — keep both in sync
- `NOTIFYICONDATA` struct is 976 bytes on 64-bit only — the `#Requires 64-bit` directive is load-bearing
- `g_ownedIcons` Map tracks LoadImage handles vs shared LoadIcon handles — only owned handles get `DestroyIcon` in Cleanup
- `ApplySettingsGUI` uses explicit `global` declaration (not bare `global`) to avoid silent typo bugs
- One-shot timers use negative period (e.g., `-2000`) — these auto-clean and don't need `SetTimer(..., 0)`
- `NIF_SHOWTIP` (0x80) flag in `BuildNID` is required for tooltips under `NOTIFYICON_VERSION_4`

## Known Issues
- See `AUDIT_TASKS.md` for current findings and `CHANGELOG.md` for resolved items
- **P4-2:** `GetDpiForSystem()` is system-wide, not per-monitor — mixed-DPI setups may show wrong-sized icons

## Audit Status
- **Last audit:** v1.3.1 (2026-03-12) — production readiness audit
- **Result:** 0 P0, 0 P1, 0 P2, 4 P3 (all documentation fixes, applied)
- **Deep sweep:** All 8 categories clean (secrets, memory, exfiltration, debug, unsafe code, permissions, async, packaging)
