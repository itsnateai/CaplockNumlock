# Changelog

*LTR — Long-Term Release · one-click self-update built in.*

All notable changes to CapsNumTray are documented here.

## [2.4.5] — 2026-05-16

### Fixed — Settings checkbox text readability

CheckBox labels in Settings now render in pure white (`#FFFFFF`) instead of Theme.FgColor (Catppuccin Text, `#CDD6F3`). At 9pt with FlatStyle.Flat's grayscale anti-aliasing path the muted lavender shade read thin and ghostly against the dark background. Pure white gives the small-glyph contrast the user asked for.

Other body text (the polling description label, NumericUpDown digits, button text) still uses Theme.FgColor — if it feels comparatively dim alongside the brighter checkbox labels, will widen the change in a follow-up.

## [2.4.4] — 2026-05-16

Real-world-feedback polish on the Settings dialog.

### Fixed — Settings section header contrast

Section headers ("Tray Icons", "Startup", "Feedback", "Polling") were using `Theme.DimColor` (Catppuccin Subtext-area #A0A0C0, luminance ~164) while body text uses `Theme.FgColor` (#CDD6F3, luminance ~215). The bold weight partially compensated, but the net visual effect was the headers reading *less* prominent than the body — the eye expects section headers to pop, not recede.

The fix matches the convention already established in HelpForm: section headers now use `Theme.AccentBlue` (Catppuccin Blue #89B4FA), which gives them clear color-coded hierarchy against the regular-weight body text. Two dialogs (Help, Settings) now share the same header treatment for visual consistency.

No code-architecture changes; one-line color swap in `SettingsForm.cs`.

## [2.4.3] — 2026-05-16

Final closing-pass on the dark theme — the post-v2.4.2 verifier swarm flagged two real items, both addressed here. Per the verifier-loop diminishing-returns rule the loop closes after this release.

### Fixed — every button in Settings + Update now has themed hover/pressed states

`FlatStyle.Flat` without an explicit `FlatAppearance.MouseOverBackColor` falls back to `SystemColors.ButtonHighlight` on hover — a system light-grey that flashed against the dark palette every time the user moused over a button. Affects all six buttons in Settings (GitHub, Update, Help, OK, Apply, Cancel) and both buttons in the Update dialog (Upgrade Now, Cancel). Now sets `MouseOverBackColor = Theme.HighlightBg` (matches the menu's selected-item color) and `MouseDownBackColor = Theme.EditBgColor` (a touch darker on click for affordance).

### Fixed — misleading internal comment that fooled the v2.4.2 verifier swarm

The post-v2.4.2 round-3 verifier swarm split: three agents concluded the DWM attribute-19 fallback was broken because the comment in `NativeMethods.cs` claimed attribute 20 returns `S_OK` on Win10 1809–19H2 (which would make the `hr != 0` gate skip the fallback). The fourth agent caught that the comment itself was wrong — per Microsoft docs, attribute 20 was added in Win10 20H1 (build 19041), and on 1809–19H2 it actually returns `E_INVALIDARG` (`0x80070057`, non-zero), so the gate fires correctly. The code is right; the comment was misleading. Now fixed so the next reader (human or LLM) doesn't reach the same wrong conclusion.

No behaviour change from v2.4.2 — this is documentation-only correctness.

### Compatibility

Same .NET 8 runtime, same self-contained single-file publish, same self-update flow. Settings, INI format, icon resources unchanged. 2.4.x clients land here automatically on next update check.

## [2.4.2] — 2026-05-16

Second polish-pass on the dark theme — the post-v2.4.1 verifier swarm flagged a self-contradiction (per-paint GDI allocation in the same release that justified static caches) plus the two reachable-from-Settings dialogs that still rendered light.

### Fixed — self-contradiction in v2.4.1

- **`OnRenderItemCheck` no longer allocates a `Pen` per paint.** v2.4.1 added the submenu check-glyph override with `using var pen = new Pen(Theme.FgColor, 1.6f)` — inside the same `BoldSegmentRenderer` whose top-of-class comment explicitly justifies static caching because "paint fires on every mouse-move over a menu item." The new `CheckPen` is now declared `static readonly` alongside `BgBrush` / `HighlightBrush` / `FgBrush` / `SeparatorPen` and reused across paints.
- **DWM legacy attribute 19 is now only sent when attribute 20 fails.** v2.4.1 fired both attributes unconditionally to cover Win10 1809–19H2 (where only attribute 19 works). That's correct on every shipping Windows build today, but defensive against a hypothetical future DWM revision: now checks the HRESULT from attribute 20 and only calls attribute 19 on non-`S_OK`.

### Added — dark theme reaches the remaining two dialogs

The post-v2.4.0 verifier flagged it, the post-v2.4.1 verifier flagged it again with REJECT severity, and the user request was "fix all": **Help** and **Update** dialogs are now themed.

- **`HelpForm`** — `RichTextBox` background is `Theme.BgColor`, title and body text in `Theme.FgColor`, section headers (the `——— BASIC USAGE ———` style lines) in `Theme.AccentBlue` for hierarchy. Same first-show optimizations as Settings (`SetStyle` double-buffer + `UserPaint`, `ShowInTaskbar = false`, `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` in `OnHandleCreated` with the attr-19 Win10 1809–19H2 fallback).
- **`UpdateDialog`** — form background `Theme.BgColor`, status text `Theme.FgColor`, detail text `Theme.DimColor` (italic, secondary), progress bar bg `Theme.EditBgColor`, progress fill `Theme.AccentGreen` (Catppuccin Green replaces the Material `#4CAF50` green for palette unity), error-state text `Theme.AccentWarn` (Catppuccin Peach replaces Material `#FF9800` orange), buttons `FlatStyle.Flat` with `Theme.DividerColor` border. The post-update success toast (the brief "✓ CapsNumTray updated to vX.Y.Z" popup near the system tray after a self-update) is also dark now — `Theme.BgColor` background, `Theme.FgColor` text.

### Added — palette additions in `Theme.cs`

Three new accent colors (Catppuccin Mocha):
- `AccentBlue` (`#89B4FA`) — section headers in HelpForm, semantic links elsewhere
- `AccentGreen` (`#A6E3A1`) — success states (UpdateDialog progress fill, future "OK" affordances)
- `AccentWarn` (`#FAB387`, Catppuccin Peach) — warnings and errors visible against the dark background (UpdateDialog error state)

`Theme.cs` now exposes 10 colors total — the 7 from v2.4.1 plus these three accents. Still a single source of truth.

### Compatibility

Same .NET 8 runtime, same self-contained single-file publish, same self-update flow. INI format, icon resources, and settings are unchanged. Self-updating from any 2.4.x or 2.3.x release lands here automatically.

## [2.4.1] — 2026-05-16

Polish-pass on the 2.4.0 dark theme, addressing every gap the post-ship verifier swarm (six agents, Sonnet+Opus pair-by-topic on Diff-clean / Gap-audit / Code-review) converged on.

### Fixed — Settings dialog

- **NumericUpDown spinner band stays themed.** The "Fallback poll interval" `NumericUpDown` was rendering with its digit area dark but the up/down arrow strip beside it system-grey — a visible split inside the otherwise themed dialog. `NumericUpDown` is composed of two child controls (`Controls[0]` is the internal `UpDownButtons` HWND, `Controls[1]` is the text field); the parent's `BackColor` does not propagate to the spinner band because `UpDownButtons` paints its own chrome via `ControlPaint`. Now sets `Controls[0].BackColor = Theme.EditBgColor` on the band so the dark digit area and the spinner strip align. The arrow glyphs themselves stay system-rendered but read fine against the dark background.
- **CheckBox tick + focus rectangle respect the theme.** The six `CheckBox` controls had `ForeColor`/`BackColor` set on the body but the actual checkmark was drawn by `Application.RenderWithVisualStyles` (light-themed glyph against our dark BG) and the focus rectangle was drawn by `ControlPaint.DrawFocusRectangle` (XOR'd against `SystemColors.ControlText` → near-invisible dotted line on `#1E1E2E`). Setting `FlatStyle = FlatStyle.Flat` switches both glyph and focus-rect to a render path that respects our `ForeColor`. `FlatAppearance.BorderColor`, `CheckedBackColor`, and `MouseOverBackColor` are pinned to the theme palette so hover and checked states stay coordinated.
- **Win10 1809–19H2 titlebar fallback.** `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE = 20)` was added in v2.4.0 for the dark titlebar. On Win10 1809 through 19H2 attribute 20 returns `S_OK` but has no visible effect — those builds need the legacy undocumented attribute 19. `OnHandleCreated` now calls both attributes unconditionally; DWM silently ignores whichever is unsupported on the current build. Win11 keeps the modern path, Win10 20H1+ keeps the modern path, Win10 1809–19H2 now also gets a dark titlebar.

### Fixed — context menu

- **"Visibility" submenu check glyphs are now visible.** The default `ToolStripProfessionalRenderer.OnRenderItemCheck` paints the checkmark via `ControlPaint.DrawMenuGlyph` using system colors (dark blue/black) — barely visible against our `#353550` highlight background. `BoldSegmentRenderer` now overrides `OnRenderItemCheck` and draws a custom anti-aliased two-segment checkmark in `Theme.FgColor`, so the toggle state of each Caps/Num/Scroll-Lock-icon visibility option reads at a glance.

### Refactored — single source of truth

- **Palette extracted to `Theme.cs`.** v2.4.0 declared the same five Catppuccin Mocha constants (`BgColor`, `FgColor`, `DimColor`, `DividerColor`, `EditBgColor`, plus the menu-renderer's `MenuBg`/`HighlightBg`/`SeparatorColor`) independently in three files: `BoldSegmentRenderer` (in `TrayApplication.cs`), `SettingsForm`, and `OsdForm`. Future palette tweaks would have required three coordinated edits with drift risk on every change. Now one `internal static class Theme` exposes the seven canonical colors and the three former call sites pull from it. Net delta: one new file (~25 lines), ~30 lines deleted across the others. Zero behaviour change for end users — purely an internal refactor that the verifier swarm flagged as the highest-value future-proofing item.

### Fixed — minor GDI / dead-code

- **`OsdForm` border pen is no longer allocated per paint.** The 1px border drawn in `OnPaint` was using `using var pen = new Pen(...)`, which constructs and destroys a GDI handle on every `WM_PAINT`. The OSD only lives 2 seconds and paints once or twice in practice so the impact was negligible, but it was inconsistent with `BoldSegmentRenderer`'s explicit cache-GDI-statically policy. Now a `static readonly Pen` shared for the process lifetime.
- **Dead `BorderPen` field removed from `BoldSegmentRenderer`.** The renderer had two separate `Pen` fields (`SeparatorPen` and `BorderPen`) initialized from the same `SeparatorColor` — pure duplication. `OnRenderToolStripBorder` now reuses `SeparatorPen`. Saves one GDI handle and one source of future drift.

### Compatibility

Same .NET 8 runtime, same self-contained single-file publish, same self-update flow. INI format, icon resources, and settings are unchanged from 2.4.0. Self-updating from 2.4.0 (or any 2.3.x) lands here automatically on the next update check.

## [2.4.0] — 2026-05-16

### Added — dark theme (Catppuccin Mocha)

The tray right-click menu, the **Settings** window, and the on-screen-display tooltip ("Caps Lock ON" / "Num Lock OFF") are now rendered in a coordinated dark palette matching the SyncthingPause sibling app. Same five constants are reusable for any future tray-app sibling that wants to match.

- **Context menu** — background `#1E1E2E`, text `#CDD6F3`, item highlight `#353550`, separator + border `#404050`. The bold "ON"/"OFF" segment of the state header line is preserved. Disabled items (the version header at the top of the menu) render in `#808095` instead of the default embossed-grey from `ControlPaint.DrawStringDisabled` — the WinForms default text-renderer ignores `e.TextColor` on the disabled path, so a manual `TextRenderer.DrawText` branch was needed to keep the colour intentional. The "Visibility" submenu inherits the same chrome (drop-down `BackColor` + `ForeColor` set explicitly so the dropdown arrow and any out-of-paint-path fallbacks pick up the dark palette).
- **Settings window** — form background `#1E1E2E`, foreground `#CDD6F3`, section headers in dim purple-grey `#A0A0C0`, NumericUpDown input in a slightly-lighter edit colour `#2A2A3E`, all six buttons flat-styled with a `#404050` divider border.
- **OSD tooltip** — borderless form rendered with the same `#1E1E2E` / `#CDD6F3` pair plus a 1px `#404050` border drawn via `OnPaint` so it reads cleanly on dark wallpapers. Replaces the previous tooltip-yellow `(255, 255, 225)` background.

The tray-icon hover tooltip (the small "Caps Lock: ON" box that pops out of the tray icon itself) is painted by the Windows shell from `NOTIFYICONDATAW.szTip` and follows the system theme — no in-process API to override it.

### Fixed — first-show lag on the Settings window

Three compounding sources of perceived "lag" on the very first open of the Settings dialog, all addressed:

- **Light titlebar on a dark body.** Windows draws the non-client area (titlebar, borders) using the system theme before DWM repaints, so a dark form would briefly show with a default light titlebar attached — that "settling" frame reads as the form popping in and then re-rendering. Fixed by calling `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)` in `OnHandleCreated`, before the first `WM_NCPAINT`. The titlebar is now dark from the first frame.
- **No form-level double-buffering.** Each `Controls.Add` in the constructor (we add ~20 controls) was triggering an immediate paint, producing visible per-child flicker as the form composed itself. Fixed by enabling `OptimizedDoubleBuffer | AllPaintingInWmPaint | UserPaint` via `SetStyle()`, which paints the entire form once into an off-screen buffer and blits it in a single `BitBlt`.
- **No layout batching.** Without `SuspendLayout()` / `ResumeLayout()`, each `Controls.Add` triggered a separate layout pass on the parent form (so ~20 passes for a single dialog). Now bracketed with a single deferred layout at the end.
- **Settings dialog was registering a taskbar button.** A settings dialog reached from a tray right-click doesn't need a taskbar entry of its own — and registering one walks through `ITaskbarList3`, jump-list lookup, and an icon-load for the thumbnail, which is what was causing the "popup" delay reported in user testing. Fixed by setting `ShowInTaskbar = false` (matches SyncthingPause's pattern). The dialog still appears in Alt+Tab for users who want to switch back to it.

### Internal

- `NativeMethods.cs` gains `DwmSetWindowAttribute` P/Invoke + `DWMWA_USE_IMMERSIVE_DARK_MODE = 20` constant.
- `BoldSegmentRenderer` (the existing custom renderer that bolds one substring of an item's text) now subclasses `ProfessionalColorTable` via a nested `DarkColorTable` — needed because the default colour table is queried by `ToolStripProfessionalRenderer` for paint regions outside the methods we override (the submenu drop shadow, scroll arrows on long submenus, the check-background), and without overriding it those regions would still leak the system light-blue/grey.
- GDI brush + pen instances in the renderer are `static readonly` and live for the process lifetime — paint fires on every mouse-move over a menu item, so per-paint allocation would burn GDI handles in 24/7 tray operation.

### Compatibility

Same .NET 8 runtime, same self-contained single-file publish, same self-update flow. Settings, INI format, and icon resources are unchanged from 2.3.3. Self-updating from 2.3.x lands here automatically the next time CapsNumTray checks for updates.

## [2.3.3] — 2026-05-14

### Fixed — display scaling on 125%+ monitors

Settings, Help, and Update dialogs now render correctly at 125%, 150%, and 175% Windows display scale. Previously the dialogs were declared "DPI-aware" only at the OS level — the per-form auto-scale baseline was left unpinned, so on a 125%/150% laptop a dialog would silently double-scale on first show: button bottom borders disappeared below the visible area, the **Fallback poll interval** NumericUpDown digits clipped behind the spinner band, and the top row of utility buttons (GitHub / Update / Help) in Settings had their bottom border clipped.

The fix aligns five layers that all need to agree:

- **`app.manifest`** (new) declares `PerMonitorV2` DPI awareness + Win10/11 supportedOS — read by the OS loader before any managed code runs.
- **`CapsNumTray.csproj`** gains `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>` and `<ApplicationDefaultFont>Segoe UI, 9pt</ApplicationDefaultFont>`. Without `ApplicationHighDpiMode`, the source-generated `ApplicationConfiguration.Initialize()` defaults to `SystemAware` and contradicts the manifest.
- **Every `Form` subclass** (`SettingsForm`, `HelpForm`, `OsdForm`, `UpdateDialog`) sets `AutoScaleDimensions = new SizeF(96F, 96F)` *before* `AutoScaleMode = AutoScaleMode.Dpi`. The order matters: WinForms snapshots `AutoScaleDimensions` at the moment `AutoScaleMode` is set, so flipping the order leaves the baseline at whatever the first-realized monitor reported.
- **`UpdateDialog`** was inheriting the WinForms default `AutoScaleMode.Font`, which scales by Font.Height ratio and diverges from `PerMonitorV2`'s pixel scaling on non-integer DPI ratios. Now uses `AutoScaleMode.Dpi` to align with the rest of the app.
- **`SettingsForm`** internal sizing: the **Fallback poll interval** NumericUpDown was 60×24 — the spinner band composes three nested HWNDs whose scaling math diverges by ~25px at 125%, which is enough to push digits behind the spinner in a 60px control. Now 80×26 with a `MinimumSize` floor so AutoScale can't shrink the spinner band into the digit area at any scale factor. The top row of utility buttons (GitHub / Update / Help) was 24px tall — bottom border clipped at 125%+; bumped to 26 (still narrower than the 28px primary row so visual contrast is preserved via the 24px width differential).

Honest framing: v2.3.1 was tagged as a "battle-tested Long-Term Release" and v2.3.2 reaffirmed it — but those claims were based on testing at 100% display scale only. **This is the actual LTR baseline.** If you're running CapsNumTray on a Windows 11 laptop at 125% or higher scale (the default for most 2024+ laptops) and any dialog looked clipped, this release is for you.

### Also fixed (caught by the verifier-pair sweep before tagging)

- **Update dialog cancel-button no longer drifts off-centre at 125%+ scale.** When the dialog showed "You're on the latest version!" / "managed by winget" / an error, `_btnCancel.Location` was reassigned to a literal `(170, 112)` design-pixel position. At 100% this centred the button; at 125%+ AutoScale had already walked `ClientSize` and the button width to device pixels, so the raw-literal reassignment landed off-centre. All three call sites now compute `(ClientSize.Width - _btnCancel.Width) / 2` at runtime so the button stays centred at any scale.
- **Post-update toast now respects display scaling.** The "✓ CapsNumTray updated to vX.Y.Z" toast that briefly appears after a successful self-update was an anonymous `new Form` without the AutoScale pin pair, so its 12×8 padding rendered at design pixels even on 125%+ monitors (same pattern that bit SyncthingPause v3.0.1).

### Minor cosmetic

The Update dialog's marquee progress-bar animation during downloads moves at a fixed pixel pace, so at 150%+ scale it visibly travels slower across the bar than at 100% (`step=4` is a logical-pixel literal compared against the now-larger device-pixel bar width). Functional only — animation pacing, not clipping. Does not affect download integrity or SHA-256 verification.

## [2.3.2] — 2026-04-25

### Security
- **Self-update URL allowlist hardened against host-confusion attacks.** The previous URL check used substring-match on `.githubusercontent.com/` — a hostile redirect target like `https://evil.example/.githubusercontent.com/payload.exe` would have passed the check because the substring appears anywhere in the URL string. The check is now `Uri.Host`-based: the host must exactly match one of the allowed CDN names (`objects.githubusercontent.com` or `release-assets.githubusercontent.com`), and the github.com / api.github.com paths must start with our exact owner/repo prefix. The same gate now also runs on the SHA256SUMS download URL, which previously had no allowlist check at all (relied entirely on hash verification). No visible change for normal updates from GitHub Releases — this only tightens paths that should never have been reached.

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
