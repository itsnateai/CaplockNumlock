namespace CapsNumTray;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Single-instance: hold mutex for lifetime
        using var mutex = new Mutex(true, @"Global\CapsNumTray_SingleInstance", out _);

        bool isAfterUpdate = args.Contains("--after-update");
        UpdateDialog.CleanupUpdateArtifacts();

        // Self-heal startup shortcut if exe path has changed (e.g., winget upgrade)
        StartupManager.ValidateStartupPath();

        ApplicationConfiguration.Initialize();

        if (isAfterUpdate)
            UpdateDialog.ShowUpdateToast();

        using var app = new TrayApplication();
        Application.Run(app);
    }
}
