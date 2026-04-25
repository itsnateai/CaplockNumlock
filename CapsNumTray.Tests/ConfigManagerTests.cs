namespace CapsNumTray.Tests;

[TestClass]
public class ConfigManagerTests
{
    private string _tempDir = null!;
    private string IniPath => Path.Combine(_tempDir, "CapsNumTray.ini");

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CapsNumTray_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [TestMethod]
    public void NoIniFile_LoadsDefaults()
    {
        // Missing file = first run. Defaults: Caps + Num shown, Scroll opt-in,
        // OSD on, no beep, no polling.
        var cfg = new ConfigManager(IniPath);
        cfg.Load();

        Assert.IsTrue(cfg.ShowCaps, "ShowCaps default should be true");
        Assert.IsTrue(cfg.ShowNum, "ShowNum default should be true");
        Assert.IsFalse(cfg.ShowScroll, "ShowScroll is opt-in (false default)");
        Assert.IsTrue(cfg.ShowOSD, "ShowOSD default should be true");
        Assert.IsFalse(cfg.BeepOnToggle, "BeepOnToggle default should be false");
        Assert.AreEqual(0, cfg.PollInterval, "PollInterval default should be 0 (disabled)");
    }

    [TestMethod]
    public void SaveThenLoad_RoundTripsValues()
    {
        var written = new ConfigManager(IniPath)
        {
            ShowCaps = false,
            ShowNum = true,
            ShowScroll = true,
            ShowOSD = false,
            BeepOnToggle = true,
            PollInterval = 30,
        };
        written.Save();

        Assert.IsTrue(File.Exists(IniPath), "Save should produce an INI file");

        var loaded = new ConfigManager(IniPath);
        loaded.Load();

        Assert.IsFalse(loaded.ShowCaps);
        Assert.IsTrue(loaded.ShowNum);
        Assert.IsTrue(loaded.ShowScroll);
        Assert.IsFalse(loaded.ShowOSD);
        Assert.IsTrue(loaded.BeepOnToggle);
        Assert.AreEqual(30, loaded.PollInterval);
    }

    [TestMethod]
    public void Save_WritesUtf8WithoutBom()
    {
        // UTF-8 BOM at the head of an AHK INI breaks AHK's IniRead parser, so
        // the C# port writes plain UTF-8 to stay compatible with the legacy
        // AHK script that may still be parsing the same file.
        var cfg = new ConfigManager(IniPath) { ShowCaps = true };
        cfg.Save();

        var bytes = File.ReadAllBytes(IniPath);
        Assert.IsTrue(bytes.Length >= 3, "INI should have content");
        bool hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        Assert.IsFalse(hasBom, "INI must NOT start with a UTF-8 BOM");
    }

    [TestMethod]
    public void Load_TreatsLockedOrUnreadableFileAsDefaults()
    {
        // Write a deliberately unparseable INI. The file is readable but the
        // section/key parser should fall back to defaults rather than throw.
        File.WriteAllText(IniPath, "this is not valid ini content @#$%^&*\n[unclosed\nbroken=key=value\n");

        var cfg = new ConfigManager(IniPath);
        cfg.Load(); // must not throw

        // Defaults preserved on garbled file.
        Assert.IsTrue(cfg.ShowCaps);
        Assert.IsTrue(cfg.ShowNum);
    }
}
