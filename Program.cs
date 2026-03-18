namespace CapsNumTray;

internal static class Program
{
    [STAThread]
    static void Main()
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

        ApplicationConfiguration.Initialize();
        using var app = new TrayApplication();
        Application.Run(app);
    }
}
