namespace CapsNumTray;

/// <summary>
/// Simple INI file reader/writer compatible with AHK's IniRead/IniWrite format.
/// </summary>
internal sealed class ConfigManager
{
    private static readonly System.Text.Encoding Utf8NoBom = new System.Text.UTF8Encoding(false);
    private readonly string _iniPath;

    public bool ShowCaps { get; set; } = true;
    public bool ShowNum { get; set; } = true;
    public bool ShowScroll { get; set; } // false by default (opt-in)
    public bool ShowOSD { get; set; } = true;
    public bool BeepOnToggle { get; set; }
    public int PollInterval { get; set; } // seconds, 0 = disabled (default), max 300 (5 min)

    // "System" (default — follow Windows), "Dark", or "Light". Unknown values
    // resolve to System via Theme.ResolveIsDark. Affects window chrome only
    // (Settings, Help, Update, OSD, context menu) — tray icons always follow
    // the OS theme regardless of this setting.
    public string ThemeMode { get; set; } = "System";

    public ConfigManager(string iniPath)
    {
        _iniPath = iniPath;
        Load();
    }

    public void Load()
    {
        if (!File.Exists(_iniPath))
            return;

        try
        {
            string[] lines = File.ReadAllLines(_iniPath, Utf8NoBom);
            string currentSection = "";

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == ';')
                    continue;

                if (line[0] == '[' && line[^1] == ']')
                {
                    currentSection = line[1..^1];
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq < 0) continue;

                string key = line[..eq].Trim();
                string val = line[(eq + 1)..].Trim();

                // Section names are matched case-insensitively so hand-edited
                // [appearance], [VISIBILITY], etc. still parse — Windows INI
                // convention is case-insensitive section headers and the v2.4.6
                // verifier swarm flagged the previous case-sensitive switch as
                // a silent drop-on-typo. Key matches stay case-sensitive (matches
                // AHK IniRead behaviour for the legacy AHK-format compatibility).
                switch (currentSection.ToLowerInvariant())
                {
                    case "visibility":
                        if (key == "ShowCaps") ShowCaps = val == "1";
                        else if (key == "ShowNum") ShowNum = val == "1";
                        else if (key == "ShowScroll") ShowScroll = val == "1";
                        break;
                    case "general":
                        if (key == "ShowOSD") ShowOSD = val == "1";
                        else if (key == "BeepOnToggle") BeepOnToggle = val == "1";
                        else if (key == "PollInterval" && int.TryParse(val, out int pi))
                            PollInterval = Math.Clamp(pi, 0, 300);
                        break;
                    case "appearance":
                        if (key == "ThemeMode")
                        {
                            // Case-insensitive value match + canonical-case storage.
                            // Theme.ResolveIsDark uses OrdinalIgnoreCase, but the
                            // SettingsForm dropdown's IndexOf is case-sensitive
                            // against the canonical {"System","Dark","Light"} —
                            // storing canonical here keeps the dropdown's selection
                            // honest when the INI was hand-edited as `themeMode=dark`.
                            if (string.Equals(val, "Dark", System.StringComparison.OrdinalIgnoreCase))
                                ThemeMode = "Dark";
                            else if (string.Equals(val, "Light", System.StringComparison.OrdinalIgnoreCase))
                                ThemeMode = "Light";
                            else if (string.Equals(val, "System", System.StringComparison.OrdinalIgnoreCase))
                                ThemeMode = "System";
                            else
                            {
                                // Unknown value falls through; keep current ThemeMode.
                                // Log so a hand-edited typo (`ThemeMode=Mauve`) doesn't
                                // become a "why isn't my theme sticking" mystery.
                                System.Diagnostics.Trace.WriteLine(
                                    $"CapsNumTray: ConfigManager unknown ThemeMode value '{val}' — keeping '{ThemeMode}'");
                            }
                        }
                        break;
                }
            }
        }
        catch (System.Exception ex)
        {
            // Defaults preserved on locked/corrupt file — log so a chronically
            // locked INI surfaces in Trace listeners instead of being a silent
            // "why aren't my settings sticking" bug. Matches the Trace.WriteLine
            // convention used in TrayApplication.ShellNotify for Shell_NotifyIconW
            // failures.
            System.Diagnostics.Trace.WriteLine(
                $"CapsNumTray: ConfigManager.Load failed (path={_iniPath}, err={ex.GetType().Name}: {ex.Message}) — using defaults");
        }
    }

    public void Save()
    {
        string content =
            "[Visibility]\r\n" +
            $"ShowCaps={B(ShowCaps)}\r\n" +
            $"ShowNum={B(ShowNum)}\r\n" +
            $"ShowScroll={B(ShowScroll)}\r\n" +
            "\r\n[General]\r\n" +
            $"ShowOSD={B(ShowOSD)}\r\n" +
            $"BeepOnToggle={B(BeepOnToggle)}\r\n" +
            $"PollInterval={PollInterval}\r\n" +
            "\r\n[Appearance]\r\n" +
            $"ThemeMode={ThemeMode}\r\n";

        // Atomic save: write to temp then rename. Protects against power loss or
        // process kill leaving a half-written INI that reverts all settings.
        // Unique suffix avoids collision if two Save() calls ever overlap.
        string tmpPath = $"{_iniPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tmpPath, content, Utf8NoBom);
            File.Move(tmpPath, _iniPath, overwrite: true);
        }
        catch (System.Exception ex)
        {
            // Best-effort cleanup of the temp + log the failure so a read-only
            // install dir (e.g. Program Files without elevation) doesn't quietly
            // discard every settings save. The OSD will still say "Settings
            // saved" — fixing that loudly requires bubbling this up, which is
            // out of scope here. At minimum a Trace listener catches it.
            System.Diagnostics.Trace.WriteLine(
                $"CapsNumTray: ConfigManager.Save failed (path={_iniPath}, err={ex.GetType().Name}: {ex.Message})");
            try { File.Delete(tmpPath); }
            catch (System.Exception delEx)
            {
                // Orphan .tmp files accumulate on locked-dir installs (read-only
                // Program Files without elevation) — log so a janitor task or
                // future cleanup pass can find the breadcrumbs.
                System.Diagnostics.Trace.WriteLine(
                    $"CapsNumTray: ConfigManager.Save temp cleanup failed (tmp={tmpPath}, err={delEx.GetType().Name}: {delEx.Message})");
            }
        }
    }

    private static string B(bool v) => v ? "1" : "0";
}
