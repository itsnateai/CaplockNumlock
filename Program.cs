namespace CapsNumTray;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        bool isAfterUpdate = args.Contains("--after-update");

        // Single-instance: hold mutex for lifetime. Post-update the outgoing exe
        // needs a moment to release the mutex, so retry briefly in that case.
        // Local\ scope — the tray is inherently per-session (icons live in the
        // session's explorer.exe), so Global\ would let user A block user B on
        // multi-session machines (RDS, fast user switching).
        Mutex mutex;
        bool createdNew;
        int remainingRetries = isAfterUpdate ? 50 : 0;
        while (true)
        {
            try
            {
                mutex = new Mutex(true, @"Local\CapsNumTray_SingleInstance", out createdNew);
            }
            catch (UnauthorizedAccessException)
            {
                // Session 0 / AppContainer / restricted DACL — decline gracefully.
                return;
            }
            if (createdNew || remainingRetries-- <= 0) break;
            mutex.Dispose();
            Thread.Sleep(100);
        }
        using var _mutex = mutex;
        if (!createdNew)
            return;

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
