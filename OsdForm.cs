namespace CapsNumTray;

/// <summary>
/// Borderless, topmost, auto-hiding OSD tooltip form.
/// Replaces AHK's ToolTip() for transient notifications.
/// </summary>
internal sealed class OsdForm : Form
{
    private static readonly System.Drawing.Font SharedFont = new("Segoe UI", 9f);

    private readonly Label _label;
    private readonly System.Windows.Forms.Timer _hideTimer;
    private bool _disposed;

    private static OsdForm? _current;

    private OsdForm(string text, int durationMs)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = System.Drawing.Color.FromArgb(255, 255, 225); // tooltip yellow
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;

        _label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = SharedFont,
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
