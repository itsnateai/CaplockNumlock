# CaplockNumlock — Production Readiness Audit

> Generated: 2026-03-12
> Stack: AutoHotkey v2 (Win32 Shell_NotifyIconW)
> Git: v1.1.0 (3 commits)

---

## P0 — Bugs & Security

_(none found)_

## P1 — High Priority / Functionality

- [x] **P1-1: Startup registry writes bare .ahk path** — Fixed: uses `A_AhkPath + A_ScriptFullPath` for script mode, bare path for compiled. _(v1.2.0)_
- [x] **P1-2: Both icons can be hidden, leaving app unreachable** — Fixed: `SetIconVisible` guards against hiding last visible icon with ToolTip warning. _(v1.2.0)_
- [x] **P1-3: No 64-bit guard on struct layout** — Fixed: `#Requires AutoHotkey v2.0 64-bit`. _(v1.2.0)_

## P2 — Medium Priority / Quality

- [x] **P2-1: SyncIcons() commented out in SetIconVisible** — Fixed: split compound statements onto separate lines. _(v1.2.0)_
- [x] **P2-2: ShowOSD/BeepOnToggle have no UI and are undocumented** — Fixed: added checkable menu items + ToggleOSD/ToggleBeep functions with INI persistence + updated README. _(v1.2.0)_
- [x] **P2-3: README missing startup feature docs** — Fixed: updated README with all features and settings. _(v1.2.0)_

## P3 — Low Priority / Nice-to-Have

- [x] **P3-1: IsStartupEnabled checks only non-empty, not correct path** — Fixed: now compares registry value to expected path. _(v1.2.0)_

## P4 — Future / Ideas

- [ ] **P4-1: Scroll Lock support** — Third tray icon for Scroll Lock, same pattern.
- [ ] **P4-2: Per-monitor DPI** — `GetDpiForSystem()` returns system DPI, not per-monitor. On mixed-DPI setups, tray icon could be wrong size.
- [ ] **P4-3: Dark/light theme detection** — Swap to high-contrast icon variants when taskbar uses light theme.
- [ ] **P4-4: Settings GUI** — Instead of INI-only settings, add a simple Settings dialog (follows other AHK app patterns in workspace).
