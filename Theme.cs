namespace CapsNumTray;

/// <summary>
/// Canonical Catppuccin Mocha palette for the dark theme. Shared by the tray
/// context menu (<see cref="BoldSegmentRenderer"/>), the Settings dialog, and
/// the OSD tooltip. Single source of truth — DO NOT redeclare these locally
/// in other files; future palette tweaks should land here and propagate.
/// </summary>
internal static class Theme
{
    // Base — form / menu background and primary text.
    public static readonly System.Drawing.Color BgColor          = System.Drawing.Color.FromArgb(0x1E, 0x1E, 0x2E);
    public static readonly System.Drawing.Color FgColor          = System.Drawing.Color.FromArgb(0xCD, 0xD6, 0xF3);

    // Variants.
    public static readonly System.Drawing.Color FgDisabledColor  = System.Drawing.Color.FromArgb(0x80, 0x80, 0x95);
    public static readonly System.Drawing.Color DimColor         = System.Drawing.Color.FromArgb(0xA0, 0xA0, 0xC0);

    // Surfaces.
    public static readonly System.Drawing.Color HighlightBg      = System.Drawing.Color.FromArgb(0x35, 0x35, 0x50);
    public static readonly System.Drawing.Color EditBgColor      = System.Drawing.Color.FromArgb(0x2A, 0x2A, 0x3E);
    public static readonly System.Drawing.Color DividerColor     = System.Drawing.Color.FromArgb(0x40, 0x40, 0x50);

    // Accent colors (Catppuccin Mocha). Section headers / success / warning —
    // reserved for places where pure FgColor would lose semantic meaning.
    public static readonly System.Drawing.Color AccentBlue       = System.Drawing.Color.FromArgb(0x89, 0xB4, 0xFA);
    public static readonly System.Drawing.Color AccentGreen      = System.Drawing.Color.FromArgb(0xA6, 0xE3, 0xA1);
    public static readonly System.Drawing.Color AccentWarn       = System.Drawing.Color.FromArgb(0xFA, 0xB3, 0x87);
}
