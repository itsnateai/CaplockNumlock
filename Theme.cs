namespace CapsNumTray;

/// <summary>
/// Theme palette for window chrome (Settings, Help, Update, OSD, context menu).
/// Two palettes — Catppuccin Mocha (Dark) and a v2.1.x-style brand-blue light
/// palette (pure-white BG / near-black text / cornsilk focus tint) — selected
/// once at startup via <see cref="Initialize"/> based on the user's saved preference
/// (resolved through <see cref="ResolveIsDark"/>). The active palette is then
/// exposed via the static colour properties (BgColor, FgColor, etc.) that all
/// chrome surfaces read from.
///
/// Tray icons are NOT driven by this — they follow the OS theme directly via
/// <c>TrayApplication.DetectLightTheme()</c> + <c>IconManager.ReloadForTheme</c>,
/// independent of the user's window-chrome pin. (The On icons are colour-coded
/// and read fine on either taskbar; only the Off outlines have light variants.)
///
/// Why static state instead of a flowing instance: BoldSegmentRenderer's GDI
/// brush/pen cache, OsdForm.BorderPen, and HelpForm's section colours are all
/// <c>static readonly</c> field initializers that capture Theme.* at first
/// class load. They are write-once per process. <see cref="Initialize"/> MUST
/// be called before any of those classes is first touched (currently: before
/// <c>new BoldSegmentRenderer()</c> in TrayApplication's constructor body).
/// Changing theme at runtime is intentionally not supported — restart-to-apply
/// keeps the GDI caches honest.
/// </summary>
internal static class Theme
{
    private static bool _isDark = true;
    private static bool _initialized;

    /// <summary>True if the active palette is the dark (Mocha) one.</summary>
    public static bool IsDark => _isDark;

    /// <summary>
    /// Selects the active palette. Call once at startup, before any class
    /// with a <c>static readonly</c> Theme.* capture is first touched
    /// (BoldSegmentRenderer, OsdForm, HelpForm).
    /// </summary>
    public static void Initialize(bool isDark)
    {
        // Idempotent guard: second call CAN'T take effect because the GDI
        // brush/pen caches in BoldSegmentRenderer, OsdForm, and HelpForm
        // already captured Theme.* colours at first class load. Log loudly
        // (rather than silently returning) so a future maintainer who tries
        // to add live-theme-swap gets a Trace entry pointing at the constraint
        // instead of debugging a mixed palette in production. Rule 12 (CLAUDE.md):
        // fail loud, proactively — silent guards mask real bugs.
        if (_initialized)
        {
            System.Diagnostics.Trace.WriteLine(
                $"CapsNumTray: Theme.Initialize called twice (was isDark={_isDark}, requested {isDark}) — ignored. " +
                "Theme is restart-to-apply by design (static GDI caches captured at first class load).");
            return;
        }
        _isDark = isDark;
        _initialized = true;
    }

    /// <summary>
    /// Resolves the user's saved <c>ThemeMode</c> value ("System", "Dark",
    /// "Light", or empty) into a concrete is-dark decision. "System" (or any
    /// unrecognized value, including empty) reads the Windows
    /// <c>SystemUsesLightTheme</c> registry value.
    /// </summary>
    public static bool ResolveIsDark(string? configValue)
    {
        if (string.Equals(configValue, "Dark", System.StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(configValue, "Light", System.StringComparison.OrdinalIgnoreCase))
            return false;
        // "System" / null / empty / typo → follow OS.
        return !IsSystemLightTheme();
    }

    /// <summary>
    /// Reads <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\SystemUsesLightTheme</c>.
    /// Returns false on any failure (locked key, missing value, registry exception).
    /// </summary>
    public static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? val = key?.GetValue("SystemUsesLightTheme");
            return val is int i && i == 1;
        }
        catch (System.Exception ex)
        {
            // A registry read failure here silently sends the user to dark mode
            // regardless of their actual OS theme. Trace so the unexpected case
            // (locked HKCU, AppContainer sandbox, group policy) is at least
            // diagnosable instead of "why is my Settings dialog dark when my
            // taskbar is light".
            System.Diagnostics.Trace.WriteLine(
                $"CapsNumTray: Theme.IsSystemLightTheme registry read failed " +
                $"(err={ex.GetType().Name}: {ex.Message}) — assuming dark theme");
            return false;
        }
    }

    // ── Active palette accessors ───────────────────────────────────────────
    // Each property routes to the matching slot on the active palette. Form
    // code reads these once during construction (e.g. BackColor = Theme.BgColor)
    // and never again — no per-paint indirection cost.

    public static System.Drawing.Color BgColor         => _isDark ? Dark.Bg         : Light.Bg;
    public static System.Drawing.Color FgColor         => _isDark ? Dark.Fg         : Light.Fg;
    public static System.Drawing.Color FgDisabledColor => _isDark ? Dark.FgDisabled : Light.FgDisabled;
    public static System.Drawing.Color DimColor        => _isDark ? Dark.Dim        : Light.Dim;
    public static System.Drawing.Color HighlightBg     => _isDark ? Dark.HighlightBg: Light.HighlightBg;
    public static System.Drawing.Color EditBgColor     => _isDark ? Dark.EditBg     : Light.EditBg;
    public static System.Drawing.Color DividerColor    => _isDark ? Dark.Divider    : Light.Divider;
    public static System.Drawing.Color AccentBlue      => _isDark ? Dark.AccentBlue : Light.AccentBlue;
    public static System.Drawing.Color AccentGreen     => _isDark ? Dark.AccentGreen: Light.AccentGreen;
    public static System.Drawing.Color AccentWarn      => _isDark ? Dark.AccentWarn : Light.AccentWarn;

    /// <summary>
    /// CheckBox glyph + label colour. Dark uses pure white because the body Fg
    /// (#CDD6F3) renders thin against the dark BG at 9pt through FlatStyle.Flat's
    /// grayscale-AA path; Light uses the normal Fg (dark text reads fine on
    /// light BG without the boost).
    /// </summary>
    public static System.Drawing.Color CheckboxFgColor => _isDark ? System.Drawing.Color.White : Light.Fg;

    // ── Dark palette — Catppuccin Mocha ────────────────────────────────────
    private static class Dark
    {
        public static readonly System.Drawing.Color Bg          = System.Drawing.Color.FromArgb(0x1E, 0x1E, 0x2E);
        public static readonly System.Drawing.Color Fg          = System.Drawing.Color.FromArgb(0xCD, 0xD6, 0xF3);
        public static readonly System.Drawing.Color FgDisabled  = System.Drawing.Color.FromArgb(0x80, 0x80, 0x95);
        public static readonly System.Drawing.Color Dim         = System.Drawing.Color.FromArgb(0xA0, 0xA0, 0xC0);
        public static readonly System.Drawing.Color HighlightBg = System.Drawing.Color.FromArgb(0x35, 0x35, 0x50);
        public static readonly System.Drawing.Color EditBg      = System.Drawing.Color.FromArgb(0x2A, 0x2A, 0x3E);
        public static readonly System.Drawing.Color Divider     = System.Drawing.Color.FromArgb(0x40, 0x40, 0x50);
        public static readonly System.Drawing.Color AccentBlue  = System.Drawing.Color.FromArgb(0x89, 0xB4, 0xFA);
        public static readonly System.Drawing.Color AccentGreen = System.Drawing.Color.FromArgb(0xA6, 0xE3, 0xA1);
        public static readonly System.Drawing.Color AccentWarn  = System.Drawing.Color.FromArgb(0xFA, 0xB3, 0x87);
    }

    // ── Light palette — v2.1.x brand revival ───────────────────────────────
    // Pure-white BG / near-black text / brand-blue headers / cornsilk focus
    // tint — matches the original v2.1.x feel pixel-for-pixel on the slots
    // that existed before the v2.4.x dual-theme refactor. Replaces the
    // Catppuccin Latte palette which read as a cool blue-grey tint that hurt
    // the user's eyes against the dark mode's warm-neutral Mocha. Slot
    // semantics unchanged: HighlightBg is still the focus tint, EditBg is
    // still slightly recessed against Bg.
    private static class Light
    {
        public static readonly System.Drawing.Color Bg          = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF); // pure white
        public static readonly System.Drawing.Color Fg          = System.Drawing.Color.FromArgb(0x1E, 0x1E, 0x1E); // near-black text
        // FgDisabled is intentionally low-contrast (~2.6:1 on Bg). WCAG SC 1.4.3
        // exempts disabled controls; Win11 Fluent uses ~38% opacity (≈ #A0A0A0)
        // for the same purpose. The slot's job is "render as disabled", not
        // "remain readable".
        public static readonly System.Drawing.Color FgDisabled  = System.Drawing.Color.FromArgb(0xA0, 0xA0, 0xA0);
        public static readonly System.Drawing.Color Dim         = System.Drawing.Color.FromArgb(0x60, 0x60, 0x60); // 6.29:1 on white
        // HighlightBg is a WARM-tint focus accent (yellow-channel hue shift),
        // not a luminance differentiator — cornsilk vs pure-white is ~1.07:1
        // by luminance alone. That's by-design (matches the v2.1.x feel) and
        // works because callers pair it with Divider borders or Fg-coloured
        // glyphs for state; the cornsilk fill carries the "focus/hover" cue
        // via temperature, not brightness. Caveat: this cue is invisible to
        // tritan users (blue-yellow CVD, ~0.01% prevalence) — secondary state
        // cues (border weight, glyph) carry the load there.
        public static readonly System.Drawing.Color HighlightBg = System.Drawing.Color.FromArgb(0xFF, 0xF8, 0xDC); // cornsilk focus tint
        public static readonly System.Drawing.Color EditBg      = System.Drawing.Color.FromArgb(0xF5, 0xF5, 0xF5); // faint inset against white BG
        public static readonly System.Drawing.Color Divider     = System.Drawing.Color.FromArgb(0xD0, 0xD0, 0xD0);
        public static readonly System.Drawing.Color AccentBlue  = System.Drawing.Color.FromArgb(0x22, 0x55, 0xAA); // brand blue — section headers, 7.12:1 on white
        public static readonly System.Drawing.Color AccentGreen = System.Drawing.Color.FromArgb(0x2E, 0x7D, 0x32); // Material green 800, 5.13:1 on white (AA pass)
        public static readonly System.Drawing.Color AccentWarn  = System.Drawing.Color.FromArgb(0xC6, 0x28, 0x28); // Material red 800, 5.62:1 on white (AA pass)
    }
}
