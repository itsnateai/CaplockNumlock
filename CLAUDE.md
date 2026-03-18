# CLAUDE.md — CaplockNumlock

## Overview
CapsNumTray is a system tray utility that shows the current state of Caps Lock, Num Lock, and Scroll Lock as independent tray icons. Left-click toggles the key, right-click provides a menu to toggle, show/hide icons, adjust settings, or exit. Visibility preferences persist via INI file. Scroll Lock icon is opt-in (disabled by default).

Two implementations exist: the original AHK v2 script and a C# (.NET 8 WinForms) port.

## Tech Stack
- **AHK version:** AutoHotkey v2 (single-file script)
- **C# version:** .NET 8, WinForms, no NuGet dependencies
- **Platform:** Windows 10/11

## Build & Run

### C# (.NET 8) — primary
```bash
cd CapsNumTray
dotnet build -c Release
dotnet run -c Release

# Publish as single-file exe
dotnet publish -c Release --self-contained true -p:PublishSingleFile=true
```

### AHK v2 — legacy
```bash
# Run (requires AHK v2 installed)
"C:/path/to/AutoHotkey64.exe" CapsNumTray.ahk
```

## Key Files

### C# project (`CapsNumTray/`)
| File | Purpose |
|------|---------|
| `Program.cs` | Entry point — single-instance enforcement, `Application.Run()` |
| `TrayApplication.cs` | Core form — Shell_NotifyIconW tray icons, WndProc, context menus, key toggling |
| `NativeMethods.cs` | All P/Invoke declarations and the NOTIFYICONDATAW struct |
| `IconManager.cs` | DPI-aware icon loading with 3-stage fallback (embedded resource, file, system) |
| `ConfigManager.cs` | INI file reader/writer, compatible with AHK format |
| `OsdForm.cs` | Borderless topmost auto-hiding tooltip (replaces BalloonTip) |
| `SettingsForm.cs` | Settings dialog — visibility, OSD, beep, startup |
| `HelpForm.cs` | Resizable scrollable help window |
| `StartupManager.cs` | Startup .lnk shortcut via WScript.Shell COM |
| `CapsNumTray.csproj` | Project file — embeds all 9 .ico resources |

### Shared
| File | Purpose |
|------|---------|
| `CapsNumTray.ahk` | Original AHK v2 script (~590 lines) |
| `icons/*.ico` | 9 icon files (CapsLockOn/Off, NumLockOn/Off, ScrollLockOn/Off, plus _Light OFF variants) |
| `CapsNumTray.ini` | User settings (gitignored, auto-created at runtime) |

## Architecture (C# version)

- **TrayApplication** — invisible Form that owns the message loop. Manages 3 independent tray icons via raw `Shell_NotifyIconW` P/Invoke (not `NotifyIcon` class). Polls key states every 250ms via `System.Windows.Forms.Timer` with state-change-only updates.
- **IconManager** — loads icons once at startup, caches handles, tracks ownership (owned handles get `DestroyIcon`, shared system handles do not). Implements `IDisposable`.
- **ConfigManager** — reads/writes a simple INI file. Graceful defaults if file missing or locked.
- **OsdForm** — static `ShowOsd()` creates borderless topmost form near cursor, auto-hides via timer. Uses a shared static font to avoid GDI leaks.
- **NativeMethods** — all Win32 interop: `Shell_NotifyIconW`, `keybd_event`, `GetKeyState`, `Beep`, `LoadImage`, `DestroyIcon`, `RegisterWindowMessage`, DPI APIs.

## Conventions
- Uses raw Win32 `Shell_NotifyIconW` instead of `NotifyIcon` (allows multiple independent tray icons)
- Icon IDs: Caps Lock = 10, Num Lock = 11, Scroll Lock = 12
- Custom tray callback message: `0x8010`
- NOTIFYICON_VERSION_4 protocol: lParam = event|iconID, wParam = clickXY
- Visibility prefs stored in `[Visibility]` section of INI
- All handles use `nint` (not `IntPtr`), all paths use `Environment.ProcessPath` (not `Assembly.Location`)
- No async, no NuGet, no polling timers for file changes — pure single-threaded WinForms
- Every `IDisposable` has a verified disposal path; COM objects released in `finally` blocks

## Resource Management Rules
When modifying the C# code, follow these rules strictly:
- Every `new Font()` must be stored in a field and disposed in `Dispose(bool)`
- Every `new Timer()` must be stopped then disposed in `Dispose(bool)`
- Every `Process.GetProcessesByName()` result must wrap each Process in `using`
- Every `Process.Start()` return must be wrapped in `using`
- Every COM object must be released via `Marshal.ReleaseComObject` in a `finally` block
- `ContextMenuStrip` must NOT use `using` (Show() is non-blocking) — store as field, dispose on `Closed` event
- `Close()` on a `Show()`-ed form auto-calls `Dispose()` — do not call `Dispose()` in `FormClosed` handlers
- Guard `BeginInvoke` calls with `IsHandleCreated` / `_disposed` checks for shutdown safety
- `LParam.ToInt32()` can throw on 64-bit — always use `unchecked((int)(long)m.LParam)`

## Known Issues
- See `CHANGELOG.md` for resolved items

## Status

**v2.0.0 — C# port (2026-03-18)**

Full AHK v2 to C# (.NET 8 WinForms) conversion. 13 audit issues found and fixed across security, memory leaks, and correctness. All resource disposal paths verified.
