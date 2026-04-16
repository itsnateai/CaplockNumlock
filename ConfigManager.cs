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

                switch (currentSection)
                {
                    case "Visibility":
                        if (key == "ShowCaps") ShowCaps = val == "1";
                        else if (key == "ShowNum") ShowNum = val == "1";
                        else if (key == "ShowScroll") ShowScroll = val == "1";
                        break;
                    case "General":
                        if (key == "ShowOSD") ShowOSD = val == "1";
                        else if (key == "BeepOnToggle") BeepOnToggle = val == "1";
                        else if (key == "PollInterval" && int.TryParse(val, out int pi))
                            PollInterval = Math.Clamp(pi, 0, 300);
                        break;
                }
            }
        }
        catch
        {
            // Graceful default on locked/corrupt file
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
            $"PollInterval={PollInterval}\r\n";

        // Atomic save: write to temp then rename. Protects against power loss or
        // process kill leaving a half-written INI that reverts all settings.
        string tmpPath = _iniPath + ".tmp";
        try
        {
            File.WriteAllText(tmpPath, content, Utf8NoBom);
            File.Move(tmpPath, _iniPath, overwrite: true);
        }
        catch
        {
            // Silently fail if file is locked; best-effort cleanup of temp.
            try { File.Delete(tmpPath); } catch { }
        }
    }

    private static string B(bool v) => v ? "1" : "0";
}
