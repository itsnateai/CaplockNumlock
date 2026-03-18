using System.Runtime.InteropServices;

namespace CapsNumTray;

/// <summary>
/// Manages .lnk shortcut in the user's Startup folder.
/// Uses WScript.Shell COM for shortcut creation.
/// </summary>
internal static class StartupManager
{
    private static string ShortcutPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "CapsNumTray.lnk");

    public static bool IsEnabled => File.Exists(ShortcutPath);

    public static void Toggle()
    {
        if (IsEnabled)
        {
            try { File.Delete(ShortcutPath); } catch { }
        }
        else
        {
            CreateShortcut();
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled) return;
        Toggle();
    }

    private static void CreateShortcut()
    {
        object? shell = null;
        object? shortcut = null;
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;
            shell = Activator.CreateInstance(shellType);
            if (shell == null) return;

            dynamic dynShell = shell;
            shortcut = dynShell.CreateShortcut(ShortcutPath);
            dynamic sc = shortcut;
            sc.TargetPath = Environment.ProcessPath;
            sc.WorkingDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? "";
            sc.Save();
        }
        catch
        {
            // Silently fail if COM not available
        }
        finally
        {
            if (shortcut != null)
                Marshal.ReleaseComObject(shortcut);
            if (shell != null)
                Marshal.ReleaseComObject(shell);
        }
    }
}
