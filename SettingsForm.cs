namespace CapsNumTray;

/// <summary>
/// Settings dialog: visibility checkboxes, feedback options, startup toggle,
/// theme selector, fallback poll interval, and utility/action button rows.
///
/// Layout is built entirely from relational containers (TableLayoutPanel /
/// FlowLayoutPanel + AutoSize), NOT absolute coordinates. There are no pixel
/// literals for the framework to mis-scale, so the form is correct by
/// construction at 100%, 125%, 150%, 175% — any DPI. (AutoScaleMode.Dpi grows
/// fonts and the frame; relational layout is what keeps the controls in step.)
/// PerMonitorV2 (set via the manifest / csproj) is correct for this standalone
/// app — do NOT switch to SystemAware (that's an EQSwitch-only carve-out for
/// its injected child windows).
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly ConfigManager _config;
    // Nullable only to support the DEBUG render harness, which builds the form
    // standalone to capture its layout. In every production path SettingsForm is
    // constructed by TrayApplication with `this`, so _app is never null when a
    // user can reach Apply().
    private readonly TrayApplication? _app;

    private readonly CheckBox _chkCaps;
    private readonly CheckBox _chkNum;
    private readonly CheckBox _chkScroll;
    private readonly CheckBox _chkOSD;
    private readonly CheckBox _chkBeep;
    private readonly CheckBox _chkStartup;
    private readonly NumericUpDown _nudPollInterval;
    private readonly ComboBox _cboTheme;
    private readonly System.Drawing.Font _formFont;
    private readonly System.Drawing.Font _boldFont;
    // Root container, kept so OnLoad can size the window to the REALIZED
    // (post-DPI-scale) content — measuring in the ctor happens at 96 DPI and
    // under-provisions the height at 150%.
    private readonly TableLayoutPanel _root;

    public SettingsForm(ConfigManager config, TrayApplication? app)
    {
        _config = config;
        _app = app;

        // First-show lag mitigations (unchanged from the absolute-layout version):
        //   1. OptimizedDoubleBuffer + AllPaintingInWmPaint + UserPaint — paint
        //      the whole tree once into an off-screen buffer, then blit, instead
        //      of per-child paint flicker.
        //   2. SuspendLayout/ResumeLayout — collapse the ~6 container adds into a
        //      single final layout pass.
        //   3. DWMWA_USE_IMMERSIVE_DARK_MODE in OnHandleCreated — dark titlebar
        //      before first WM_NCPAINT so the frame doesn't flash light.
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint,
            true);
        SuspendLayout();

        Text = "CapsNumTray v" + TrayApplication.Version + " — Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        // Hide from the taskbar — Settings is an auxiliary dialog reached via the
        // tray right-click, and skipping the taskbar-button registration makes it
        // appear noticeably faster.
        ShowInTaskbar = false;
        BackColor = Theme.BgColor;
        ForeColor = Theme.FgColor;
        StartPosition = FormStartPosition.CenterScreen;
        // Pin the design baseline to 96 DPI BEFORE setting AutoScaleMode so the
        // form scales uniformly from 96 regardless of which monitor first
        // realizes it (no double-scale on a 125%/150% laptop).
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        _formFont = new System.Drawing.Font("Segoe UI", 9f);
        Font = _formFont;
        _boldFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

        // ── Controls ──────────────────────────────────────────────────────
        _chkCaps    = MakeCheckBox("Show Caps Lock icon", config.ShowCaps);
        _chkNum     = MakeCheckBox("Show Num Lock icon", config.ShowNum);
        _chkScroll  = MakeCheckBox("Show Scroll Lock icon", config.ShowScroll);
        _chkStartup = MakeCheckBox("Run at Windows startup", StartupManager.IsEnabled);
        _chkOSD     = MakeCheckBox("Show OSD tooltip on toggle", config.ShowOSD);
        _chkBeep    = MakeCheckBox("Beep on toggle", config.BeepOnToggle);

        var lblTheme = new Label
        {
            Text = "Theme:",
            AutoSize = true,
            ForeColor = Theme.FgColor,
            // Indent to line up under the "Run at Windows startup" checkbox; top
            // margin nudges the caption onto the combo's text baseline.
            Margin = new Padding(16, 6, 4, 2),
        };
        _cboTheme = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            ForeColor = Theme.FgColor,
            BackColor = Theme.EditBgColor,
            FlatStyle = FlatStyle.Flat,
            // Fits the longest item ("System") + the dropdown arrow at 96 DPI;
            // AutoScaleMode.Dpi widens it proportionally at higher scales.
            Width = 92,
            Margin = new Padding(0, 3, 4, 2),
        };
        _cboTheme.Items.AddRange(new object[] { "System", "Dark", "Light" });
        int themeIdx = _cboTheme.Items.IndexOf(config.ThemeMode);
        _cboTheme.SelectedIndex = themeIdx >= 0 ? themeIdx : 0; // unknown -> System

        var lblPollDesc = new Label
        {
            Text = "Fallback poll interval (seconds, 0 = disabled):",
            AutoSize = true,
            ForeColor = Theme.FgColor,
            Margin = new Padding(16, 6, 4, 2),
        };
        _nudPollInterval = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 300,
            Value = config.PollInterval,
            Increment = 5,
            // Width sized for the 3-digit maximum ("300") + the spinner band,
            // plus a small margin for the few-px scaling divergence of the NUD's
            // nested HWNDs at non-integer DPI. Content-appropriate, not a blanket
            // multiplier; AutoScaleMode.Dpi scales it from this 96-DPI baseline.
            Width = 64,
            MinimumSize = new System.Drawing.Size(64, 0),
            ForeColor = Theme.FgColor,
            BackColor = Theme.EditBgColor,
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Left,
            Margin = new Padding(8, 1, 4, 2),
        };
        // The spinner band (Controls[0]) is an internal UpDownButtons HWND that
        // paints its own background and ignores the parent BackColor; tint it to
        // match the digit area so there's no light/dark split.
        if (_nudPollInterval.Controls.Count > 0)
        {
            _nudPollInterval.Controls[0].BackColor = Theme.EditBgColor;
            _nudPollInterval.Controls[0].ForeColor = Theme.FgColor;
        }

        var btnGitHub = MakeButton("GitHub", (_, _) =>
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/itsnateai/CaplockNumlock",
                UseShellExecute = true
            });
        });
        var btnUpdate = MakeButton("Update", (_, _) =>
        {
            using var dlg = new UpdateDialog();
            dlg.ShowDialog(this);
        });
        var btnHelp   = MakeButton("Help", (_, _) => ShowHelpWindow());
        var btnOK     = MakeButton("OK", (_, _) => { Apply(); Close(); });
        var btnApply  = MakeButton("Apply", (_, _) => Apply());
        var btnCancel = MakeButton("Cancel", (_, _) => Close());

        // ── Compose with layout containers ────────────────────────────────
        // Top: two equal columns — Tray Icons (left) | Startup + Theme (right).
        // Anchored Left|Right so the 50/50 split spans the full content width.
        var topRow = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.BgColor,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        topRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        topRow.Controls.Add(Column(SectionLabel("Tray Icons"), _chkCaps, _chkNum, _chkScroll), 0, 0);
        topRow.Controls.Add(Column(SectionLabel("Startup"), _chkStartup, Row(lblTheme, _cboTheme)), 1, 0);

        var feedback   = Column(SectionLabel("Feedback"), _chkOSD, _chkBeep);
        var polling    = Column(SectionLabel("Polling"), Row(lblPollDesc, _nudPollInterval));
        var utilityRow = ButtonRow(btnGitHub, btnUpdate, btnHelp);
        var actionRow  = ButtonRow(btnOK, btnApply, btnCancel);

        // Single-column stack. Explicit AutoSize column + per-row AutoSize styles
        // so each row sizes to its content (a TableLayoutPanel with fewer styles
        // than rows can otherwise leave later rows un-sized and clip them).
        _root = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.BgColor,
            Padding = new Padding(12),
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        foreach (Control r in new Control[] { topRow, feedback, polling, utilityRow, actionRow })
        {
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(r);
        }

        Controls.Add(_root);
        AcceptButton = btnOK;
        CancelButton = btnCancel;

        ResumeLayout(performLayout: true);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Size the client area to the realized (post-DPI-scale) content. _root is an
        // AutoSize TableLayoutPanel at (0,0) with a symmetric Padding(12), so its
        // PreferredSize is the exact content box; using it yields an even 12px margin
        // on all four sides with no magic height constant, correct by construction at
        // any DPI (the margin scales with the content).
        //
        // Live measurement (CopyFromScreen, host 100%): _root.PreferredSize.Height ==
        // _root.Height == the true rendered content — a centre-column pixel probe shows
        // the OK/Apply/Cancel row ending well inside it, then pure background. The prior
        // "AutoSize button rows under-report, so pin a constant" rationale was a
        // misdiagnosis: PreferredSize is accurate here. The real historical under-
        // provisioning came from measuring in the CTOR at 96 DPI (see class remarks),
        // which a generously-pinned constant masked at 100% but over-padded at 150%.
        _root.PerformLayout();
        ClientSize = _root.PreferredSize;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Match the titlebar to the active chrome theme BEFORE the window becomes
        // visible. Try the modern attribute 20 first (Win10 20H1+/Win11); fall
        // back to legacy 19 (Win10 1809–19H2) only if 20 is rejected.
        int dark = Theme.IsDark ? 1 : 0;
        int hr = NativeMethods.DwmSetWindowAttribute(
            Handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref dark,
            sizeof(int));
        if (hr != 0)
        {
            NativeMethods.DwmSetWindowAttribute(
                Handle,
                NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
                ref dark,
                sizeof(int));
        }
    }

    // ── Container + control builders (no pixel coordinates) ────────────────

    /// <summary>A vertically-stacked, auto-sizing column of controls.</summary>
    private static FlowLayoutPanel Column(params Control[] items)
    {
        var f = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Theme.BgColor,
            Margin = new Padding(0),
        };
        f.Controls.AddRange(items);
        return f;
    }

    /// <summary>A left-to-right, auto-sizing row of controls (label + field).</summary>
    private static FlowLayoutPanel Row(params Control[] items)
    {
        var f = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Theme.BgColor,
            Margin = new Padding(0),
        };
        f.Controls.AddRange(items);
        return f;
    }

    /// <summary>Equal-width button grid — each column an even Percent slice so
    /// the buttons fill and scale together with no per-button width math.
    /// Anchored Left|Right so the grid spans the full content width set by the
    /// widest stacked row.</summary>
    private static TableLayoutPanel ButtonRow(params Button[] btns)
    {
        var t = new TableLayoutPanel
        {
            ColumnCount = btns.Length,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.BgColor,
            Margin = new Padding(0, 6, 0, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
        };
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        for (int i = 0; i < btns.Length; i++)
        {
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / btns.Length));
            t.Controls.Add(btns[i], i, 0);
        }
        return t;
    }

    private Label SectionLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = _boldFont,
        ForeColor = Theme.AccentBlue,
        Margin = new Padding(3, 8, 3, 2),
    };

    private CheckBox MakeCheckBox(string text, bool isChecked)
    {
        var chk = new CheckBox
        {
            Text = text,
            Checked = isChecked,
            AutoSize = true,
            // Indented under the section header. Dark uses pure white for the
            // glyph/label (the body Fg renders thin at 9pt through Flat's
            // grayscale-AA path); Light uses the normal Fg.
            ForeColor = Theme.CheckboxFgColor,
            BackColor = Theme.BgColor,
            // FlatStyle.Flat respects ForeColor for the tick glyph and draws the
            // focus state in themed colours (Standard paints a light-themed glyph
            // and an XOR focus rect near-invisible on a dark BG).
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(16, 2, 4, 2),
        };
        chk.FlatAppearance.BorderColor = Theme.DividerColor;
        chk.FlatAppearance.CheckedBackColor = Theme.HighlightBg;
        chk.FlatAppearance.MouseOverBackColor = Theme.HighlightBg;
        return chk;
    }

    private Button MakeButton(string text, EventHandler onClick)
    {
        var b = new Button
        {
            Text = text,
            // AutoSize for height (no clipped 9pt descenders); Anchor Left|Right
            // fills the grid column width WITHOUT inflating it (a fixed Width here
            // would force the Percent column wide — 75px / 33% x 3 ~= 675px). The
            // window sizes to content via `ClientSize = _root.PreferredSize` in OnLoad —
            // PreferredSize is accurate for this relational layout (the earlier belief
            // that the AutoSize button row "under-reports" was a misdiagnosis).
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(4),
            Padding = new Padding(2, 6, 2, 6),
        };
        ThemeButton(b);
        b.Click += onClick;
        return b;
    }

    private static void ThemeButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.ForeColor = Theme.FgColor;
        btn.BackColor = Theme.BgColor;
        btn.FlatAppearance.BorderColor = Theme.DividerColor;
        // Explicit hover/pressed colours so Flat doesn't fall back to a light
        // SystemColors.ButtonHighlight flash against the dark palette.
        btn.FlatAppearance.MouseOverBackColor = Theme.HighlightBg;
        btn.FlatAppearance.MouseDownBackColor = Theme.EditBgColor;
    }

    private void Apply()
    {
        // _app is only null under the DEBUG render harness, where the Apply
        // buttons are never clicked — guard so the form still compiles/renders.
        _app?.ApplySettings(
            _chkCaps.Checked, _chkNum.Checked, _chkScroll.Checked,
            _chkOSD.Checked, _chkBeep.Checked, _chkStartup.Checked,
            (int)_nudPollInterval.Value,
            (_cboTheme.SelectedItem as string) ?? "System");
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
            _helpForm = null; // Close() on a Show()-ed form auto-disposes
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
