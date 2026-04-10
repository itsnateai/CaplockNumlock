namespace CapsNumTray;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Single-instance: kill previous instances
        string processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "CapsNumTray");
        foreach (var p in System.Diagnostics.Process.GetProcessesByName(processName))
        {
            using (p)
            {
                if (p.Id != Environment.ProcessId)
                {
                    try { p.Kill(); } catch { }
                }
            }
        }

        bool isAfterUpdate = args.Contains("--after-update");
        UpdateDialog.CleanupUpdateArtifacts();

        ApplicationConfiguration.Initialize();

        if (isAfterUpdate)
            UpdateDialog.ShowUpdateToast();

        using var app = new TrayApplication();
        Application.Run(app);
    }
}
