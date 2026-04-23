# Changelog

*LTR — Long-Term Release · one-click self-update built in.*

All notable changes to CapsNumTray are documented here.

## [2.3.1] — 2026-04-23

**Battle-tested Long-Term Release.**

CapsNumTray is now stable. If you're running an older version, open **Settings → Update** and the app will download, verify, and relaunch itself — no manual install, no reboot.

Includes a small polish to make the right-click menu consistent with the rest of the tray-app family: the **CapsNumTray** header is now bold, and Windows 11 menu spacing matches MicMute and MWBToggle.

This is the new LTR baseline.

## [2.3.0] — 2026-04-23

Settings dialog refresh: the **Startup** option is now tucked up beside Tray Icons instead of buried at the bottom, and the action row is split into two tidy rows (GitHub/Update/Help above, OK/Apply/Cancel below). The Help window got a typography pass to match MicMute.

## [2.2.10] — 2026-04-18

### Fixed
- **Right-click menu "Caps Lock is ON/OFF" now actually toggles cleanly.** Previously, clicking the state item from the right-click menu could leave the tray icon, the notification popup, and the menu label itself out of sync — e.g. the OSD would briefly flash "ON" but the icon would revert to OFF while the menu label ended up saying something different again on next open. Left-click on the tray icon already worked; the bug was specific to the right-click menu path. Same fix also covers Num Lock and Scroll Lock toggle menu items.

### Changed
- **Bottom padding on the right-click menu.** "Exit CapsNumTray" no longer sits jammed against the bottom edge of the menu — there's now a bit of breathing room on top and bottom for a cleaner look.

Supersedes v2.2.9 as the current long-term release.

## [2.2.9] — 2026-04-18

### Windows 11 tray icon visibility
- **Tray icons now auto-show in the taskbar on first run on Windows 11.** Previously, Win11 22H2+ hid each new tray icon in the overflow flyout until you went to Settings → Personalization → Taskbar → *Other system tray icons* and flipped the Caps Lock / Num Lock / Scroll Lock toggles on one by one. That's three manual clicks against the whole point of the app. First launch now flips those toggles for you. If you've deliberately turned one of them off yourself, that choice is respected — we never flip a user-set OFF back to ON.
- **Each tray icon shows up with a clean label in the Windows Settings tray list** ("Caps Lock", "Num Lock", "Scroll Lock") so you can tell them apart at a glance instead of seeing three unlabeled entries.
- **Tray icons stay visible if Explorer crashes and restarts while the app is running.** The Explorer-restart handler now re-runs the auto-promote logic idempotently, so a mid-session Explorer crash, update, or manual restart doesn't silently drop icons back into overflow.

Sandbox-validated on a cold-boot Win11 25H2 guest: fresh install of `CapsNumTray.exe` → both default icons (Caps + Num) appear in the taskbar within 2 seconds, full registry schema present, labels correct.

Supersedes v2.2.8 as the current long-term release.

## [2.2.8] - 2026-04-17 — New LTR

*Second hardening pass from a fresh four-agent red-team audit focused on enterprise / Microsoft Store / WinGet scenarios.*

### Fixed
- **Tray icons now refresh when you change your Windows theme.** Switching light/dark taskbar themes (or toggling high-contrast) while the tray is running used to leave the OFF icons looking wrong until you restarted the app. They now update live.
- **Startup lock is more resilient.** If another running program in your session happens to own a kernel object with the same name, the tray now declines gracefully instead of crashing at launch.
- **"Run at startup" logs a diagnostic line if your system blocks the shortcut folder** (Folder Redirection GPO, read-only share). Previously it silently did nothing.

### Changed
- **Self-update is more tolerant of GitHub's CDN changes.** The download source check now accepts any `*.githubusercontent.com` host, so future GitHub redirect changes won't break updates.
- **Self-update handles gzipped asset downloads.** Defensive: the HTTP client now transparently decompresses, so if GitHub ever switches to compressed binaries the integrity check still sees the real content.

### Known limitations
- **The published binary is not yet Authenticode-signed.** Corporate environments enforcing WDAC / strict SmartScreen / AppLocker Publisher rules will block it. Signing is tracked as the next priority.

Supersedes v2.2.7 as the current long-term release.

## [2.2.7] - 2026-04-17

*Hardening release from a four-agent red-team audit. Same toggle fix as v2.2.6, plus the issues the audit surfaced under the "what else got missed?" lens.*

### Fixed
- **Tray icons now resync on resume-from-sleep and RDP reconnect.** Previously, if your laptop slept with Caps Lock on and resumed with it off (BIOS-toggled), the tray icon could show the wrong state until the next keypress. Same for RDP reconnects that sync keyboard state server-side.
- **Launching on a multi-user machine (RDS / fast user switching) no longer fails silently for the second user.** The single-instance lock is now scoped per-session, so one user can't block another from running the tray.
- **Graceful decline in restricted runtimes.** If the single-instance lock can't be acquired due to an OS-level permission error (Session 0, AppContainer), the app exits cleanly instead of crashing.

### Changed
- **Tray API failures now leave a diagnostic trace.** If Windows ever silently rejects a tray-icon update (e.g., during an Explorer crash), the failure is logged (visible in DebugView) instead of leaving a stale icon with no clue why.
- **Release workflow actions are now SHA-pinned** (`actions/checkout`, `actions/setup-dotnet`) so a hypothetical upstream tag move can't alter what we publish.

Supersedes v2.2.6 as the current long-term release.

## [2.2.6] - 2026-04-16

### Fixed
- **Left-click and right-click menu now actually toggle the lock key.** Since the C# port shipped, clicking a tray icon or picking "click to turn On/Off" from the menu silently did nothing. Both routes now flip Caps Lock, Num Lock, and Scroll Lock as intended. Supersedes the v2.2.4 LTR designation — this is the new long-term release.

## [2.2.5] - 2026-04-16

### Changed

- Auto-updater now refuses to install a release that doesn't publish an integrity checksum alongside it. Previously, a release shipped without its checksum file would have been installed unverified — now it's aborted with a clear message instead.

## [2.2.4] - 2026-04-16

### Fixed
- **Duplicate trays if launched twice** — second launch now exits cleanly instead of spawning another tray that fought the first one over the same icons.
- **Self-update relaunch reliability** — new version now waits briefly for the old one to release before giving up, so updates don't strand you with nothing running.
- **Settings no longer closes your open Help window** — Help window now stays put when you click OK or Cancel on Settings.
- **Settings survive a sudden power loss** — preferences are written atomically, so an unexpected shutdown mid-save can't blank your config.
- **Icons load correctly for users with accented characters in their Windows username** — fixed a path-handling bug that silently fell back to generic Windows icons in that case.
- **Polling auto-enables if the keyboard hook is blocked** — in enterprise environments where low-level hooks are disallowed, the tray now falls back to a 10-second poll instead of silently going stale.
- **Update integrity check is now strict** — if the checksum file can't be fetched, the update is aborted instead of proceeding unverified.

### Changed
- Update checker now rotates its connection every 5 minutes so long-running sessions pick up GitHub DNS changes automatically.

## [2.1.0] - 2026-03-31

### Added
- **Low-level keyboard hook** — toggle key changes (Caps Lock, Num Lock, Scroll Lock) are now detected instantly via `WH_KEYBOARD_LL` instead of relying on polling. Icon updates are immediate.
- **Configurable poll interval** — new "Polling" section in Settings with a numeric control (0–300 seconds). Default is 10 seconds. Set to 0 to disable polling entirely. The poll timer now serves as a failsafe for external state changes (RDP, SendKeys).

### Changed
- Default poll interval changed from 5 seconds to 10 seconds (keyboard hook handles normal usage instantly)

## [2.0.1] - 2026-03-19

### Bug Fixes
- **No stray invisible window above the taskbar at startup** — the tray now launches cleanly with no ghost window hanging around.

## [2.0.0] - 2026-03-18

### Added
- **Full C# (.NET 8 WinForms) port** — complete rewrite from AHK v2 to C#. All features preserved: multi-icon tray, click-to-toggle, context menus, Settings dialog, Help window, OSD tooltips, beep feedback, startup management, dark/light theme detection, per-monitor DPI scaling, 3-stage icon fallback.
- **Embedded icons** — all icons are built into the .exe, so nothing external is needed at runtime.
- **Single-file build** — ships as one self-contained .exe.

### Changed
- **Startup management** — "Run at startup" now uses a Start Menu shortcut so Windows' own startup tools can see and manage it.
- **INI format** — stays fully compatible with existing AHK-generated `.ini` files, so your settings carry over untouched.

### Fixed
- **No memory creep over long sessions** — the tray now cleans up after itself properly, so leaving it running for days no longer grows its footprint.
- **No more rare crash when exiting the app** — shutdown path is now clean even when clicks land mid-exit.
- **Context menus close cleanly** — right-click menus no longer get stuck visible or block other clicks.
- **Reliable icon cleanup on exit** — tray icons go away immediately instead of lingering as ghosts until you hover them.
- **Correct behavior on 64-bit Windows** — click coordinates from the tray are interpreted correctly on all Windows versions.

## [1.4.1] - 2026-03-13

### Added
- **Light-theme icon variants** — OFF icons now use dark gray (`#606060`) instead of white when Windows light theme is detected, ensuring visibility on light taskbars. New icon files: `CapsLockOff_Light.ico`, `NumLockOff_Light.ico`, `ScrollLockOff_Light.ico`. Embedded as PE resources (IDs 216-218).

## [1.4.0] - 2026-03-12

### Added
- **Scroll Lock tray icon** — third independent tray icon (ID 12) for Scroll Lock state. Opt-in via `ShowScroll=0` in INI (disabled by default). Includes left-click toggle, right-click menu, Settings GUI checkbox, OSD tooltip, beep feedback, and Explorer restart recovery.
- **ScrollLockOn.ico / ScrollLockOff.ico** — matching green/gray up-down arrow icons in `icons/` folder. Embedded as PE resources (IDs 214-215) for compiled .exe.
- **Per-monitor DPI** — replaced `GetDpiForSystem()` with `GetDpiForWindow(A_ScriptHwnd)` via new `GetEffectiveDpi()` helper. Tray icons now scale correctly on mixed-DPI multi-monitor setups. Falls back to system DPI on older Windows versions.
- **Dark/light theme detection** — reads `SystemUsesLightTheme` registry value at startup via `DetectLightTheme()`. Stored in `g_lightTheme` global.

### Changed
- `SetIconVisible` last-icon guard now counts all 3 icons (was only checking 2)
- Context menus for each icon now include show/hide options for both other icons

## [1.3.1] - 2026-03-12

### Fixed
- **Help close handler crash risk** — `hlp.OnEvent("Close")` was an inline `g_helpGui.Destroy()` with no try wrapper. If the GUI was already destroyed, it would throw an unhandled error. Extracted `CloseHelpWindow()` function with `try`, matching the `CloseSettingsGUI` pattern.
- **Bare `global` in ApplySettingsGUI** — `global` with no names made every variable in the function global. A typo (e.g. `g_showODS`) would silently create a new global instead of erroring. Replaced with explicit `global g_showCaps, g_showNum, g_showOSD, g_beepOnToggle, g_settingsGui`.

## [1.3.0] - 2026-03-12

### Added
- **Settings GUI** — full settings dialog accessible from right-click menu with checkboxes for all options (icon visibility, OSD, beep, startup). Follows MicMute/EQSwitch pattern with GitHub + Help buttons, OK/Apply/Cancel
- **Help window** — resizable, scrollable help dialog with full usage guide, settings explanations, and technical notes

### Changed
- **Icons moved to `icons/` folder** — cleaner project structure. `@Ahk2Exe-AddResource` and `LoadIco` paths updated
- **Context menu simplified** — OSD, beep, and startup toggles removed from right-click menu in favor of the Settings GUI. Menu now has: toggle, show/hide other icon, Settings..., Exit
- Inline `ToggleOSD()` and `ToggleBeep()` functions retained for internal use by Settings GUI

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
