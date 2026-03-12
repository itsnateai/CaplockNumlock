# Changelog

All notable changes to CapsNumTray are documented here.

## [1.2.0] - 2026-03-12

### Fixed
- **P2-1: SyncIcons() was commented out** — `;` inside braces on `SetIconVisible` lines 114/118 was treated as AHK comment delimiter, not statement separator. Icon re-show caused up to 250ms blank flash. Split onto separate lines.
- **P1-1: Startup registry path for .ahk** — `ToggleStartup()` now writes `"AhkPath" "ScriptPath"` when running as .ahk script, bare path when compiled. Previously the bare .ahk path would fail at login if AHK wasn't the default handler.
- **P1-2: Both icons could be hidden** — Added guard in `SetIconVisible` that refuses to hide the last visible icon. Shows ToolTip warning instead.
- **P1-3: No 64-bit guard** — Added `#Requires AutoHotkey v2.0 64-bit` since `NOTIFYICONDATA` struct offsets are 64-bit only.
- **P3-1: Stale startup check** — `IsStartupEnabled()` now verifies the registry value matches the current script path, not just that it's non-empty.

### Added
- **OSD toggle in menu** — Right-click menu now has "Show OSD on toggle" checkable item, persisted to INI `[General] ShowOSD`
- **Beep toggle in menu** — Right-click menu now has "Beep on toggle" checkable item, persisted to INI `[General] BeepOnToggle`

### Documentation
- Updated README with all INI settings (ShowOSD, BeepOnToggle), startup feature, OSD, and beep docs
- Updated CLAUDE.md architecture section, line count, and known issues pointer
- Populated AUDIT_TASKS.md with full audit findings

## [1.1.0] - 2026-03-10

### Fixed
- **P1-A: LoadIcon fallback** — system icon ordinals are now integers (`32516`, `32515`) instead of broken string names (`"IDI_WARNING"`) which always returned NULL
- **P1-B: TaskbarCreated handler** — icons are now re-added after Explorer restarts/crashes (previously they'd vanish permanently)
- **P1-C: Compiled .exe icons** — added `@Ahk2Exe-AddResource` directives (IDs 210–213) and a PE resource fallback stage in `LoadIco` so the compiled binary carries its own icons
- **P1-D: DestroyIcon on shared handles** — `g_ownedIcons` Map tracks which handles are owned vs shared; `Cleanup()` only calls `DestroyIcon` on owned handles
- **P2-A: Blank icon flash on add** — `TrayAdd` now sends `NIF_MESSAGE` only on `NIM_ADD`, then `SyncIcons` fills in the icon/tip via `NIM_MODIFY`, eliminating the brief blank flash
- **P2-B: Hardcoded 32×32 icon size** — icon size is now DPI-aware via `GetDpiForSystem()` (16px at 96 DPI, scales with display scaling)
- **P2-C: Redundant `global` declarations** — removed unnecessary `global` re-declarations inside functions

### Added
- **NOTIFYICON_VERSION_4** — `NIM_SETVERSION` called after each `NIM_ADD`; enables modern tray protocol with click-position coordinates in wParam
- **NIF_SHOWTIP** — added `0x80` flag to `BuildNID` so hover tooltips display correctly under VERSION_4
- **OSD tooltip on toggle** — left-clicking an icon shows a brief floating "Caps Lock: ON/OFF" tooltip (auto-dismisses after 2s); configurable via `ShowOSD` in INI
- **Optional beep on toggle** — audible feedback on lock key change (`BeepOnToggle=0` in INI, off by default)
- **Run at startup** — right-click menu includes "Run at startup" with checkmark; managed via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` registry key
- **Version header in menu** — right-click menu shows "CapsNumTray v1.1.0" as a disabled header item
- **Multi-monitor aware menu position** — context menu now opens at the exact click location (`m.Show(clickX, clickY)`) with sign-extension for negative coordinates on multi-monitor setups
- **3-stage icon fallback** — `LoadIco` tries disk file → embedded PE resource (compiled) → Windows shared system icon

## [1.0.0] - 2026-03-10

### Added
- **Dual tray icons** — independent system tray icons for Caps Lock and Num Lock state (ON/OFF)
- **Click to toggle** — left-click either icon to toggle the corresponding lock key
- **Right-click menu** — context menu per icon: toggle key, show/hide the other icon, exit
- **Visibility persistence** — show/hide either icon; preference saved to `CapsNumTray.ini` and restored on next launch
- **Icon fallback** — missing `.ico` files fall back to Windows built-in icons; script never crashes on missing assets
- **Polling sync** — 250ms timer keeps icons accurate even when keys are changed externally
- **Clean exit** — `Cleanup()` removes tray icons and destroys icon handles on exit to avoid ghost icons
- **Win32 direct** — uses `Shell_NotifyIconW` directly for multiple independent tray icons (AHK's built-in tray only supports one)
