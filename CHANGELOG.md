# Changelog

All notable changes to CapsNumTray are documented here.

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
