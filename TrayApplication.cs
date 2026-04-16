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

    private bool _disposed;
    private bool _cleanedUp;
    private bool _syncing;

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

        // Register TaskbarCreated message
        _wmTaskbarCreated = NativeMethods.RegisterWindowMessage("TaskbarCreated");

        // Add visible tray icons
        if (_config.ShowCaps) TrayAdd(ID_CAPS);
        if (_config.ShowNum) TrayAdd(ID_NUM);
        if (_config.ShowScroll) TrayAdd(ID_SCROLL);

        // Initial sync
        SyncIcons(force: true);

        // Low-level keyboard hook for instant toggle detection
        _hookProc = KeyboardHookCallback;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL, _hookProc,
            NativeMethods.GetModuleHandle(null), 0);

        // Polling timer — safety net for external key state changes (RDP, other apps).
        // The keyboard hook handles normal keystrokes instantly.
        _syncTimer = new System.Windows.Forms.Timer();
        ApplyPollInterval(_config.PollInterval);
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
        NativeMethods.SendInput(2, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    // ── Sync Icons ─────────────────────────────────────────────────────────

    private void SyncIcons(bool force = false)
    {
        // Guard against re-entrancy: Shell_NotifyIconW can pump the message
        // queue, which may dispatch another timer tick or a WndProc that
        // calls SyncIcons again before the previous call completes.
        if (_syncing) return;
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

    private void ToggleCapsLock()
    {
        ToggleKey(NativeMethods.VK_CAPITAL);
        bool newState = IsKeyToggled(NativeMethods.VK_CAPITAL);
        SyncIcons(force: true);
        if (_config.BeepOnToggle)
            BeepAsync(newState ? 880u : 440u);
        if (_config.ShowOSD)
            OsdForm.ShowOsd(newState ? CapsOn : CapsOff);
    }

    private void ToggleNumLock()
    {
        ToggleKey(NativeMethods.VK_NUMLOCK);
        bool newState = IsKeyToggled(NativeMethods.VK_NUMLOCK);
        SyncIcons(force: true);
        if (_config.BeepOnToggle)
            BeepAsync(newState ? 1000u : 500u);
        if (_config.ShowOSD)
            OsdForm.ShowOsd(newState ? NumOn : NumOff);
    }

    private void ToggleScrollLock()
    {
        ToggleKey(NativeMethods.VK_SCROLL);
        bool newState = IsKeyToggled(NativeMethods.VK_SCROLL);
        SyncIcons(force: true);
        if (_config.BeepOnToggle)
            BeepAsync(newState ? 1100u : 550u);
        if (_config.ShowOSD)
            OsdForm.ShowOsd(newState ? ScrollOn : ScrollOff);
    }

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
            TrayAdd(id);
            SyncIcons(force: true);
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
            // Explorer restarted — re-add all visible icons
            if (_config.ShowCaps) TrayAdd(ID_CAPS);
            if (_config.ShowNum) TrayAdd(ID_NUM);
            if (_config.ShowScroll) TrayAdd(ID_SCROLL);
            SyncIcons(force: true);
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
        menu.RenderMode = ToolStripRenderMode.System;

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

        // Toggle item for clicked icon
        switch (iconId)
        {
            case ID_CAPS:
            {
                bool on = IsKeyToggled(NativeMethods.VK_CAPITAL);
                menu.Items.Add(
                    "Caps Lock is " + (on ? "ON  \u2014 click to turn Off" : "OFF \u2014 click to turn On"),
                    null, (_, _) => ToggleCapsLock());
                menu.Items.Add(new ToolStripSeparator());
                AddVisibilityItem(menu, ID_NUM, "Num Lock", _config.ShowNum);
                AddVisibilityItem(menu, ID_SCROLL, "Scroll Lock", _config.ShowScroll);
                break;
            }
            case ID_NUM:
            {
                bool on = IsKeyToggled(NativeMethods.VK_NUMLOCK);
                menu.Items.Add(
                    "Num Lock is " + (on ? "ON  \u2014 click to turn Off" : "OFF \u2014 click to turn On"),
                    null, (_, _) => ToggleNumLock());
                menu.Items.Add(new ToolStripSeparator());
                AddVisibilityItem(menu, ID_CAPS, "Caps Lock", _config.ShowCaps);
                AddVisibilityItem(menu, ID_SCROLL, "Scroll Lock", _config.ShowScroll);
                break;
            }
            case ID_SCROLL:
            {
                bool on = IsKeyToggled(NativeMethods.VK_SCROLL);
                menu.Items.Add(
                    "Scroll Lock is " + (on ? "ON  \u2014 click to turn Off" : "OFF \u2014 click to turn On"),
                    null, (_, _) => ToggleScrollLock());
                menu.Items.Add(new ToolStripSeparator());
                AddVisibilityItem(menu, ID_CAPS, "Caps Lock", _config.ShowCaps);
                AddVisibilityItem(menu, ID_NUM, "Num Lock", _config.ShowNum);
                break;
            }
        }

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings...", null, (_, _) => ShowSettingsDialog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit CapsNumTray", null, (_, _) => ExitApplication());

        _contextMenu = menu;
        NativeMethods.SetForegroundWindow(Handle);
        menu.Show(x, y);
    }

    private void AddVisibilityItem(ContextMenuStrip menu, uint id, string name, bool currentlyVisible)
    {
        string label = (currentlyVisible ? "Hide " : "Show ") + name + " icon";
        menu.Items.Add(label, null, (_, _) => SetIconVisible(id, !currentlyVisible));
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

    private void TrayAdd(uint id)
    {
        var nid = new NativeMethods.NOTIFYICONDATAW
        {
            cbSize = NidSize,
            hWnd = Handle,
            uID = id,
            uFlags = NativeMethods.NIF_MESSAGE,
            uCallbackMessage = NativeMethods.WM_TRAY,
            szTip = "",
            szInfo = "",
            szInfoTitle = "",
        };

        if (NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_ADD, ref nid))
        {
            // Set NOTIFYICON_VERSION_4
            nid.uVersion = NativeMethods.NOTIFYICON_VERSION_4;
            NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_SETVERSION, ref nid);
        }
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
        NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_MODIFY, ref nid);
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
        NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_DELETE, ref nid);
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
        }
        base.Dispose(disposing);
    }
}
