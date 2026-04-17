# Privacy Policy

**Version:** 1.1 · **Effective:** 2026-04-17

## Short version

CapsNumTray collects no personal data. It does not transmit telemetry, record your keystrokes, or report to any analytics service. It is a local-only Windows tray utility. Its one optional network use — the manual **Update** button — talks only to GitHub.

## The long version

### What CapsNumTray does on your machine

CapsNumTray runs locally on Windows. It:

- Reads the current state of the **Caps Lock**, **Num Lock**, and **Scroll Lock** toggle keys via standard Windows APIs (`GetKeyState`, and optionally the low-level keyboard hook `WH_KEYBOARD_LL` for instant detection).
- Draws system tray icons reflecting those states via `Shell_NotifyIconW`.
- Toggles the state of those keys when you click, via `SendInput`.
- Reads the Windows theme setting (`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\SystemUsesLightTheme`) to pick the correct icon colour. Read-only.
- Writes the files listed under [Files the app may write](#files-the-app-may-write).

That is the full extent of its behavior. No process, file, or network resource off your machine is touched during ordinary use.

### About the keyboard hook

The optional low-level keyboard hook (`WH_KEYBOARD_LL`) sees every key event the OS dispatches. **CapsNumTray's hook callback reads only the virtual-key code** (the "which key" field) and reacts only when that code is `VK_CAPITAL`, `VK_NUMLOCK`, or `VK_SCROLL`. It does not inspect, store, transmit, or otherwise examine the content of any other keystroke. Every other key event is passed straight back to the OS untouched via `CallNextHookEx`. The hook is source-inspectable in `TrayApplication.cs`.

### What CapsNumTray never does

- Record, store, or transmit the content of your keystrokes, clipboard, or any text you type.
- Send telemetry, analytics, crash reports, or usage metrics.
- Write log, trace, or diagnostic files to disk. (The app emits a handful of `Trace.WriteLine` diagnostics to the Windows debug channel, visible only if you attach a tool like DebugView — these are not persisted.)
- Phone home to any server owned by the maintainer.
- Include advertising, tracking, or third-party SDKs. The `.csproj` has zero NuGet dependencies.
- Require an online account, licence check, or activation.

### Files the app may write

All of these stay on your machine:

- **`CapsNumTray.ini`** — your preferences (six fields: three icon-visibility booleans, `ShowOSD`, `BeepOnToggle`, `PollInterval` integer). Written next to the executable. No identifiers, paths, or history.
- **Startup shortcut** — if and only if you enable *Run at Windows startup* in Settings, a `CapsNumTray.lnk` is created in your user Startup folder (`%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\`). Deleted when you toggle the setting off. On each launch, if the shortcut exists, its `TargetPath` is silently rewritten to match the current executable path (this keeps the shortcut valid after a WinGet upgrade).
- **Temporary icon files** — at startup, each embedded icon is extracted to `%TEMP%\CapsNumTray_<guid>.ico` so Windows can load it, then deleted immediately. Ephemeral.
- **Update artifacts** — during a user-initiated self-update, `CapsNumTray.exe.new` and `CapsNumTray.exe.old` appear briefly next to the executable while the new binary is verified and swapped in. Leftovers from an interrupted update are cleaned up on the next launch.

### Network traffic

The app makes a network request only when **you** invoke it via the **Update** button in the Settings dialog. There is no background update check, no startup ping, no timer-driven poll.

When you invoke an update, the app issues:

1. `GET https://api.github.com/repos/itsnateai/CaplockNumlock/releases/latest` — to discover the latest release version.
2. If you then click **Upgrade Now**, a download of the new executable and its `SHA256SUMS` integrity file from `https://github.com/...` (which GitHub redirects to `*.githubusercontent.com`). Only these two hosts are accepted; any other redirect target is rejected.

These requests go to GitHub's infrastructure. GitHub may log them per [GitHub's Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-privacy-statement). The maintainer receives no information about who checked for updates or when.

### GDPR, UK GDPR, and CCPA

Because CapsNumTray collects no personal data, the maintainer acts as neither a data controller nor a data processor under GDPR/UK GDPR. There is no personal information to "sell" or "share" under the CCPA, and no subject-access request can be meaningfully fulfilled because there is no stored data to retrieve.

### Jurisdiction

The maintainer is an individual based in Alberta, Canada. There is no company or legal entity behind the project.

### Contact

- **General privacy questions**, or anything you're happy to discuss in public: open a GitHub issue at <https://github.com/itsnateai/CaplockNumlock/issues>.
- **Sensitive concerns**, or anything you want to raise privately: use GitHub's private security advisory channel at <https://github.com/itsnateai/CaplockNumlock/security/advisories/new>. Reports are encrypted, visible only to you and the maintainer, and tied to an authenticated GitHub account — no email exposure on either end.

### Changes to this policy

Any change to the app's data-handling behavior will be made in the same commit that introduces the change, called out in `CHANGELOG.md`, and reflected here before the release tag ships. The **Version** and **Effective** date at the top of this file will be updated accordingly.
