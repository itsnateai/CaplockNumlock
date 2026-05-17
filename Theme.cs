namespace CapsNumTray;

/// <summary>
/// Theme palette for window chrome (Settings, Help, Update, OSD, context menu).
/// Two palettes — Catppuccin Mocha (Dark) and Latte (Light) — selected once at
/// startup via <see cref="Initialize"/> based on the user's saved preference
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

    // ── Light palette — Catppuccin Latte ───────────────────────────────────
    // Latte is Mocha's canonical light counterpart; slot-for-slot semantics so
    // the layout code that worked against Mocha keeps working against Latte
    // without any tuning. EditBg = mantle (slightly darker than base) so input
    // fields read as inset against the form BG — matches the dark mode's
    // EditBg-darker-than-Bg relationship.
    private static class Light
    {
        // Bg = crust (the darkest of Latte's three neutral tiers), not base.
        // First-user feedback on the v2.4.6 pre-ship build was that the canonical
        // Latte base (#EFF1F5, ~91% relative luminance) read as "burn your eyes
        // bright white" at Settings-dialog dimensions — fine for code-editor body
        // text (Latte's intended use case) but too brilliant for an OS dialog
        // taking up screen real-estate. Crust drops perceived luminance ~13%
        // and is still a clean Catppuccin grey, with the bonus that every other
        // palette slot's contrast against the BG *improves* (AccentWarn red
        // 3.9:1 → 5.5:1, Fg 7.7:1 → 5.5:1 — still strictly WCAG AA on all).
        public static readonly System.Drawing.Color Bg          = System.Drawing.Color.FromArgb(0xDC, 0xE0, 0xE8); // crust
        public static readonly System.Drawing.Color Fg          = System.Drawing.Color.FromArgb(0x4C, 0x4F, 0x69); // text
        public static readonly System.Drawing.Color FgDisabled  = System.Drawing.Color.FromArgb(0x9C, 0xA0, 0xB0); // overlay0
        // Dim = subtext1 (#5C5F77, ~5.6:1 against base) rather than subtext0
        // (#7C7F93, ~4.0:1). subtext0 was borderline-failing WCAG AA for the
        // italic 9.5pt _lblDetail in UpdateDialog where Dim shows up; subtext1
        // is still a Latte palette slot but reads cleanly at small italic sizes.
        public static readonly System.Drawing.Color Dim         = System.Drawing.Color.FromArgb(0x5C, 0x5F, 0x77); // subtext1
        public static readonly System.Drawing.Color HighlightBg = System.Drawing.Color.FromArgb(0xCC, 0xD0, 0xDA); // surface0
        public static readonly System.Drawing.Color EditBg      = System.Drawing.Color.FromArgb(0xE6, 0xE9, 0xEF); // mantle
        public static readonly System.Drawing.Color Divider     = System.Drawing.Color.FromArgb(0xBC, 0xC0, 0xCC); // surface1
        public static readonly System.Drawing.Color AccentBlue  = System.Drawing.Color.FromArgb(0x1E, 0x66, 0xF5); // blue
        public static readonly System.Drawing.Color AccentGreen = System.Drawing.Color.FromArgb(0x40, 0xA0, 0x2B); // green
        // Warning accent = Latte red (#D20F39), not peach (#FE640B). Peach on
        // base #EFF1F5 is ~2.7:1 — fails WCAG AA on the UpdateDialog error path
        // (_lblStatus.ForeColor = Theme.AccentWarn). Red has both better contrast
        // (~3.9:1) AND the correct semantic for error states. Dark mode keeps
        // peach (#FAB387) because contrast against a dark BG isn't the bottleneck
        // there.
        public static readonly System.Drawing.Color AccentWarn  = System.Drawing.Color.FromArgb(0xD2, 0x0F, 0x39); // red
    }
}
