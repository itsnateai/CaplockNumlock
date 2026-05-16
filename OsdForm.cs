namespace CapsNumTray;

/// <summary>
/// Borderless, topmost, auto-hiding OSD tooltip form.
/// Replaces AHK's ToolTip() for transient notifications.
/// </summary>
internal sealed class OsdForm : Form
{
    private static readonly System.Drawing.Font SharedFont = new("Segoe UI", 9f);

    // Catppuccin Mocha palette — matches the tray menu + Settings window.
    private static readonly System.Drawing.Color OsdBgColor      = System.Drawing.Color.FromArgb(0x1E, 0x1E, 0x2E);
    private static readonly System.Drawing.Color OsdFgColor      = System.Drawing.Color.FromArgb(0xCD, 0xD6, 0xF3);
    private static readonly System.Drawing.Color OsdBorderColor  = System.Drawing.Color.FromArgb(0x40, 0x40, 0x50);

    private readonly Label _label;
    private readonly System.Windows.Forms.Timer _hideTimer;
    private bool _disposed;

    private static OsdForm? _current;

    private OsdForm(string text, int durationMs)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = OsdBgColor;
        ForeColor = OsdFgColor;
        StartPosition = FormStartPosition.Manual;
        // Pin design baseline to 96 DPI BEFORE setting AutoScaleMode so any
        // future literal Size/Point in this form is interpreted as 96-DPI
        // design pixels regardless of which monitor first realizes it. The
        // size below is already DPI-correct via TextRenderer.MeasureText
        // (handle-independent, measures at current DC), but the pin is
        // canonical per _.claude/_templates/snippets/csharp/winforms-dpi-scaling.md
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        _label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = SharedFont,
            ForeColor = OsdFgColor,
            BackColor = OsdBgColor,
            Location = new System.Drawing.Point(6, 4),
        };
        Controls.Add(_label);

        // Size to content
        var sz = TextRenderer.MeasureText(text, SharedFont);
        ClientSize = new System.Drawing.Size(sz.Width + 16, sz.Height + 10);

        // Position near cursor, clamped to the working area of the active screen
        var pos = Cursor.Position;
        var screen = Screen.FromPoint(pos);
        var area = screen.WorkingArea;
        int x = pos.X + 16;
        int y = pos.Y + 16;
        if (x + Width > area.Right) x = area.Right - Width;
        if (y + Height > area.Bottom) y = area.Bottom - Height;
        if (x < area.Left) x = area.Left;
        if (y < area.Top) y = area.Top;
        Location = new System.Drawing.Point(x, y);

        _hideTimer = new System.Windows.Forms.Timer { Interval = durationMs };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            if (_current == this)
                _current = null;
            Close();
        };
        _hideTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // 1px border in DividerColor — the form is borderless and the BG would
        // otherwise blend into a dark wallpaper. Inset by 1px so the line draws
        // fully inside ClientRectangle (Width-1 / Height-1 is the standard
        // off-by-one fix for Rectangle drawing).
        using var pen = new Pen(OsdBorderColor);
        e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;
            _hideTimer.Stop();
            _hideTimer.Dispose();
            _label.Dispose();
        }
        base.Dispose(disposing);
    }

    // Prevent stealing focus
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST (authoritative — TopMost property removed)
            return cp;
        }
    }

    public static void ShowOsd(string text, int durationMs = 2000)
    {
        if (_current != null && !_current.IsDisposed)
        {
            _current.Close();
        }
        _current = new OsdForm(text, durationMs);
        _current.Show();
    }
}
