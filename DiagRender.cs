#if DEBUG
using System.Drawing;
using System.Drawing.Imaging;

namespace CapsNumTray;

/// <summary>
/// Debug-only DPI render harness. Realizes each chrome form at the host/VM's
/// real DPI and writes a PNG of its client area so 100% vs 150% layout can be
/// diffed visually. Windows Sandbox is locked at 96 DPI; this is meant to run
/// on Tiny11Lab at a real 150% scale (the PNG is ground truth — it beats any
/// static "this will clip" reasoning).
///
/// Never compiled into Release (whole file is <c>#if DEBUG</c>). Invoke via:
///   CapsNumTray.exe --diag-render-form &lt;SettingsForm|UpdateDialog|HelpForm|OsdForm|all&gt; --out &lt;dir&gt;
///
/// Each PNG is named <c>&lt;Form&gt;-dpi&lt;DeviceDpi&gt;.png</c> and the console line reports the
/// resolved scale (DeviceDpi 144 == 150%), so the filename itself proves the scale rendered.
/// </summary>
internal static class DiagRender
{
    /// <summary>True while a render pass is running — lets forms suppress
    /// runtime side effects (e.g. UpdateDialog's GitHub network call) that
    /// would distort the captured layout or fail on the air-gapped VM.</summary>
    internal static bool Active;

    internal static void Run(string[] args)
    {
        Active = true;

        string outDir = GetArg(args, "--out") ?? Path.Combine(Path.GetTempPath(), "capsnumtray-diag");
        string which = GetArg(args, "--diag-render-form") ?? "all";
        Directory.CreateDirectory(outDir);

        // Sets the process to PerMonitorV2 high-DPI (matches the manifest) so a
        // form realized on a 150% monitor scales to 144 DeviceDpi.
        ApplicationConfiguration.Initialize();

        // ConfigManager falls back to defaults when the file is absent, giving a
        // representative form. Theme must be initialized before any form reads
        // Theme.* (static GDI caches capture on first touch).
        var config = new ConfigManager(Path.Combine(outDir, "_diag.ini"));
        Theme.Initialize(Theme.ResolveIsDark(config.ThemeMode));

        string[] targets = which.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? new[] { "SettingsForm", "UpdateDialog", "HelpForm", "OsdForm" }
            : new[] { which };

        // WinExe has no attached console over SSH, so the run is reported into a
        // text file pulled back alongside the PNGs — the DeviceDpi line is the
        // proof the forms rendered at the intended scale.
        var report = new List<string> { $"CapsNumTray DPI render — {targets.Length} target(s)" };
        foreach (var name in targets)
        {
            try
            {
                using Form form = CreateForm(name, config);
                report.Add(Capture(form, name, outDir));
            }
            catch (Exception ex)
            {
                report.Add($"{name}: FAILED — {ex.GetType().Name}: {ex.Message}");
            }
        }
        File.WriteAllLines(Path.Combine(outDir, "_render-report.txt"), report);
    }

    /// <summary>
    /// Live-window measurement for the Settings dialog. Unlike Run()'s offscreen
    /// DrawToBitmap (which is unfaithful for UserPaint forms), this shows the form
    /// ON SCREEN, lets OnLoad/OnShown + layout fully settle, then captures the real
    /// composited pixels via CopyFromScreen and pixel-scans for the true button-row
    /// bottom. Cross-checks against the layout's own _root.Height / PreferredSize so
    /// we know whether size-to-content is reliable. Prints the exact gutter + the
    /// recommended ClientSize — math, not guesswork.
    /// </summary>
    internal static void MeasureSettings(string[] args)
    {
        Active = true;
        string outDir = GetArg(args, "--out") ?? Path.Combine(Path.GetTempPath(), "capsnumtray-diag");
        Directory.CreateDirectory(outDir);

        ApplicationConfiguration.Initialize();
        var config = new ConfigManager(Path.Combine(outDir, "_diag.ini"));
        Theme.Initialize(Theme.ResolveIsDark(config.ThemeMode));

        var report = new List<string> { "CapsNumTray — Settings live measurement" };
        using var form = new SettingsForm(config, null);
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(120, 120);
        form.TopMost = true;
        form.Show();
        form.Activate();
        // Let OnLoad/OnShown, layout, and paint fully settle on a real message pump.
        for (int i = 0; i < 40; i++) { Application.DoEvents(); Thread.Sleep(15); }

        int dpi = form.DeviceDpi;
        var root = (TableLayoutPanel)form.Controls[0];
        var actionRow = root.Controls[root.Controls.Count - 1];
        var clientSize = form.ClientSize;

        report.Add($"DeviceDpi = {dpi} ({dpi * 100 / 96}%)");
        report.Add($"ClientSize.Height (set in OnLoad) = {clientSize.Height}");
        report.Add($"_root.Height (AutoSize)             = {root.Height}");
        report.Add($"_root.PreferredSize.Height          = {root.PreferredSize.Height}");
        report.Add($"_root.Top                           = {root.Top}  (Padding bottom = {root.Padding.Bottom})");
        report.Add($"actionRow.Bottom in client coords   = {root.Top + actionRow.Bottom}");

        // Real capture: client-area pixels straight off the screen compositor.
        using var bmp = new Bitmap(clientSize.Width, clientSize.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, clientSize);
        string png = Path.Combine(outDir, $"SettingsMeasure-dpi{dpi}.png");
        bmp.Save(png, ImageFormat.Png);

        // Sample the bg from the captured bitmap itself (top-left _root padding at
        // (3,3) is always background) — using Theme.BgColor as the reference can
        // miss by a few units after compositor capture and break the scan.
        var bg = bmp.GetPixel(3, 3);
        report.Add($"bg sample (3,3) = R{bg.R} G{bg.G} B{bg.B}");
        bool IsBg(System.Drawing.Color p) =>
            Math.Abs(p.R - bg.R) + Math.Abs(p.G - bg.G) + Math.Abs(p.B - bg.B) <= 24;
        // True rendered top of content (first non-bg row from the top).
        int trueTop = -1;
        for (int y = 0; y < clientSize.Height && trueTop < 0; y++)
            for (int x = 0; x < clientSize.Width; x++)
                if (!IsBg(bmp.GetPixel(x, y))) { trueTop = y; break; }
        // True rendered bottom of content (first non-bg row from the bottom).
        int trueBottom = -1;
        for (int y = clientSize.Height - 1; y >= 0 && trueBottom < 0; y--)
            for (int x = 0; x < clientSize.Width; x++)
                if (!IsBg(bmp.GetPixel(x, y))) { trueBottom = y; break; }
        report.Add($"rendered content top row   = {trueTop}px (top margin)");
        // Column probe down the centre (over the Apply button) to see exactly where
        // the button row ends vs where pure bg resumes — settles under-report vs gutter.
        int cx = clientSize.Width / 2;
        report.Add($"--- centre-column probe (x={cx}) bg=is background ---");
        for (int y = 300; y < clientSize.Height; y += 8)
        {
            var p = bmp.GetPixel(cx, y);
            report.Add($"  y={y,3}: R{p.R,3} G{p.G,3} B{p.B,3}  {(IsBg(p) ? "bg" : "CONTENT")}");
        }
        int gutterPx = (clientSize.Height - 1) - trueBottom;
        report.Add("--- real pixel scan (CopyFromScreen) ---");
        report.Add($"true content bottom row    = {trueBottom}px");
        report.Add($"current bottom gutter      = {gutterPx}px  (~{gutterPx * 96 / dpi} logical)");
        report.Add($"true content height        = {trueBottom + 1}px  (~{(trueBottom + 1) * 96 / dpi} logical)");
        report.Add($"if ClientSize=_root.Size -> bottom gutter would be {clientSize.Height - root.Height + gutterPx}px (the {root.Padding.Bottom}px _root pad, symmetric with top)");
        report.Add($"-> SAVED {Path.GetFileName(png)}");

        form.Close();
        File.WriteAllLines(Path.Combine(outDir, "_measure-report.txt"), report);
    }

    /// <summary>
    /// Live ground-truth measurement for the Update dialog — the UserPaint twin of
    /// MeasureSettings. DrawToBitmap mis-composites this OptimizedDoubleBuffer/UserPaint
    /// form (the button row renders at the wrong offset), so the only honest check that
    /// the content-fit height isn't clipping the button row is an on-screen CopyFromScreen
    /// capture + a pixel scan for the true content bottom. Populates the "new version"
    /// state (both buttons visible) — the tallest button-row state — via DiagPopulate.
    /// </summary>
    internal static void MeasureUpdate(string[] args)
    {
        Active = true;
        string outDir = GetArg(args, "--out") ?? Path.Combine(Path.GetTempPath(), "capsnumtray-diag");
        Directory.CreateDirectory(outDir);

        ApplicationConfiguration.Initialize();
        var config = new ConfigManager(Path.Combine(outDir, "_diag.ini"));
        Theme.Initialize(Theme.ResolveIsDark(config.ThemeMode));

        var report = new List<string> { "CapsNumTray — Update dialog live measurement" };
        using var form = new UpdateDialog();
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(120, 120);
        form.TopMost = true;
        form.Show();
        form.Activate();
        // OnLoad fits the initial (Checking...) height; settle it on a real pump.
        for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(15); }
        // Swap to the tallest button-row state (Upgrade Now + Cancel) and let the
        // FitToContentHeight re-fit settle before capturing.
        form.DiagPopulate();
        for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(15); }

        int dpi = form.DeviceDpi;
        var root = (TableLayoutPanel)form.Controls[0];
        var buttonRow = (FlowLayoutPanel)root.Controls[root.Controls.Count - 1];   // the button host
        var clientSize = form.ClientSize;

        report.Add($"DeviceDpi = {dpi} ({dpi * 100 / 96}%)");
        report.Add($"CurrentAutoScaleDimensions       = {form.CurrentAutoScaleDimensions}");
        report.Add($"ClientSize                       = {clientSize.Width}x{clientSize.Height}");
        report.Add($"_root.Height (AutoSize)          = {root.Height}");
        report.Add($"_root.PreferredSize.Height       = {root.PreferredSize.Height}");
        report.Add($"_root.Padding                    = {root.Padding.Left},{root.Padding.Top},{root.Padding.Right},{root.Padding.Bottom}");
        report.Add($"buttonRow.Bottom in client coords= {root.Top + buttonRow.Bottom}  (Padding bottom = {root.Padding.Bottom})");
        // Did the fixed-size elements scale? At 150% expect buttons/progress ~1.5x.
        foreach (Control b in buttonRow.Controls)
            report.Add($"  button '{b.Text}' size           = {b.Width}x{b.Height}  (visible={b.Visible})");
        var po = root.Controls[2];   // _progressOuter (status=0, detail=1, progress=2)
        report.Add($"progressOuter size               = {po.Width}x{po.Height}");

        using var bmp = new Bitmap(clientSize.Width, clientSize.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, clientSize);
        string png = Path.Combine(outDir, $"UpdateMeasure-dpi{dpi}.png");
        bmp.Save(png, ImageFormat.Png);

        var bg = bmp.GetPixel(3, 3);
        bool IsBg(System.Drawing.Color p) =>
            Math.Abs(p.R - bg.R) + Math.Abs(p.G - bg.G) + Math.Abs(p.B - bg.B) <= 24;
        int trueBottom = -1;
        for (int y = clientSize.Height - 1; y >= 0 && trueBottom < 0; y--)
            for (int x = 0; x < clientSize.Width; x++)
                if (!IsBg(bmp.GetPixel(x, y))) { trueBottom = y; break; }
        int gutterPx = (clientSize.Height - 1) - trueBottom;
        report.Add("--- real pixel scan (CopyFromScreen) ---");
        report.Add($"bg sample (3,3)            = R{bg.R} G{bg.G} B{bg.B}");
        report.Add($"true content bottom row    = {trueBottom}px");
        report.Add($"bottom gutter              = {gutterPx}px  (>=0 means the button row is NOT clipped)");
        report.Add($"buttonRow.Bottom <= ClientSize.Height ? {(root.Top + buttonRow.Bottom <= clientSize.Height ? "YES (no clip)" : "NO — CLIPPED")}");
        report.Add($"-> SAVED {Path.GetFileName(png)}");

        // ── Long-error state: does a long status/detail WRAP (height grows) or
        // OVERFLOW the fixed-width column (horizontal clip)? This is the state the
        // populated capture above misses. A completeness audit flagged the AutoSize
        // labels as potentially non-wrapping; settle it with real measurements.
        form.DiagShowLongError();
        for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(15); }
        var errClient = form.ClientSize;
        var lblStatus = root.Controls[0];   // status=0, detail=1, progress=2, buttons=3
        var lblDetail = root.Controls[1];
        int contentW = errClient.Width - root.Padding.Left - root.Padding.Right;
        report.Add("--- long-error state (WRAP vs OVERFLOW) ---");
        report.Add($"ClientSize (error)               = {errClient.Width}x{errClient.Height}");
        report.Add($"content width (client - padding)  = {contentW}");
        report.Add($"lblStatus size                   = {lblStatus.Width}x{lblStatus.Height}  right={lblStatus.Right}  overflow={(lblStatus.Width > contentW ? "YES-CLIP" : "no(wrapped/fits)")}");
        report.Add($"lblDetail size                   = {lblDetail.Width}x{lblDetail.Height}  right={lblDetail.Right}  overflow={(lblDetail.Width > contentW ? "YES-CLIP" : "no(wrapped/fits)")}");
        report.Add($"any label right edge > client?   = {((lblStatus.Right > errClient.Width || lblDetail.Right > errClient.Width) ? "YES-CLIP" : "no")}");

        using var bmpE = new Bitmap(errClient.Width, errClient.Height);
        using (var g = Graphics.FromImage(bmpE))
            g.CopyFromScreen(form.PointToScreen(Point.Empty), Point.Empty, errClient);
        string pngE = Path.Combine(outDir, $"UpdateMeasureError-dpi{dpi}.png");
        bmpE.Save(pngE, ImageFormat.Png);
        report.Add($"-> SAVED {Path.GetFileName(pngE)}");

        form.Close();
        File.WriteAllLines(Path.Combine(outDir, "_measure-update-report.txt"), report);
    }

    private static Form CreateForm(string name, ConfigManager config) => name.ToLowerInvariant() switch
    {
        "settingsform" => new SettingsForm(config, null),
        "updatedialog" => new UpdateDialog(),
        "helpform"     => new HelpForm(),
        "osdform"      => OsdForm.CreateForDiag("Caps Lock: ON"),
        _ => throw new ArgumentException($"unknown form '{name}' (expected SettingsForm|UpdateDialog|HelpForm|OsdForm)"),
    };

    private static string Capture(Form form, string name, string outDir)
    {
        // Show offscreen so layout + per-monitor DPI scaling fully apply (these
        // happen on Show/OnLoad, not on bare handle creation). The VM has a
        // single monitor, so the offscreen location still resolves to its DPI.
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-32000, -32000);
        form.ShowInTaskbar = false;
        form.Show();
        Application.DoEvents();

        // UpdateDialog opens in a transient "Checking GitHub..." state; populate
        // a representative settled state so the rebuilt button row is visible.
        if (form is UpdateDialog ud) ud.DiagPopulate();
        Application.DoEvents();

        int dpi = form.DeviceDpi;
        Size client = form.ClientSize;
        using var bmp = new Bitmap(Math.Max(1, client.Width), Math.Max(1, client.Height));
        form.DrawToBitmap(bmp, new Rectangle(0, 0, client.Width, client.Height));

        string path = Path.Combine(outDir, $"{name}-dpi{dpi}.png");
        bmp.Save(path, ImageFormat.Png);
        form.Close();

        return $"{name}: DeviceDpi={dpi} ({dpi * 100 / 96}%) client={client.Width}x{client.Height} -> {Path.GetFileName(path)}";
    }

    private static string? GetArg(string[] args, string key)
    {
        int i = Array.IndexOf(args, key);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
#endif
