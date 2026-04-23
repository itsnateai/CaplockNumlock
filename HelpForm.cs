namespace CapsNumTray;

using System.Text;

/// <summary>
/// Resizable help window. Renders a structured, typographically styled view
/// of <see cref="s_helpText"/> via a <see cref="RichTextBox"/>.
///
/// Formatting mirrors MicMute's HelpWindow: title → large bold, section
/// headers (lines starting with ——— or ---) → semibold blue, body → regular.
/// </summary>
internal sealed class HelpForm : Form
{
    private readonly RichTextBox _textBox;

    // Fonts + colors inlined — CapsNumTray has no UiTokens class. Same values
    // as MicMute/UiTokens so the two help windows read identically.
    private const string PrimaryFont  = "Segoe UI";
    private const string SemiboldFont = "Segoe UI Semibold";
    private const float  TitleSize    = 13.5f;
    private const float  HeaderSize   = 10.75f;
    private const float  BodySize     = 9.75f;

    private static readonly Color s_titleColor  = Color.FromArgb(0x11, 0x11, 0x11);
    private static readonly Color s_headerColor = Color.FromArgb(0x22, 0x55, 0xAA);
    private static readonly Color s_bodyColor   = Color.FromArgb(0x1E, 0x1E, 0x1E);

    // Static fonts live for process lifetime — no per-instance Dispose needed.
    private static readonly Font s_titleFont  = new(PrimaryFont, TitleSize, FontStyle.Bold);
    private static readonly Font s_headerFont = new(SemiboldFont, HeaderSize, FontStyle.Bold);
    private static readonly Font s_bodyFont   = new(PrimaryFont, BodySize);

    private static readonly string s_helpText = @"CAPSNUMTRAY — Caps/Num/Scroll Lock Tray Indicators

CapsNumTray adds independent system tray icons that show the current state of your Caps Lock, Num Lock, and Scroll Lock keys. Left-click to toggle, right-click for options.

Bright icon = key is ON
Dim icon = key is OFF

——— BASIC USAGE ———————————————————

• Left-click any tray icon to toggle that key.
• Right-click any icon for a menu with toggle, visibility, settings, and exit.

——— SETTINGS —————————————————————

Show Caps/Num/Scroll Lock icon: Choose which icons appear in the tray. Scroll Lock is hidden by default. At least one must remain visible.

Show OSD tooltip on toggle: A small floating tooltip appears briefly showing the new state after toggling.

Beep on toggle: Plays a short tone when you toggle a key. Higher pitch = ON, lower pitch = OFF.

Run at Windows startup: Creates a shortcut in your Startup folder so CapsNumTray launches automatically at login.

All settings are saved to CapsNumTray.ini and persist across restarts.

——— TRAY ICONS ———————————————————

Icons are embedded in the application. When running from source, icons are loaded from the icons/ folder. Light-theme OFF variants are used automatically when Windows is set to a light taskbar theme. If all else fails, Windows built-in system icons are used as a fallback.

——— TECHNICAL NOTES ——————————————

CapsNumTray uses the Win32 Shell_NotifyIconW API directly to support multiple independent tray icons. A low-level keyboard hook detects toggle key changes instantly. A configurable polling timer (disabled by default, adjustable in Settings) acts as a failsafe for external changes (RDP, other apps). If the low-level keyboard hook ever fails to install, polling auto-enables at 10 seconds so the tray stays in sync. Icons are automatically re-added if Explorer restarts, and tray state resyncs on resume from sleep and on RDP reconnect.

Fallback poll interval: Controls how often the app checks key states independently of the keyboard hook. Range: 0 (disabled, the default) to 300 seconds (5 minutes). The keyboard hook handles normal key presses instantly, so this is only needed as a safety net for scenarios the hook can't see.";

    public HelpForm()
    {
        Text = "CapsNumTray v" + TrayApplication.Version + " — Help";
        TopMost = true;
        BackColor = Color.White;
        ClientSize = new Size(540, 560);
        MinimumSize = new Size(440, 360);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;

        const int margin = 18;
        const int topGap = 14;

        _textBox = new RichTextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            DetectUrls = false,
            WordWrap = true,
            TabStop = false,
            Location = new Point(margin, topGap),
            Size = new Size(ClientSize.Width - 2 * margin, ClientSize.Height - topGap - margin),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };
        Controls.Add(_textBox);

        RenderHelp();

        // Kill the default "all text selected on show" behaviour.
        Shown += (_, _) =>
        {
            _textBox.SelectionStart = 0;
            _textBox.SelectionLength = 0;
            _textBox.DeselectAll();
            ActiveControl = null;
        };
    }

    private void RenderHelp()
    {
        _textBox.Clear();

        var body = new StringBuilder();

        void FlushBody()
        {
            if (body.Length == 0) return;
            // Collapse leading blank lines so sections don't have stacked gaps.
            var text = body.ToString().TrimStart('\r', '\n');
            body.Clear();
            if (text.Length == 0) return;
            _textBox.SelectionFont = s_bodyFont;
            _textBox.SelectionColor = s_bodyColor;
            _textBox.AppendText(text);
        }

        var lines = s_helpText.Replace("\r\n", "\n").Split('\n');
        bool titleWritten = false;

        foreach (var raw in lines)
        {
            if (!titleWritten)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                _textBox.SelectionFont = s_titleFont;
                _textBox.SelectionColor = s_titleColor;
                _textBox.AppendText(raw.Trim() + "\n\n");
                titleWritten = true;
                continue;
            }

            if (raw.StartsWith("———") || raw.StartsWith("---"))
            {
                FlushBody();
                var title = raw.Trim().Trim('—', '-', ' ');
                if (title.Length == 0) continue;
                _textBox.AppendText("\n");
                _textBox.SelectionFont = s_headerFont;
                _textBox.SelectionColor = s_headerColor;
                _textBox.AppendText(title + "\n\n");
                continue;
            }

            body.AppendLine(raw);
        }
        FlushBody();

        _textBox.SelectionStart = 0;
        _textBox.SelectionLength = 0;
    }

    // Static fonts live for process lifetime; base.Dispose handles the RichTextBox.
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
