# Privacy Policy

**Last updated:** 2026-04-17

## Short version

CapsNumTray collects no data. It does not phone home, transmit telemetry, log your keystrokes, or report to any analytics service.

## Details

### What CapsNumTray does on your machine

CapsNumTray runs locally as a Windows tray utility. It:

- Reads the current state of Caps Lock, Num Lock, and Scroll Lock via standard Windows APIs (`GetKeyState`, and optionally `WH_KEYBOARD_LL` for instant detection).
- Draws tray icons reflecting those states via `Shell_NotifyIconW`.
- Optionally toggles the state of those keys when you click.
- Writes your preferences (which icons are visible, poll interval, beep-on-toggle, etc.) to a local `CapsNumTray.ini` file next to the executable.

That is the full extent of its behavior. No process, file, or network resource outside your machine is touched by ordinary use.

### What CapsNumTray never does

- Collect, store, or transmit your keystrokes, clipboard, or any content you type.
- Send telemetry, analytics, crash reports, or usage metrics.
- Report to any server owned by the author.
- Include advertising, tracking pixels, or third-party SDKs.
- Require an online account, licence check, or activation.

### Network traffic (update checks)

CapsNumTray has an optional self-updater. When — and only when — you open the Settings dialog and invoke the update check, the app issues two HTTPS requests:

1. `GET https://api.github.com/repos/itsnateai/CaplockNumlock/releases/latest` to discover the latest release version.
2. If you then click **Upgrade Now**, a download from `https://github.com/...` (which GitHub redirects to `*.githubusercontent.com`) for the new executable and its `SHA256SUMS` integrity file.

These requests go to GitHub's infrastructure, not to the author. GitHub may log them per [GitHub's Privacy Statement](https://docs.github.com/en/site-policy/privacy-policies/github-privacy-statement). The author receives no information about who checked for updates or when.

No background timer performs update checks. The app never makes a network request unless you explicitly trigger one.

### Data stored locally

`CapsNumTray.ini` stores only your preferences (boolean flags and a polling-interval integer). It contains nothing personal and is never transmitted anywhere.

### Contact

Questions about this policy can be raised as a GitHub issue at <https://github.com/itsnateai/CaplockNumlock/issues>.

### Changes to this policy

If the app's data-handling behavior ever changes, this policy will be updated in the same commit that introduces the change, and the change will be called out in `CHANGELOG.md`.
