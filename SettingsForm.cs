namespace CapsNumTray;

/// <summary>
/// Settings dialog matching the AHK GUI: visibility checkboxes, feedback options,
/// startup toggle, and OK/Apply/Cancel buttons.
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly ConfigManager _config;
    private readonly TrayApplication _app;

    private readonly CheckBox _chkCaps;
    private readonly CheckBox _chkNum;
    private readonly CheckBox _chkScroll;
    private readonly CheckBox _chkOSD;
    private readonly CheckBox _chkBeep;
    private readonly CheckBox _chkStartup;
    private readonly NumericUpDown _nudPollInterval;
    private readonly System.Drawing.Font _formFont;
    private readonly System.Drawing.Font _boldFont;

    public SettingsForm(ConfigManager config, TrayApplication app)
    {
        _config = config;
        _app = app;

        // Three first-show lag mitigations applied here, top-to-bottom:
        //
        //   1. OptimizedDoubleBuffer + AllPaintingInWmPaint + UserPaint
        //      Eliminates the per-child-control paint flicker. Without this,
        //      each Controls.Add below paints immediately on a CPU-side surface;
        //      with it, the whole form paints once into an off-screen buffer
        //      then blits in a single GDI BitBlt \u2014 much less visible "settling."
        //
        //   2. SuspendLayout() bracketing the constructor
        //      Each Controls.Add triggers a layout pass on the parent form.
        //      We add ~20 controls (6 checkboxes, 4 section labels, 1 NUD,
        //      1 helper label, 6 buttons, 2 NUD-companion labels) so without
        //      SuspendLayout this is ~20 layout passes for nothing \u2014 ResumeLayout
        //      (true) collapses them into one final layout at the end.
        //
        //   3. DWMWA_USE_IMMERSIVE_DARK_MODE in OnHandleCreated (below)
        //      The biggest perceived-lag fix. Without it the OS shows a default
        //      LIGHT titlebar attached to a dark body for a frame or two before
        //      DWM repaints, which reads as the form "popping in then settling."
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true);
        SuspendLayout();

        Text = "CapsNumTray v" + TrayApplication.Version + " \u2014 Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        // Hide from the taskbar \u2014 Settings is an auxiliary dialog reached via
        // tray right-click, not a top-level workspace window. Bonus: Windows
        // skips the taskbar-button registration step (which is what was causing
        // the visible "pop in" delay), so the form appears noticeably faster.
        ShowInTaskbar = false;
        BackColor = Theme.BgColor;
        ForeColor = Theme.FgColor;
        StartPosition = FormStartPosition.CenterScreen;
        // Pin design baseline to 96 DPI BEFORE setting AutoScaleMode so every
        // literal Size/Point/Location below is interpreted as 96-DPI design
        // pixels regardless of which monitor first realizes this form.
        // Without this, the form gets double-scaled on 125%/150% laptops and
        // button bottom borders / NumericUpDown digits clip.
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        _formFont = new System.Drawing.Font("Segoe UI", 9f);
        Font = _formFont;
        _boldFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

        const int PollInputX = 320;

        int yLeft = 16;
        int yRight = 16;

        // ── Tray Icons (left column, top) ──
        var lblIcons = new Label { Text = "Tray Icons", Location = new(16, yLeft), AutoSize = true, Font = _boldFont, ForeColor = Theme.DimColor };
        Controls.Add(lblIcons);
        yLeft += 26;

        _chkCaps = AddCheckBox("Show Caps Lock icon", config.ShowCaps, 28, ref yLeft);
        _chkNum = AddCheckBox("Show Num Lock icon", config.ShowNum, 28, ref yLeft);
        _chkScroll = AddCheckBox("Show Scroll Lock icon", config.ShowScroll, 28, ref yLeft);

        // ── Startup (right column, top) ──
        const string startupText = "Run at Windows startup";
        const int StartupHdrX = 210;

        var lblStartup = new Label { Text = "Startup", Location = new(StartupHdrX, yRight), AutoSize = true, Font = _boldFont, ForeColor = Theme.DimColor };
        Controls.Add(lblStartup);
        yRight += 26;

        _chkStartup = AddCheckBox(startupText, StartupManager.IsEnabled, StartupHdrX + 12, ref yRight);

        int y = Math.Max(yLeft, yRight);

        // ── Feedback ──
        y += 10;
        var lblFeedback = new Label { Text = "Feedback", Location = new(16, y), AutoSize = true, Font = _boldFont, ForeColor = Theme.DimColor };
        Controls.Add(lblFeedback);
        y += 26;

        _chkOSD = AddCheckBox("Show OSD tooltip on toggle", config.ShowOSD, 28, ref y);
        _chkBeep = AddCheckBox("Beep on toggle", config.BeepOnToggle, 28, ref y);

        // ── Polling ──
        y += 10;
        var lblPolling = new Label { Text = "Polling", Location = new(16, y), AutoSize = true, Font = _boldFont, ForeColor = Theme.DimColor };
        Controls.Add(lblPolling);
        y += 26;

        var lblPollDesc = new Label { Text = "Fallback poll interval (seconds, 0 = disabled):", Location = new(28, y + 2), AutoSize = true, ForeColor = Theme.FgColor };
        Controls.Add(lblPollDesc);
        _nudPollInterval = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 300,
            Value = config.PollInterval,
            Location = new(PollInputX, y),
            // Width 80 (not 60) — NumericUpDown's spinner band composes three
            // nested HWNDs whose scaling diverges by a few px at every non-integer
            // ratio; at 125% the spinner eats ~25px which collides with digits
            // in a 60px-wide control. MinimumSize floor prevents AutoScale from
            // shrinking the spinner back into the digit area at any scale factor.
            Size = new(80, 26),
            MinimumSize = new(80, 26),
            Increment = 5,
            ForeColor = Theme.FgColor,
            BackColor = Theme.EditBgColor,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Left,
        };
        Controls.Add(_nudPollInterval);
        // NumericUpDown is composed of two child controls: the inner text box
        // (Controls[1]) and the spinner band (Controls[0]) — the spinner is an
        // internal UpDownButtons HWND that paints its own background via
        // ControlPaint and ignores its parent's BackColor. Without this assign
        // the digit area is dark but the up/down arrow strip beside it is
        // system-grey (visible split). Setting Controls[0].BackColor matches
        // the band to the digit area; the arrow glyphs themselves stay
        // system-rendered but read fine against the dark band.
        if (_nudPollInterval.Controls.Count > 0)
        {
            _nudPollInterval.Controls[0].BackColor = Theme.EditBgColor;
            _nudPollInterval.Controls[0].ForeColor = Theme.FgColor;
        }
        y += 28;

        // ── Buttons (two rows, each row fills horizontally with equal-width buttons) ──
        y += 16;

        const int FormWidth = 480;
        // Equal padding: border|pad|btn|pad|btn|pad|btn|pad|border
        // 4 * pad + 3 * btnW = FormWidth. With pad=12, btnW=144 → exactly 480.
        const int BtnPad = 12;
        const int BtnW = (FormWidth - 4 * BtnPad) / 3; // = 144

        // Primary (bottom) row: full 144×28. Utility (top) row: smaller 120×26 for visual contrast.
        // (TopBtnH was 24 — bottom border clipped at 125%+ scale; floor is 26 for 9pt Segoe UI per
        // _.claude/_templates/snippets/csharp/winforms-dpi-scaling.md §6. Contrast preserved via
        // the 24px width differential, not height.)
        const int BotBtnH = 28;
        const int TopBtnW = 120;
        const int TopBtnH = 26;
        const int TopBtnOffset = (BtnW - TopBtnW) / 2; // center each small button in its column slot

        int col1X = BtnPad;
        int col2X = col1X + BtnW + BtnPad;
        int col3X = col2X + BtnW + BtnPad;

        // Row 1 (top): utility — GitHub, Update, Help (smaller, centered in column slots)
        int gitHubX = col1X + TopBtnOffset;
        int updateX = col2X + TopBtnOffset;
        int helpX   = col3X + TopBtnOffset;

        var btnGitHub = new Button { Text = "GitHub", Location = new(gitHubX, y), Size = new(TopBtnW, TopBtnH) };
        ThemeButton(btnGitHub);
        btnGitHub.Click += (_, _) =>
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/itsnateai/CaplockNumlock",
                UseShellExecute = true
            });
        };
        Controls.Add(btnGitHub);

        var btnUpdate = new Button { Text = "Update", Location = new(updateX, y), Size = new(TopBtnW, TopBtnH) };
        ThemeButton(btnUpdate);
        btnUpdate.Click += (_, _) =>
        {
            using var dlg = new UpdateDialog();
            dlg.ShowDialog(this);
        };
        Controls.Add(btnUpdate);

        var btnHelp = new Button { Text = "Help", Location = new(helpX, y), Size = new(TopBtnW, TopBtnH) };
        ThemeButton(btnHelp);
        btnHelp.Click += (_, _) => ShowHelpWindow();
        Controls.Add(btnHelp);

        // Row 2 (bottom): actions — OK, Apply, Cancel (full-size, primary)
        y += TopBtnH + BtnPad;

        int okX     = col1X;
        int applyX  = col2X;
        int cancelX = col3X;

        var btnOK = new Button { Text = "OK", Location = new(okX, y), Size = new(BtnW, BotBtnH) };
        ThemeButton(btnOK);
        btnOK.Click += (_, _) => { Apply(); Close(); };
        Controls.Add(btnOK);
        AcceptButton = btnOK;

        var btnApply = new Button { Text = "Apply", Location = new(applyX, y), Size = new(BtnW, BotBtnH) };
        ThemeButton(btnApply);
        btnApply.Click += (_, _) => Apply();
        Controls.Add(btnApply);

        var btnCancel = new Button { Text = "Cancel", Location = new(cancelX, y), Size = new(BtnW, BotBtnH) };
        ThemeButton(btnCancel);
        btnCancel.Click += (_, _) => Close();
        Controls.Add(btnCancel);
        CancelButton = btnCancel;

        ClientSize = new System.Drawing.Size(FormWidth, y + BotBtnH + 16);

        // ResumeLayout(true) — single layout pass for the whole tree instead of
        // ~20 incremental ones. Must come AFTER ClientSize so the final layout
        // sees the right form bounds.
        ResumeLayout(performLayout: true);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Flip the titlebar to dark BEFORE the window becomes visible (handle
        // creation precedes the first WM_NCPAINT). Try the modern attribute
        // first (20, Win10 20H1+ and Win11). On Win10 1809–19H2 attribute 20
        // returns S_OK with no visible effect, so we also send the legacy
        // attribute 19 unconditionally — DWM silently ignores it on builds
        // where it isn't recognized, and on builds that need it the dark
        // titlebar appears. On pre-1809 Win10 both calls fail silently and
        // the form keeps its default light titlebar — no functional impact.
        int dark = 1;
        NativeMethods.DwmSetWindowAttribute(
            Handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref dark,
            sizeof(int));
        NativeMethods.DwmSetWindowAttribute(
            Handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
            ref dark,
            sizeof(int));
    }

    private CheckBox AddCheckBox(string text, bool isChecked, int x, ref int y)
    {
        var chk = new CheckBox
        {
            Text = text,
            Checked = isChecked,
            Location = new(x, y),
            AutoSize = true,
            ForeColor = Theme.FgColor,
            BackColor = Theme.BgColor,
            // FlatStyle.Flat switches the CheckBox to a render path that
            // respects ForeColor for the tick glyph. The default
            // FlatStyle.Standard uses Application.RenderWithVisualStyles which
            // paints a light-themed glyph regardless of our ForeColor, and
            // draws the focus rect via ControlPaint.DrawFocusRectangle which
            // XORs against SystemColors.ControlText (near-invisible on our
            // dark BG). Flat draws both in our themed colors.
            FlatStyle = FlatStyle.Flat,
        };
        chk.FlatAppearance.BorderColor = Theme.DividerColor;
        chk.FlatAppearance.CheckedBackColor = Theme.HighlightBg;
        chk.FlatAppearance.MouseOverBackColor = Theme.HighlightBg;
        Controls.Add(chk);
        y += 24;
        return chk;
    }

    private static void ThemeButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.ForeColor = Theme.FgColor;
        btn.BackColor = Theme.BgColor;
        btn.FlatAppearance.BorderColor = Theme.DividerColor;
    }

    private void Apply()
    {
        _app.ApplySettings(
            _chkCaps.Checked, _chkNum.Checked, _chkScroll.Checked,
            _chkOSD.Checked, _chkBeep.Checked, _chkStartup.Checked,
            (int)_nudPollInterval.Value);
    }

    private HelpForm? _helpForm;

    private void ShowHelpWindow()
    {
        if (_helpForm != null && !_helpForm.IsDisposed)
        {
            _helpForm.BringToFront();
            return;
        }
        _helpForm = new HelpForm();
        _helpForm.FormClosed += (_, _) =>
        {
            _helpForm = null; // Close() on Show()-ed form auto-disposes
        };
        _helpForm.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Help window owns its own lifecycle once Show()-ed — do not dispose it here.
            _boldFont.Dispose();
            _formFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
