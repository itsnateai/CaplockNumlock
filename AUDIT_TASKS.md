# CaplockNumlock — Production Readiness Audit

> Generated: 2026-03-12
> Stack: AutoHotkey v2 (Win32 Shell_NotifyIconW)
> Git: v1.3.1

---

## Production Readiness Audit — v1.3.1 (2026-03-12)

### Findings

| ID | Severity | File:Line | Description | Fix Applied |
|----|----------|-----------|-------------|-------------|
| F1 | P3 | `CapsNumTray.ahk:3` | Header comment said `v1.3.0`, should be `v1.3.1` | Updated to `v1.3.1` |
| F2 | P3 | `AUDIT_TASKS.md:5` | Header said `v1.1.0 (3 commits)` — stale | Updated to `v1.3.1` |
| F3 | P3 | `CLAUDE.md:25` | Line count said `~480 lines`, actual is 503 | Updated to `~500 lines` |
| F4 | P3 | `CLAUDE.md:15` | Contains personal filesystem path | Replaced with generic placeholder |

### Cross-Reference Verification

| Check | Result |
|-------|--------|
| Version strings match across files | **Fixed** — header comment, `g_version`, CHANGELOG, AUDIT_TASKS now all say 1.3.1 |
| INI keys consistent (read vs write) | **Clean** — `ShowCaps`, `ShowNum`, `ShowOSD`, `BeepOnToggle` match exactly |
| Function params match all call sites | **Clean** — all 20 functions verified |
| Icon IDs consistent | **Clean** — `ID_CAPS=10`, `ID_NUM=11` used consistently |
| `WM_TRAY` value consistent | **Clean** — `0x8010` in code and CLAUDE.md |

### Deep Sweep Results (4A–4H)

| Check | Status | Findings |
|-------|--------|----------|
| 4A: Secrets & credentials | **Clean** | No API keys, tokens, passwords, or secrets. One personal path in CLAUDE.md (F4). |
| 4B: Memory leaks | **Clean** | All timers, GUI objects, and icon handles properly managed. Cleanup() destroys owned handles. |
| 4C: Data exfiltration & privacy | **Clean** | Zero network calls. Only external URL is GitHub link (user-initiated browser open). |
| 4D: Console & debug cleanup | **Clean** | No MsgBox, OutputDebug, or debug statements. |
| 4E: Unsafe code patterns | **Clean** | No eval, no user-input injection. DllCall uses fixed parameters only. |
| 4F: Manifest & permissions | **N/A** | Not a browser extension. Registry access limited to startup toggle (appropriate). |
| 4G: Async error handling | **Clean** | AHK is single-threaded. All timers and OnMessage handlers are correct. |
| 4H: Distribution packaging | **Clean** | No OS metadata, test files, build artifacts, or large binaries tracked. |

### Category Audit Summary

| Category | Status |
|----------|--------|
| Logic & correctness | **Clean** — no wrong operators, off-by-ones, unreachable code, or scope issues |
| Cross-file consistency | **Clean** — single source file, all constants defined once, INI keys match |
| Race conditions & timing | **Clean** — single-threaded, one-shot timers auto-clean, no shared state conflicts |
| Error handling | **Clean** — try/catch on RegRead, GUI Destroy, GUI Show; all appropriate |
| Resource cleanup | **Clean** — OnExit removes icons, DestroyIcon on owned handles, timers auto-clean |
| Edge cases | **Clean** — both-icons-hidden guard, 3-stage icon fallback, multi-monitor sign-extension |
| User-facing text | **Clean** — no typos, consistent capitalization, correct terminology |
| HTML/CSS completeness | **N/A** — pure AHK project |
| Documentation & metadata | **Fixed** — version strings and line counts corrected |

---

## Historical Findings (previous audits)

### P0 — Bugs & Security

_(none found)_

### P1 — High Priority / Functionality

- [x] **P1-1: Startup registry writes bare .ahk path** — Fixed: uses `A_AhkPath + A_ScriptFullPath` for script mode, bare path for compiled. _(v1.2.0)_
- [x] **P1-2: Both icons can be hidden, leaving app unreachable** — Fixed: `SetIconVisible` guards against hiding last visible icon with ToolTip warning. _(v1.2.0)_
- [x] **P1-3: No 64-bit guard on struct layout** — Fixed: `#Requires AutoHotkey v2.0 64-bit`. _(v1.2.0)_

### P2 — Medium Priority / Quality

- [x] **P2-1: SyncIcons() commented out in SetIconVisible** — Fixed: split compound statements onto separate lines. _(v1.2.0)_
- [x] **P2-2: ShowOSD/BeepOnToggle have no UI and are undocumented** — Fixed: added checkable menu items + ToggleOSD/ToggleBeep functions with INI persistence + updated README. _(v1.2.0)_
- [x] **P2-3: README missing startup feature docs** — Fixed: updated README with all features and settings. _(v1.2.0)_

### P3 — Low Priority / Nice-to-Have

- [x] **P3-1: IsStartupEnabled checks only non-empty, not correct path** — Fixed: now compares registry value to expected path. _(v1.2.0)_

### P4 — Future / Ideas

- [ ] **P4-1: Scroll Lock support** — Third tray icon for Scroll Lock, same pattern.
- [ ] **P4-2: Per-monitor DPI** — `GetDpiForSystem()` returns system DPI, not per-monitor. On mixed-DPI setups, tray icon could be wrong size.
- [ ] **P4-3: Dark/light theme detection** — Swap to high-contrast icon variants when taskbar uses light theme.
- [x] **P4-4: Settings GUI** — Added full Settings dialog with GitHub + Help buttons, OK/Apply/Cancel. _(v1.3.0)_
