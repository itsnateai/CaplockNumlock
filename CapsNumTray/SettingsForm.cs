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
        Font = new System.Drawing.Font("Segoe UI", 9f);

        int y = 16;

        // ── Tray Icons ──
        var lblIcons = new Label { Text = "Tray Icons", Location = new(16, y), AutoSize = true, Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold) };
        Controls.Add(lblIcons);
        y += 26;

        _chkCaps = AddCheckBox("Show Caps Lock icon", config.ShowCaps, 28, ref y);
        _chkNum = AddCheckBox("Show Num Lock icon", config.ShowNum, 28, ref y);
        _chkScroll = AddCheckBox("Show Scroll Lock icon", config.ShowScroll, 28, ref y);

        // ── Feedback ──
        y += 10;
        var lblFeedback = new Label { Text = "Feedback", Location = new(16, y), AutoSize = true, Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold) };
        Controls.Add(lblFeedback);
        y += 26;

        _chkOSD = AddCheckBox("Show OSD tooltip on toggle", config.ShowOSD, 28, ref y);
        _chkBeep = AddCheckBox("Beep on toggle", config.BeepOnToggle, 28, ref y);

        // ── Startup ──
        y += 10;
        var lblStartup = new Label { Text = "Startup", Location = new(16, y), AutoSize = true, Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold) };
        Controls.Add(lblStartup);
        y += 26;

        _chkStartup = AddCheckBox("Run at Windows startup", StartupManager.IsEnabled, 28, ref y);

        // ── Buttons ──
        y += 16;

        var btnGitHub = new Button { Text = "GitHub", Location = new(16, y), Size = new(80, 28) };
        btnGitHub.Click += (_, _) =>
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/itsnateai/CaplockNumlock",
                UseShellExecute = true
            });
        };
        Controls.Add(btnGitHub);

        var btnHelp = new Button { Text = "Help", Location = new(102, y), Size = new(55, 28) };
        btnHelp.Click += (_, _) => ShowHelpWindow();
        Controls.Add(btnHelp);

        var btnOK = new Button { Text = "OK", Location = new(200, y), Size = new(70, 28) };
        btnOK.Click += (_, _) => { Apply(); Close(); };
        Controls.Add(btnOK);
        AcceptButton = btnOK;

        var btnApply = new Button { Text = "Apply", Location = new(278, y), Size = new(70, 28) };
        btnApply.Click += (_, _) => Apply();
        Controls.Add(btnApply);

        var btnCancel = new Button { Text = "Cancel", Location = new(356, y), Size = new(70, 28) };
        btnCancel.Click += (_, _) => Close();
        Controls.Add(btnCancel);
        CancelButton = btnCancel;

        ClientSize = new System.Drawing.Size(440, y + 42);
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
            _chkOSD.Checked, _chkBeep.Checked, _chkStartup.Checked);
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
            _helpForm.Dispose();
            _helpForm = null;
        };
        _helpForm.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _helpForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
