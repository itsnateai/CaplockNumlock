namespace CapsNumTray;

/// <summary>
/// Resizable, scrollable help window matching the AHK help dialog.
/// </summary>
internal sealed class HelpForm : Form
{
    private readonly TextBox _textBox;

    private const string HelpText =
        "CAPSNUMTRAY \u2014 Caps/Num/Scroll Lock Tray Indicators\r\n" +
        "\r\n" +
        "CapsNumTray adds independent system tray icons that show the current state of your " +
        "Caps Lock, Num Lock, and Scroll Lock keys. Left-click to toggle, right-click for options.\r\n" +
        "\r\n" +
        "Bright icon = key is ON\r\n" +
        "Dim icon = key is OFF\r\n" +
        "\r\n" +
        "\u2500\u2500\u2500 BASIC USAGE \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\r\n" +
        "\r\n" +
        "\u2022 Left-click any tray icon to toggle that key.\r\n" +
        "\u2022 Right-click any icon for a menu with toggle, visibility, settings, and exit.\r\n" +
        "\r\n" +
        "\u2500\u2500\u2500 SETTINGS \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\r\n" +
        "\r\n" +
        "Show Caps/Num/Scroll Lock icon: Choose which icons appear in the tray. " +
        "Scroll Lock is hidden by default. At least one must remain visible.\r\n" +
        "\r\n" +
        "Show OSD tooltip on toggle: A small floating tooltip appears briefly showing " +
        "the new state after toggling.\r\n" +
        "\r\n" +
        "Beep on toggle: Plays a short tone when you toggle a key. Higher pitch = ON, " +
        "lower pitch = OFF.\r\n" +
        "\r\n" +
        "Run at Windows startup: Creates a shortcut in your Startup folder so CapsNumTray " +
        "launches automatically at login.\r\n" +
        "\r\n" +
        "All settings are saved to CapsNumTray.ini and persist across restarts.\r\n" +
        "\r\n" +
        "\u2500\u2500\u2500 TRAY ICONS \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\r\n" +
        "\r\n" +
        "Icons are embedded in the application. When running from source, icons are loaded " +
        "from the icons/ folder. Light-theme OFF variants are used automatically when Windows " +
        "is set to a light taskbar theme. If all else fails, Windows built-in system icons are " +
        "used as a fallback.\r\n" +
        "\r\n" +
        "\u2500\u2500\u2500 TECHNICAL NOTES \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\r\n" +
        "\r\n" +
        "CapsNumTray uses the Win32 Shell_NotifyIconW API directly to support multiple " +
        "independent tray icons. A 5-second polling timer keeps icons in sync even when keys " +
        "are changed externally. Icons are automatically re-added if Explorer restarts.";

    public HelpForm()
    {
        Text = "CapsNumTray v" + TrayApplication.Version + " \u2014 Help";
        TopMost = true;
        BackColor = System.Drawing.Color.White;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new System.Drawing.Size(400, 300);
        ClientSize = new System.Drawing.Size(460, 420);

        _textBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            Text = HelpText,
            Location = new(10, 10),
            Size = new(ClientSize.Width - 20, ClientSize.Height - 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };
        Controls.Add(_textBox);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _textBox.Dispose();
        }
        base.Dispose(disposing);
    }
}
