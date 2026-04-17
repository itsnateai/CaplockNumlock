using System.Reflection;

namespace CapsNumTray;

/// <summary>
/// Loads and caches icon handles. Tracks ownership for cleanup.
/// 3-stage fallback: embedded resource → .ico file on disk → system icon.
/// </summary>
internal sealed class IconManager : IDisposable
{
    private readonly int _iconSize;
    private readonly HashSet<nint> _ownedHandles = new();
    private bool _disposed;

    public nint CapsOn { get; }
    public nint CapsOff { get; private set; }
    public nint NumOn { get; }
    public nint NumOff { get; private set; }
    public nint ScrollOn { get; }
    public nint ScrollOff { get; private set; }

    public IconManager(nint windowHandle, bool lightTheme)
    {
        _iconSize = GetDpiAwareIconSize(windowHandle);

        CapsOn = LoadIcon("CapsLockOn", 32516);
        CapsOff = LoadIcon(lightTheme ? "CapsLockOff_Light" : "CapsLockOff", 32515);
        NumOn = LoadIcon("NumLockOn", 32516);
        NumOff = LoadIcon(lightTheme ? "NumLockOff_Light" : "NumLockOff", 32515);
        ScrollOn = LoadIcon("ScrollLockOn", 32516);
        ScrollOff = LoadIcon(lightTheme ? "ScrollLockOff_Light" : "ScrollLockOff", 32515);
    }

    // Reload the theme-dependent OFF icons when the system theme flips
    // between light and dark at runtime. ON icons are theme-independent.
    public void ReloadForTheme(bool lightTheme)
    {
        DisposeIfOwned(CapsOff);
        DisposeIfOwned(NumOff);
        DisposeIfOwned(ScrollOff);
        CapsOff = LoadIcon(lightTheme ? "CapsLockOff_Light" : "CapsLockOff", 32515);
        NumOff = LoadIcon(lightTheme ? "NumLockOff_Light" : "NumLockOff", 32515);
        ScrollOff = LoadIcon(lightTheme ? "ScrollLockOff_Light" : "ScrollLockOff", 32515);
    }

    private void DisposeIfOwned(nint h)
    {
        if (_ownedHandles.Remove(h))
            NativeMethods.DestroyIcon(h);
    }

    private static int GetDpiAwareIconSize(nint windowHandle)
    {
        uint dpi = NativeMethods.GetDpiForWindow(windowHandle);
        if (dpi == 0)
            dpi = NativeMethods.GetDpiForSystem();
        if (dpi == 0)
            dpi = 96;
        return (int)Math.Round(16.0 * dpi / 96.0);
    }

    /// <summary>
    /// 3-stage icon loading:
    /// 1. Embedded resource (works in published exe)
    /// 2. .ico file on disk (dev/source mode)
    /// 3. System fallback icon
    /// </summary>
    private nint LoadIcon(string name, int fallbackOrdinal)
    {
        // Stage 1: Embedded resource
        nint h = LoadFromEmbeddedResource(name);
        if (h != 0) return h;

        // Stage 2: File on disk
        h = LoadFromFile(name);
        if (h != 0) return h;

        // Stage 3: System icon (shared handle — do NOT track for destruction)
        return NativeMethods.LoadIcon(0, (nint)fallbackOrdinal);
    }

    private nint LoadFromEmbeddedResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(name + ".ico");
        if (stream == null) return 0;

        // Write to a temp file, then LoadImage for correct sizing.
        // Use random filename to prevent TOCTOU race (predictable path = plantable).
        string tempPath = Path.Combine(Path.GetTempPath(), $"CapsNumTray_{Guid.NewGuid():N}.ico");
        try
        {
            using (var fs = File.Create(tempPath))
                stream.CopyTo(fs);

            nint h = NativeMethods.LoadImage(0, tempPath, NativeMethods.IMAGE_ICON,
                _iconSize, _iconSize, NativeMethods.LR_LOADFROMFILE);
            if (h != 0)
                _ownedHandles.Add(h);
            return h;
        }
        catch
        {
            return 0;
        }
        finally
        {
            try { File.Delete(tempPath); } catch { }
        }
    }

    private nint LoadFromFile(string name)
    {
        string? exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (exeDir == null) return 0;

        string path = Path.Combine(exeDir, "icons", name + ".ico");

        // No File.Exists guard — LoadImage returns 0 on missing/inaccessible files,
        // and adding a check would widen the TOCTOU window for no benefit.
        nint h = NativeMethods.LoadImage(0, path, NativeMethods.IMAGE_ICON,
            _iconSize, _iconSize, NativeMethods.LR_LOADFROMFILE);
        if (h != 0)
            _ownedHandles.Add(h);
        return h;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (nint h in _ownedHandles)
        {
            if (h != 0)
                NativeMethods.DestroyIcon(h);
        }
        _ownedHandles.Clear();
    }
}
