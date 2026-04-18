using System.Runtime.InteropServices;

namespace CapsNumTray;

/// <summary>
/// Main application form. Manages multiple independent tray icons via Shell_NotifyIconW,
/// polls key states, and provides context menus.
/// </summary>
internal sealed class TrayApplication : Form
{
    public static readonly string Version = typeof(TrayApplication).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    // Icon IDs matching AHK convention
    private const uint ID_CAPS = 10;
    private const uint ID_NUM = 11;
    private const uint ID_SCROLL = 12;

    private readonly ConfigManager _config;
    private readonly IconManager _icons;
    private readonly System.Windows.Forms.Timer _syncTimer;
    private readonly uint _wmTaskbarCreated;
    private readonly NativeMethods.LowLevelKeyboardProc _hookProc;
    private nint _hookHandle;

    // Cached last-known states to avoid redundant updates
    private bool _lastCapsState;
    private bool _lastNumState;
    private bool _lastScrollState;
    private bool _statesInitialized;

    // Cached tooltip strings
    private static readonly string CapsOn = "Caps Lock: ON";
    private static readonly string CapsOff = "Caps Lock: OFF";
    private static readonly string NumOn = "Num Lock: ON";
    private static readonly string NumOff = "Num Lock: OFF";
    private static readonly string ScrollOn = "Scroll Lock: ON";
    private static readonly string ScrollOff = "Scroll Lock: OFF";

    // _disposed is read from the SystemEvents background thread before
    // BeginInvoke-ing to the UI thread. Mark volatile so the write from
    // Dispose is observable on ARM64 without relying on x86/x64's stronger
    // memory model.
    private volatile bool _disposed;
    private bool _cleanedUp;
    private bool _syncing;

    // Set by ToggleLockKey to a future TickCount. While Environment.TickCount64
    // < this value, non-forced SyncIcons calls early-return. Purpose: after our
    // SendInput-generated toggle, the LL keyboard hook fires and posts a
    // SyncIcons via BeginInvoke — but GetKeyState's toggle bit lags the actual
    // global flag by a message-pump cycle, so the hook's SyncIcons reads stale
    // state and reverts the icon we just correctly set. The in-flight window
    // lets our deterministic TrayModify (from ToggleLockKey) stand.
    // Not marked volatile — C# disallows volatile on long. Aligned 8-byte
    // reads/writes are atomic on x64 which is the only platform we target.
    private long _toggleInFlightUntilTicks;

    private readonly BoldSegmentRenderer _menuRenderer = new();

    // Cache struct size — Marshal.SizeOf uses reflection internally
    private static readonly uint NidSize = (uint)Marshal.SizeOf<NativeMethods.NOTIFYICONDATAW>();

    public TrayApplication()
    {
        // Invisible owner form
        Text = "CapsNumTray";
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Visible = false;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Size = System.Drawing.Size.Empty;

        // Config
        string? exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        string iniPath = Path.Combine(exeDir ?? ".", "CapsNumTray.ini");
        _config = new ConfigManager(iniPath);

        // Theme detection
        bool lightTheme = DetectLightTheme();

        // Icons
        _icons = new IconManager(Handle, lightTheme);

        // Register TaskbarCreated message. Windows guarantees non-zero for any
        // non-empty string, so a 0 return indicates a system failure worth
        // tracing — and would alias WM_NULL in WndProc, causing tray churn.
        _wmTaskbarCreated = NativeMethods.RegisterWindowMessage("TaskbarCreated");
        if (_wmTaskbarCreated == 0)
            System.Diagnostics.Trace.WriteLine(
                $"CapsNumTray: RegisterWindowMessage(TaskbarCreated) returned 0 (err={Marshal.GetLastWin32Error()})");

        // Snapshot existing NotifyIconSettings subkeys BEFORE our NIM_ADDs so
        // TrayIconPromoter can identify the ones Explorer creates for us even
        // when it ships without ExecutablePath populated (Win11 schema quirk).
        var trayBaseline = TrayIconPromoter.CaptureBaseline();

        // Add visible tray icons. Each NIM_ADD seeds a non-empty tooltip so
        // Explorer writes the full NotifyIconSettings schema (see TrayAdd).
        if (_config.ShowCaps) TrayAdd(ID_CAPS);
        if (_config.ShowNum) TrayAdd(ID_NUM);
        if (_config.ShowScroll) TrayAdd(ID_SCROLL);

        // Initial sync
        SyncIcons(force: true);

        // Auto-promote our tray icons to visible on Win11 22H2+ (no-op on
        // Win10 and when the user has explicitly hidden us). 500 ms × 20 =
        // 10 s cap. See TrayIconPromoter for the two-phase identification
        // rules and why the timer runs to its full cap for multi-icon apps.
        StartTrayIconPromotion(trayBaseline);

        // Low-level keyboard hook for instant toggle detection. If install fails
        // (group policy, some enterprise environments), the polling timer still
        // keeps icons in sync — just with poll-interval lag instead of instant.
        _hookProc = KeyboardHookCallback;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _hookProc,
            NativeMethods.GetModuleHandle(null), 0);
        if (_hookHandle == 0 && _config.PollInterval == 0)
        {
            // Hook failed AND polling disabled — icons would never update.
            // Force-enable polling at a reasonable default so the app stays useful.
            _config.PollInterval = 10;
        }

        // Polling timer — safety net for external key state changes (RDP, other apps).
        // The keyboard hook handles normal keystrokes instantly.
        _syncTimer = new System.Windows.Forms.Timer();
        ApplyPollInterval(_config.PollInterval);

        // Resume-from-suspend and session unlock can desync our cached state
        // (BIOS may toggle Caps LED on resume without generating WM_KEYUP;
        // RDP reconnect syncs keyboard state server-side). Force a resync.
        Microsoft.Win32.SystemEvents.PowerModeChanged += OnPowerModeChanged;
        Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object? sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        // General covers theme (light/dark taskbar) and high-contrast changes.
        if (e.Category != Microsoft.Win32.UserPreferenceCategory.General) return;
        if (_disposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke(() =>
            {
                _icons.ReloadForTheme(DetectLightTheme());
                SyncIcons(force: true);
            });
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private void OnPowerModeChanged(object? sender, Microsoft.Win32.PowerModeChangedEventArgs e)
    {
        // Fires on a non-UI thread — marshal back before touching icons.
        if (e.Mode != Microsoft.Win32.PowerModes.Resume) return;
        SafeForceSync();
    }

    private void OnSessionSwitch(object? sender, Microsoft.Win32.SessionSwitchEventArgs e)
    {
        if (e.Reason != Microsoft.Win32.SessionSwitchReason.SessionUnlock &&
            e.Reason != Microsoft.Win32.SessionSwitchReason.ConsoleConnect &&
            e.Reason != Microsoft.Win32.SessionSwitchReason.RemoteConnect) return;
        SafeForceSync();
    }

    private void SafeForceSync()
    {
        if (_disposed || !IsHandleCreated) return;
        try { BeginInvoke(() => SyncIcons(force: true)); }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private static bool DetectLightTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? val = key?.GetValue("SystemUsesLightTheme");
            return val is int i && i == 1;
        }
        catch
        {
            return false;
        }
    }

    // ── Prevent Application.Run() from showing the form ────────────────────
    // Application.Run(form) internally sets Visible = true, which shows a
    // minimized tool window bar above the taskbar. Override SetVisibleCore
    // to suppress the initial show while still creating the handle needed
    // for WndProc and the message loop.
    protected override void SetVisibleCore(bool value)
    {
        // This form is tray-only — never show it. Just ensure the handle
        // exists so WndProc and the message loop work.
        if (!IsHandleCreated) CreateHandle();
        base.SetVisibleCore(false);
    }

    // ── Keyboard Hook ──────────────────────────────────────────────────────

    private nint KeyboardHookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && !_disposed && IsHandleCreated)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == NativeMethods.VK_CAPITAL ||
                vkCode == NativeMethods.VK_NUMLOCK ||
                vkCode == NativeMethods.VK_SCROLL)
            {
                // Key-up means the toggle state has changed
                if ((int)wParam == NativeMethods.WM_KEYUP || (int)wParam == NativeMethods.WM_SYSKEYUP)
                    BeginInvoke(() => SyncIcons());
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    // ── Poll Interval ─────────────────────────────────────────────────────

    private void ApplyPollInterval(int seconds)
    {
        _syncTimer.Stop();
        if (seconds > 0)
        {
            _syncTimer.Interval = seconds * 1000;
            _syncTimer.Tick -= OnSyncTimerTick;
            _syncTimer.Tick += OnSyncTimerTick;
            _syncTimer.Start();
        }
    }

    private void OnSyncTimerTick(object? sender, EventArgs e) => SyncIcons();

    // ── Key State Helpers ──────────────────────────────────────────────────

    private static bool IsKeyToggled(byte vk) =>
        (NativeMethods.GetKeyState(vk) & 1) != 0;

    private static void ToggleKey(byte vk)
    {
        var inputs = new NativeMethods.INPUT[2];
        inputs[0].type = NativeMethods.INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = vk;
        inputs[1].type = NativeMethods.INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = vk;
        inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
        uint sent = NativeMethods.SendInput(2, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        if (sent != 2)
            System.Diagnostics.Trace.WriteLine(
                $"CapsNumTray: SendInput rejected (sent={sent}, err={Marshal.GetLastWin32Error()})");
    }

    // ── Sync Icons ─────────────────────────────────────────────────────────

    private void SyncIcons(bool force = false)
    {
        // Guard against re-entrancy: Shell_NotifyIconW can pump the message
        // queue, which may dispatch another timer tick or a WndProc that
        // calls SyncIcons again before the previous call completes.
        if (_syncing) return;
        // Suppress non-forced syncs during the in-flight window after our
        // own SendInput — otherwise the LL hook's stale GetKeyState read
        // would revert the icon we just correctly set. Physical user
        // keypresses outside this window are unaffected.
        if (!force && Environment.TickCount64 < _toggleInFlightUntilTicks) return;
        _syncing = true;
        try
        {
            bool capsOn = IsKeyToggled(NativeMethods.VK_CAPITAL);
            bool numOn = IsKeyToggled(NativeMethods.VK_NUMLOCK);
            bool scrollOn = IsKeyToggled(NativeMethods.VK_SCROLL);

            if (!force && _statesInitialized &&
                capsOn == _lastCapsState && numOn == _lastNumState && scrollOn == _lastScrollState)
                return;

            _statesInitialized = true;

            if (force || capsOn != _lastCapsState)
            {
                _lastCapsState = capsOn;
                if (_config.ShowCaps)
                    TrayModify(ID_CAPS, capsOn ? _icons.CapsOn : _icons.CapsOff, capsOn ? CapsOn : CapsOff);
            }
            if (force || numOn != _lastNumState)
            {
                _lastNumState = numOn;
                if (_config.ShowNum)
                    TrayModify(ID_NUM, numOn ? _icons.NumOn : _icons.NumOff, numOn ? NumOn : NumOff);
            }
            if (force || scrollOn != _lastScrollState)
            {
                _lastScrollState = scrollOn;
                if (_config.ShowScroll)
                    TrayModify(ID_SCROLL, scrollOn ? _icons.ScrollOn : _icons.ScrollOff, scrollOn ? ScrollOn : ScrollOff);
            }
        }
        finally
        {
            _syncing = false;
        }
    }

    // ── Toggle Lock Keys ───────────────────────────────────────────────────

    // NOTE on the !oldState pattern below:
    // SendInput's keyup is processed asynchronously by the foreground window's
    // thread; the global Caps/Num/Scroll toggle flag updates when THAT thread
    // processes the keyup message. Our tray thread's GetKeyState reads its
    // own cached view, which lags. If we re-read IsKeyToggled right after
    // ToggleKey, we often get the PRE-toggle value — making the icon, OSD,
    // and beep frequency disagree with reality. Instead, capture state first
    // and compute the known-new-state deterministically. The LL keyboard
    // hook will fire a reconciling SyncIcons shortly after as a safety net.
    private void ToggleLockKey(byte vk, uint iconId, string tipOn, string tipOff, nint iconOnHandle, nint iconOffHandle, bool showEnabled, uint freqOn, uint freqOff)
    {
        // Open the in-flight suppression window BEFORE SendInput so the hook
        // that fires on the synthetic keyup can see it. 250 ms is generous;
        // GetKeyState typically reconciles within tens of ms.
        _toggleInFlightUntilTicks = Environment.TickCount64 + 250;

        bool oldState = IsKeyToggled(vk);
        ToggleKey(vk);
        bool newState = !oldState;

        // Drive icon + cached state from the KNOWN new state, not a
        // re-read of GetKeyState. Bypasses the SendInput race.
        _statesInitialized = true;
        switch (vk)
        {
            case NativeMethods.VK_CAPITAL:  _lastCapsState   = newState; break;
            case NativeMethods.VK_NUMLOCK:  _lastNumState    = newState; break;
            case NativeMethods.VK_SCROLL:   _lastScrollState = newState; break;
        }
        if (showEnabled)
            TrayModify(iconId, newState ? iconOnHandle : iconOffHandle, newState ? tipOn : tipOff);

        if (_config.BeepOnToggle)
            BeepAsync(newState ? freqOn : freqOff);
        if (_config.ShowOSD)
            OsdForm.ShowOsd(newState ? tipOn : tipOff);
    }

    private void ToggleCapsLock() =>
        ToggleLockKey(NativeMethods.VK_CAPITAL, ID_CAPS, CapsOn, CapsOff,
            _icons.CapsOn, _icons.CapsOff, _config.ShowCaps, 880u, 440u);

    private void ToggleNumLock() =>
        ToggleLockKey(NativeMethods.VK_NUMLOCK, ID_NUM, NumOn, NumOff,
            _icons.NumOn, _icons.NumOff, _config.ShowNum, 1000u, 500u);

    private void ToggleScrollLock() =>
        ToggleLockKey(NativeMethods.VK_SCROLL, ID_SCROLL, ScrollOn, ScrollOff,
            _icons.ScrollOn, _icons.ScrollOff, _config.ShowScroll, 1100u, 550u);

    private static void BeepAsync(uint freq) =>
        Task.Run(() => NativeMethods.Beep(freq, 80));

    // ── Icon Visibility ────────────────────────────────────────────────────

    private void SetIconVisible(uint id, bool visible)
    {
        if (!visible)
        {
            int count = (_config.ShowCaps ? 1 : 0) + (_config.ShowNum ? 1 : 0) + (_config.ShowScroll ? 1 : 0);
            if (count <= 1)
            {
                OsdForm.ShowOsd("At least one icon must remain visible", 3000);
                return;
            }
        }

        switch (id)
        {
            case ID_CAPS: _config.ShowCaps = visible; break;
            case ID_NUM: _config.ShowNum = visible; break;
            case ID_SCROLL: _config.ShowScroll = visible; break;
        }
        _config.Save();

        if (visible)
        {
            // Capture baseline BEFORE NIM_ADD so the promoter can identify
            // the newly-created subkey on Win11, same as cold-boot path.
            // Without this, toggling Scroll Lock (or any previously-hidden
            // icon) ON would register the icon but leave it in Win11's
            // overflow flyout — defeating the auto-show behavior.
            var toggleBaseline = TrayIconPromoter.CaptureBaseline();
            TrayAdd(id);
            SyncIcons(force: true);
            StartTrayIconPromotion(toggleBaseline);
        }
        else
        {
            TrayRemove(id);
        }
    }

    // ── WndProc — tray messages & TaskbarCreated ───────────────────────────

    protected override void WndProc(ref Message m)
    {
        if ((uint)m.Msg == _wmTaskbarCreated)
        {
            // Explorer restarted — re-add all visible icons.
            // Recapture a fresh baseline so the promoter can recover
            // visibility if the per-icon subkey was externally cleaned
            // up while we were running (e.g. Settings UI "Remove" entry).
            var recoveryBaseline = TrayIconPromoter.CaptureBaseline();
            if (_config.ShowCaps) TrayAdd(ID_CAPS);
            if (_config.ShowNum) TrayAdd(ID_NUM);
            if (_config.ShowScroll) TrayAdd(ID_SCROLL);
            SyncIcons(force: true);
            StartTrayIconPromotion(recoveryBaseline);
            return;
        }

        if ((uint)m.Msg == NativeMethods.WM_TRAY)
        {
            // Use unchecked to safely truncate 64-bit LParam/WParam to 32-bit
            int lParam = unchecked((int)(long)m.LParam);
            int wParam = unchecked((int)(long)m.WParam);

            int eventId = lParam & 0xFFFF;
            uint iconId = (uint)((lParam >> 16) & 0xFFFF);

            int clickX = wParam & 0xFFFF;
            int clickY = (wParam >> 16) & 0xFFFF;
            if (clickX > 32767) clickX -= 65536;
            if (clickY > 32767) clickY -= 65536;

            if (eventId == NativeMethods.WM_LBUTTONUP)
            {
                switch (iconId)
                {
                    case ID_CAPS: ToggleCapsLock(); break;
                    case ID_NUM: ToggleNumLock(); break;
                    case ID_SCROLL: ToggleScrollLock(); break;
                }
                return;
            }

            if (eventId == NativeMethods.WM_CONTEXTMENU || eventId == NativeMethods.WM_RBUTTONUP)
            {
                ShowContextMenu(iconId, clickX, clickY);
                return;
            }
        }

        base.WndProc(ref m);
    }

    // ── Context Menu ───────────────────────────────────────────────────────

    private ContextMenuStrip? _contextMenu;

    private void ShowContextMenu(uint iconId, int x, int y)
    {
        // Dispose previous menu if still alive
        if (_contextMenu != null)
        {
            _contextMenu.Close();
            _contextMenu.Dispose();
            _contextMenu = null;
        }

        var menu = new ContextMenuStrip();
        menu.Renderer = _menuRenderer;
        // Breathing room around the outside of the menu — without this,
        // "Exit CapsNumTray" sits jammed against the bottom border.
        menu.Padding = new Padding(0, 2, 0, 4);

        // Auto-dispose when menu closes
        menu.Closed += (_, _) =>
        {
            // Post disposal to avoid disposing during the Closed event
            // Guard against ObjectDisposedException if form is shutting down
            if (_disposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(() =>
                {
                    if (_contextMenu == menu)
                    {
                        _contextMenu.Dispose();
                        _contextMenu = null;
                    }
                });
            }
            catch (ObjectDisposedException) { }
        };

        // Version header (disabled)
        var header = menu.Items.Add("CapsNumTray v" + Version);
        header.Enabled = false;
        menu.Items.Add(new ToolStripSeparator());

        // Top state line — per-icon
        switch (iconId)
        {
            case ID_CAPS:
                AddStateItem(menu, "Caps Lock is ", IsKeyToggled(NativeMethods.VK_CAPITAL),
                    (_, _) => ToggleCapsLock());
                break;
            case ID_NUM:
                AddStateItem(menu, "Num Lock is ", IsKeyToggled(NativeMethods.VK_NUMLOCK),
                    (_, _) => ToggleNumLock());
                break;
            case ID_SCROLL:
                AddStateItem(menu, "Scroll Lock is ", IsKeyToggled(NativeMethods.VK_SCROLL),
                    (_, _) => ToggleScrollLock());
                break;
        }

        // Shared Visibility submenu — same contents regardless of which icon was clicked
        menu.Items.Add(new ToolStripSeparator());
        var visMenu = (ToolStripMenuItem)menu.Items.Add("Visibility");
        AddCheckItem(visMenu, ID_CAPS,   "Caps Lock",   _config.ShowCaps);
        AddCheckItem(visMenu, ID_NUM,    "Num Lock",    _config.ShowNum);
        AddCheckItem(visMenu, ID_SCROLL, "Scroll Lock", _config.ShowScroll);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings...", null, (_, _) => ShowSettingsDialog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit CapsNumTray", null, (_, _) => ExitApplication());

        _contextMenu = menu;
        NativeMethods.SetForegroundWindow(Handle);
        menu.Show(x, y);
    }

    private void AddStateItem(ContextMenuStrip menu, string prefix, bool on, EventHandler handler)
    {
        string stateWord = on ? "ON" : "OFF";
        var item = menu.Items.Add(prefix + stateWord, null, handler);
        item.Tag = stateWord;                       // bold segment for the renderer
        item.Font = _menuRenderer.GetBold(menu.Font); // reserve bold width for AutoSize
    }

    private void AddCheckItem(ToolStripMenuItem parent, uint id, string label, bool visible)
    {
        var item = new ToolStripMenuItem(label) { Checked = visible };
        item.Click += (_, _) => SetIconVisible(id, !visible);
        parent.DropDownItems.Add(item);
    }

    // ── Settings Dialog ────────────────────────────────────────────────────

    private SettingsForm? _settingsForm;

    private void ShowSettingsDialog()
    {
        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            _settingsForm.BringToFront();
            return;
        }

        _settingsForm = new SettingsForm(_config, this);
        _settingsForm.FormClosed += (_, _) =>
        {
            _settingsForm = null; // Close() on Show()-ed form auto-disposes
        };
        _settingsForm.Show();
    }

    internal void ApplySettings(bool showCaps, bool showNum, bool showScroll,
        bool showOSD, bool beepOnToggle, bool runAtStartup, int pollInterval)
    {
        // Guard: at least one visible
        if (!showCaps && !showNum && !showScroll)
        {
            OsdForm.ShowOsd("At least one icon must remain visible", 3000);
            return;
        }

        // Show new icons before hiding old ones so the "at least one visible"
        // guard in SetIconVisible is never tripped mid-transition.
        if (!_config.ShowCaps && showCaps) SetIconVisible(ID_CAPS, true);
        if (!_config.ShowNum && showNum) SetIconVisible(ID_NUM, true);
        if (!_config.ShowScroll && showScroll) SetIconVisible(ID_SCROLL, true);
        if (_config.ShowCaps && !showCaps) SetIconVisible(ID_CAPS, false);
        if (_config.ShowNum && !showNum) SetIconVisible(ID_NUM, false);
        if (_config.ShowScroll && !showScroll) SetIconVisible(ID_SCROLL, false);

        _config.ShowOSD = showOSD;
        _config.BeepOnToggle = beepOnToggle;
        _config.PollInterval = pollInterval;
        _config.Save();

        ApplyPollInterval(pollInterval);

        StartupManager.SetEnabled(runAtStartup);

        OsdForm.ShowOsd("Settings saved.", 3000);
    }

    // ── Shell_NotifyIconW Wrappers ─────────────────────────────────────────

    // Wraps Shell_NotifyIconW with failure logging. NIM_MODIFY / NIM_DELETE on
    // a stale iconID (e.g. after an Explorer crash and before TaskbarCreated
    // fires) return false with E_FAIL silently — the kind of landmine that
    // hid the SendInput bug. Log at Trace level for future diagnosis.
    private static bool ShellNotify(uint msg, ref NativeMethods.NOTIFYICONDATAW nid, string op)
    {
        bool ok = NativeMethods.Shell_NotifyIconW(msg, ref nid);
        if (!ok)
            System.Diagnostics.Trace.WriteLine(
                $"CapsNumTray: Shell_NotifyIconW {op} id={nid.uID} failed (err=0x{Marshal.GetLastWin32Error():X8})");
        return ok;
    }

    private void TrayAdd(uint id)
    {
        // Win11 22H2+: NIM_ADD without NIF_TIP makes Explorer write a sparse
        // NotifyIconSettings subkey (IconSnapshot only, no ExecutablePath /
        // InitialTooltip / UID), which breaks the auto-promote helper's
        // Phase-1 path match. Seed a per-icon human-readable tooltip here
        // so Explorer writes the full schema; the state-driven tooltip lands
        // a moment later via NIM_MODIFY in SyncIcons and overwrites it.
        // The seed also becomes the label shown in Settings → Taskbar →
        // Other system tray icons.
        string seedTip = id switch
        {
            ID_CAPS => "Caps Lock",
            ID_NUM => "Num Lock",
            ID_SCROLL => "Scroll Lock",
            _ => "CapsNumTray",
        };

        var nid = new NativeMethods.NOTIFYICONDATAW
        {
            cbSize = NidSize,
            hWnd = Handle,
            uID = id,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_TIP,
            uCallbackMessage = NativeMethods.WM_TRAY,
            szTip = seedTip,
            szInfo = "",
            szInfoTitle = "",
        };

        if (ShellNotify(NativeMethods.NIM_ADD, ref nid, "NIM_ADD"))
        {
            // Upgrade to v4 protocol (required for the lParam encoding in WndProc).
            nid.uVersion = NativeMethods.NOTIFYICON_VERSION_4;
            ShellNotify(NativeMethods.NIM_SETVERSION, ref nid, "NIM_SETVERSION");
        }
    }

    // ── Win11 tray icon auto-promote ───────────────────────────────────────

    /// <summary>
    /// Poll TrayIconPromoter until it has identified all of our per-icon
    /// NotifyIconSettings subkeys — 500 ms × 20 attempts = 10 s cap.
    /// Explorer writes each subkey asynchronously after NIM_ADD; under
    /// Windows-login load it can take a few seconds. Unlike single-icon
    /// apps, we don't stop on first identification: with three icons it's
    /// possible for the first tick to catch one Phase-1 match while the
    /// other two are still orphans, which would block Phase-2 commit. Run
    /// the timer to its full cap so later ticks catch the late subkeys.
    /// TryPromote is idempotent — already-promoted subkeys are no-ops.
    /// On Win10 the helper returns false immediately; the loop then just
    /// ticks harmlessly to exhaustion.
    /// </summary>
    private void StartTrayIconPromotion(HashSet<string>? baseline)
    {
        var promoteTimer = new System.Windows.Forms.Timer { Interval = 500 };
        int attempts = 0;
        const int maxAttempts = 20;
        promoteTimer.Tick += (_, _) =>
        {
            attempts++;
            TrayIconPromoter.TryPromote(Environment.ProcessPath ?? "", baseline);
            if (attempts >= maxAttempts || _disposed)
            {
                promoteTimer.Stop();
                promoteTimer.Dispose();
            }
        };
        promoteTimer.Start();
    }

    private void TrayModify(uint id, nint hIcon, string tip)
    {
        var nid = new NativeMethods.NOTIFYICONDATAW
        {
            cbSize = NidSize,
            hWnd = Handle,
            uID = id,
            uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_SHOWTIP,
            uCallbackMessage = NativeMethods.WM_TRAY,
            hIcon = hIcon,
            szTip = tip,
            szInfo = "",
            szInfoTitle = "",
        };
        ShellNotify(NativeMethods.NIM_MODIFY, ref nid, "NIM_MODIFY");
    }

    private void TrayRemove(uint id)
    {
        var nid = new NativeMethods.NOTIFYICONDATAW
        {
            cbSize = NidSize,
            hWnd = Handle,
            uID = id,
            szTip = "",
            szInfo = "",
            szInfoTitle = "",
        };
        ShellNotify(NativeMethods.NIM_DELETE, ref nid, "NIM_DELETE");
    }

    // ── Exit & Cleanup ─────────────────────────────────────────────────────

    private void ExitApplication()
    {
        Cleanup();
        Application.Exit();
    }

    private void Cleanup()
    {
        if (_cleanedUp) return;
        _cleanedUp = true;

        _syncTimer.Stop();

        // Unsubscribe from SystemEvents — these hold a strong ref to our
        // handlers and would keep the form alive past disposal otherwise.
        Microsoft.Win32.SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }

        // Remove all tray icons (NIM_DELETE is idempotent)
        TrayRemove(ID_CAPS);
        TrayRemove(ID_NUM);
        TrayRemove(ID_SCROLL);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;
            Cleanup();

            _syncTimer.Dispose();
            _contextMenu?.Dispose();
            _settingsForm?.Dispose();
            _icons.Dispose();
            _menuRenderer.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Custom context-menu renderer that bolds one substring inside an item's text.
// Item.Tag carries the substring to bold; items without a Tag use base rendering.
internal sealed class BoldSegmentRenderer : ToolStripSystemRenderer, IDisposable
{
    private Font? _bold;
    private Font? _boldBase;

    public Font GetBold(System.Drawing.Font baseFont)
    {
        if (_bold == null || !ReferenceEquals(_boldBase, baseFont))
        {
            _bold?.Dispose();
            _boldBase = baseFont;
            _bold = new Font(baseFont, FontStyle.Bold);
        }
        return _bold;
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        if (e.Item.Tag is not string boldWord || boldWord.Length == 0)
        {
            base.OnRenderItemText(e);
            return;
        }
        string text = e.Text ?? "";
        int idx = text.IndexOf(boldWord, StringComparison.Ordinal);
        if (idx < 0)
        {
            base.OnRenderItemText(e);
            return;
        }

        Font regular = e.ToolStrip!.Font;
        Font bold = GetBold(regular);
        Color color = e.TextColor;
        TextFormatFlags flags =
            TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.NoPadding;

        Rectangle bounds = e.TextRectangle;
        string pre = text.Substring(0, idx);
        string mid = text.Substring(idx, boldWord.Length);
        string post = text.Substring(idx + boldWord.Length);

        int preWidth = pre.Length > 0
            ? TextRenderer.MeasureText(e.Graphics, pre, regular, bounds.Size, flags).Width
            : 0;
        int midWidth = TextRenderer.MeasureText(e.Graphics, mid, bold, bounds.Size, flags).Width;

        if (pre.Length > 0)
            TextRenderer.DrawText(e.Graphics, pre, regular, bounds, color, flags);
        var midRect = new Rectangle(bounds.X + preWidth, bounds.Y, bounds.Width - preWidth, bounds.Height);
        TextRenderer.DrawText(e.Graphics, mid, bold, midRect, color, flags);
        if (post.Length > 0)
        {
            var postRect = new Rectangle(midRect.X + midWidth, bounds.Y,
                bounds.Width - preWidth - midWidth, bounds.Height);
            TextRenderer.DrawText(e.Graphics, post, regular, postRect, color, flags);
        }
    }

    public void Dispose()
    {
        _bold?.Dispose();
        _bold = null;
        _boldBase = null;
    }
}
