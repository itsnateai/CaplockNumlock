namespace CapsNumTray;

/// <summary>
/// Borderless, topmost, auto-hiding OSD tooltip form.
/// Replaces AHK's ToolTip() for transient notifications.
/// </summary>
internal sealed class OsdForm : Form
{
    private static readonly System.Drawing.Font SharedFont = new("Segoe UI", 9f);

    // Border pen is cached as a static for the process lifetime — same rationale
    // as the BoldSegmentRenderer's brush/pen caches (24/7 tray, paint can fire
    // multiple times if another window overlaps the OSD during its 2s lifetime).
    private static readonly System.Drawing.Pen BorderPen = new(Theme.DividerColor);

    private readonly Label _label;
    private readonly System.Windows.Forms.Timer _hideTimer;
    private bool _disposed;

    private static OsdForm? _current;

    private OsdForm(string text, int durationMs)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        BackColor = Theme.BgColor;
        ForeColor = Theme.FgColor;
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
            ForeColor = Theme.FgColor,
            BackColor = Theme.BgColor,
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
        // off-by-one fix for Rectangle drawing). Pen is the shared static
        // instance so we don't allocate a GDI handle on every paint.
        e.Graphics.DrawRectangle(BorderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
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

    /// <summary>
    /// Show an OSD after <paramref name="delayMs"/> from now, for
    /// <paramref name="dwellMs"/>. Used by Program.cs post-launch when the user
    /// triggered an auto-restart (theme change, update apply) and we want to
    /// confirm the new state once the tray icons have had a moment to
    /// materialize. The timer fires once the WinForms message loop is up
    /// (so this is safe to call from Main BEFORE Application.Run).
    /// </summary>
    public static void ShowDelayedOsd(string text, int delayMs, int dwellMs)
    {
        var timer = new System.Windows.Forms.Timer { Interval = delayMs };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            ShowOsd(text, dwellMs);
        };
        timer.Start();
    }
}
