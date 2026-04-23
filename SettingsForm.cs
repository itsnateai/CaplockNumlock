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

        Text = "CapsNumTray v" + TrayApplication.Version + " \u2014 Settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        BackColor = System.Drawing.Color.White;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        _formFont = new System.Drawing.Font("Segoe UI", 9f);
        Font = _formFont;
        _boldFont = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);

        const int PollInputX = 320;

        int yLeft = 16;
        int yRight = 16;

        // ── Tray Icons (left column, top) ──
        var lblIcons = new Label { Text = "Tray Icons", Location = new(16, yLeft), AutoSize = true, Font = _boldFont };
        Controls.Add(lblIcons);
        yLeft += 26;

        _chkCaps = AddCheckBox("Show Caps Lock icon", config.ShowCaps, 28, ref yLeft);
        _chkNum = AddCheckBox("Show Num Lock icon", config.ShowNum, 28, ref yLeft);
        _chkScroll = AddCheckBox("Show Scroll Lock icon", config.ShowScroll, 28, ref yLeft);

        // ── Startup (right column, top) ──
        const string startupText = "Run at Windows startup";
        const int StartupHdrX = 210;

        var lblStartup = new Label { Text = "Startup", Location = new(StartupHdrX, yRight), AutoSize = true, Font = _boldFont };
        Controls.Add(lblStartup);
        yRight += 26;

        _chkStartup = AddCheckBox(startupText, StartupManager.IsEnabled, StartupHdrX + 12, ref yRight);

        int y = Math.Max(yLeft, yRight);

        // ── Feedback ──
        y += 10;
        var lblFeedback = new Label { Text = "Feedback", Location = new(16, y), AutoSize = true, Font = _boldFont };
        Controls.Add(lblFeedback);
        y += 26;

        _chkOSD = AddCheckBox("Show OSD tooltip on toggle", config.ShowOSD, 28, ref y);
        _chkBeep = AddCheckBox("Beep on toggle", config.BeepOnToggle, 28, ref y);

        // ── Polling ──
        y += 10;
        var lblPolling = new Label { Text = "Polling", Location = new(16, y), AutoSize = true, Font = _boldFont };
        Controls.Add(lblPolling);
        y += 26;

        var lblPollDesc = new Label { Text = "Fallback poll interval (seconds, 0 = disabled):", Location = new(28, y + 2), AutoSize = true };
        Controls.Add(lblPollDesc);
        _nudPollInterval = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 300,
            Value = config.PollInterval,
            Location = new(PollInputX, y),
            Size = new(60, 24),
            Increment = 5,
        };
        Controls.Add(_nudPollInterval);
        y += 28;

        // ── Buttons (two rows, each row fills horizontally with equal-width buttons) ──
        y += 16;

        const int FormWidth = 480;
        // Equal padding: border|pad|btn|pad|btn|pad|btn|pad|border
        // 4 * pad + 3 * btnW = FormWidth. With pad=12, btnW=144 → exactly 480.
        const int BtnPad = 12;
        const int BtnW = (FormWidth - 4 * BtnPad) / 3; // = 144

        // Primary (bottom) row: full 144×28. Utility (top) row: smaller 120×24 for visual contrast.
        const int BotBtnH = 28;
        const int TopBtnW = 120;
        const int TopBtnH = 24;
        const int TopBtnOffset = (BtnW - TopBtnW) / 2; // center each small button in its column slot

        int col1X = BtnPad;
        int col2X = col1X + BtnW + BtnPad;
        int col3X = col2X + BtnW + BtnPad;

        // Row 1 (top): utility — GitHub, Update, Help (smaller, centered in column slots)
        int gitHubX = col1X + TopBtnOffset;
        int updateX = col2X + TopBtnOffset;
        int helpX   = col3X + TopBtnOffset;

        var btnGitHub = new Button { Text = "GitHub", Location = new(gitHubX, y), Size = new(TopBtnW, TopBtnH) };
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
        btnUpdate.Click += (_, _) =>
        {
            using var dlg = new UpdateDialog();
            dlg.ShowDialog(this);
        };
        Controls.Add(btnUpdate);

        var btnHelp = new Button { Text = "Help", Location = new(helpX, y), Size = new(TopBtnW, TopBtnH) };
        btnHelp.Click += (_, _) => ShowHelpWindow();
        Controls.Add(btnHelp);

        // Row 2 (bottom): actions — OK, Apply, Cancel (full-size, primary)
        y += TopBtnH + BtnPad;

        int okX     = col1X;
        int applyX  = col2X;
        int cancelX = col3X;

        var btnOK = new Button { Text = "OK", Location = new(okX, y), Size = new(BtnW, BotBtnH) };
        btnOK.Click += (_, _) => { Apply(); Close(); };
        Controls.Add(btnOK);
        AcceptButton = btnOK;

        var btnApply = new Button { Text = "Apply", Location = new(applyX, y), Size = new(BtnW, BotBtnH) };
        btnApply.Click += (_, _) => Apply();
        Controls.Add(btnApply);

        var btnCancel = new Button { Text = "Cancel", Location = new(cancelX, y), Size = new(BtnW, BotBtnH) };
        btnCancel.Click += (_, _) => Close();
        Controls.Add(btnCancel);
        CancelButton = btnCancel;

        ClientSize = new System.Drawing.Size(FormWidth, y + BotBtnH + 16);
    }

    private CheckBox AddCheckBox(string text, bool isChecked, int x, ref int y)
    {
        var chk = new CheckBox
        {
            Text = text,
            Checked = isChecked,
            Location = new(x, y),
            AutoSize = true,
        };
        Controls.Add(chk);
        y += 24;
        return chk;
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
