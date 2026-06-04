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
