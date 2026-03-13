; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  CapsNumTray.ahk  —  Caps/Num/Scroll Lock tray indicators               ║
; ║  v1.4.1  |  Requires: AutoHotkey v2 64-bit                               ║
; ║                                                                          ║
; ║  • Left-click  Caps icon    → toggle Caps Lock                          ║
; ║  • Left-click  Num  icon    → toggle Num Lock                           ║
; ║  • Left-click  Scroll icon  → toggle Scroll Lock                        ║
; ║  • Right-click any          → menu (toggle / show-hide / startup / exit)║
; ║  Visibility prefs saved to CapsNumTray.ini (next to script)             ║
; ╚══════════════════════════════════════════════════════════════════════════╝

;@Ahk2Exe-AddResource icons\CapsLockOn.ico,    210
;@Ahk2Exe-AddResource icons\CapsLockOff.ico,   211
;@Ahk2Exe-AddResource icons\NumLockOn.ico,     212
;@Ahk2Exe-AddResource icons\NumLockOff.ico,    213
;@Ahk2Exe-AddResource icons\ScrollLockOn.ico,  214
;@Ahk2Exe-AddResource icons\ScrollLockOff.ico, 215
;@Ahk2Exe-AddResource icons\CapsLockOff_Light.ico,   216
;@Ahk2Exe-AddResource icons\NumLockOff_Light.ico,     217
;@Ahk2Exe-AddResource icons\ScrollLockOff_Light.ico,  218

#Requires AutoHotkey v2.0 64-bit
#SingleInstance Force
Persistent
#NoTrayIcon   ; suppress AHK's own icon — we manage ours manually

; ── VERSION ───────────────────────────────────────────────────────────────────
global g_version := "1.4.1"

; ── ICON IDs ──────────────────────────────────────────────────────────────────
global ID_CAPS   := 10
global ID_NUM    := 11
global ID_SCROLL := 12

; ── TRAY CALLBACK MESSAGE ─────────────────────────────────────────────────────
global WM_TRAY := 0x8010

; ── INI FILE (saved next to the script) ──────────────────────────────────────
global g_ini := A_ScriptDir "\CapsNumTray.ini"

; ── LOAD VISIBILITY PREFS FROM INI ───────────────────────────────────────────
global g_showCaps     := IniRead(g_ini, "Visibility", "ShowCaps",     "1") = "1"
global g_showNum      := IniRead(g_ini, "Visibility", "ShowNum",      "1") = "1"
global g_showScroll   := IniRead(g_ini, "Visibility", "ShowScroll",   "0") = "1"  ; opt-in
global g_showOSD      := IniRead(g_ini, "General",    "ShowOSD",      "1") = "1"
global g_beepOnToggle := IniRead(g_ini, "General",    "BeepOnToggle", "0") = "1"

; ── GUI STATE ────────────────────────────────────────────────────────────────
global g_settingsGui := 0
global g_helpGui     := 0

; ── DPI-AWARE ICON SIZE (per-monitor) ────────────────────────────────────────
; Uses GetDpiForWindow on the script's hidden HWND for per-monitor accuracy.
; Falls back to GetDpiForSystem if the per-monitor call fails (e.g., Win 8.1).
global g_iconSize := Round(16 * GetEffectiveDpi() / 96)

GetEffectiveDpi() {
    ; Per-monitor DPI via the script's owner window
    dpi := DllCall("GetDpiForWindow", "Ptr", A_ScriptHwnd, "UInt")
    if (dpi > 0)
        return dpi
    ; Fallback: system-wide DPI
    return DllCall("GetDpiForSystem", "UInt")
}

; ── THEME DETECTION ──────────────────────────────────────────────────────────
; Read Windows light/dark theme preference from registry.
; 1 = light theme (apps use light), 0 = dark theme.
; Light theme uses darker OFF icons so they remain visible on light taskbar.
global g_lightTheme := DetectLightTheme()

DetectLightTheme() {
    try {
        val := RegRead("HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme")
        return val = 1
    } catch
        return false  ; assume dark if registry key missing
}

; ── OWNERSHIP TRACKING ────────────────────────────────────────────────────────
; LoadImage (from file or resource) returns owned handles → must DestroyIcon
; LoadIcon  (system shared icons)   returns shared handles → must NOT DestroyIcon
global g_ownedIcons := Map()

; ── LOAD ICO FILES ────────────────────────────────────────────────────────────
; FIX P1-A: integer ordinals (32516=IDI_INFORMATION, 32515=IDI_WARNING)
; String ordinals like "IDI_WARNING" always return NULL from LoadIcon
global g_hCapOn     := LoadIco("CapsLockOn",    32516)   ; fallback: IDI_INFORMATION
global g_hCapOff    := LoadIco(g_lightTheme ? "CapsLockOff_Light"   : "CapsLockOff",   32515)   ; fallback: IDI_WARNING
global g_hNumOn     := LoadIco("NumLockOn",     32516)
global g_hNumOff    := LoadIco(g_lightTheme ? "NumLockOff_Light"    : "NumLockOff",    32515)
global g_hScrollOn  := LoadIco("ScrollLockOn",  32516)
global g_hScrollOff := LoadIco(g_lightTheme ? "ScrollLockOff_Light" : "ScrollLockOff", 32515)

; ── ADD TRAY ICONS ────────────────────────────────────────────────────────────
if g_showCaps
    TrayAdd(ID_CAPS)
if g_showNum
    TrayAdd(ID_NUM)
if g_showScroll
    TrayAdd(ID_SCROLL)

SyncIcons()
SetTimer(SyncIcons, 250)
OnMessage(WM_TRAY, OnTrayMsg)

; FIX P1-B: re-add icons if Explorer restarts
OnMessage(DllCall("RegisterWindowMessage", "Str", "TaskbarCreated", "UInt"), OnTaskbarCreated)

OnExit(Cleanup)

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Core functions                                                          ║
; ╚══════════════════════════════════════════════════════════════════════════╝

SyncIcons() {
    capOn    := GetKeyState("CapsLock",   "T")
    numOn    := GetKeyState("NumLock",    "T")
    scrollOn := GetKeyState("ScrollLock", "T")
    if g_showCaps
        TrayModify(ID_CAPS,   capOn    ? g_hCapOn     : g_hCapOff,    capOn    ? "Caps Lock: ON"   : "Caps Lock: OFF")
    if g_showNum
        TrayModify(ID_NUM,    numOn    ? g_hNumOn     : g_hNumOff,    numOn    ? "Num Lock: ON"    : "Num Lock: OFF")
    if g_showScroll
        TrayModify(ID_SCROLL, scrollOn ? g_hScrollOn  : g_hScrollOff, scrollOn ? "Scroll Lock: ON" : "Scroll Lock: OFF")
}

ToggleCapsLock() {
    newState := !GetKeyState("CapsLock", "T")
    SetCapsLockState(newState ? "On" : "Off")
    SyncIcons()
    if g_beepOnToggle
        SoundBeep(newState ? 880 : 440, 80)
    if g_showOSD {
        ToolTip("Caps Lock: " (newState ? "ON" : "OFF"))
        SetTimer(() => ToolTip(), -2000)
    }
}

ToggleNumLock() {
    newState := !GetKeyState("NumLock", "T")
    SetNumLockState(newState ? "On" : "Off")
    SyncIcons()
    if g_beepOnToggle
        SoundBeep(newState ? 1000 : 500, 80)
    if g_showOSD {
        ToolTip("Num Lock: " (newState ? "ON" : "OFF"))
        SetTimer(() => ToolTip(), -2000)
    }
}

ToggleScrollLock() {
    newState := !GetKeyState("ScrollLock", "T")
    SetScrollLockState(newState ? "On" : "Off")
    SyncIcons()
    if g_beepOnToggle
        SoundBeep(newState ? 1100 : 550, 80)
    if g_showOSD {
        ToolTip("Scroll Lock: " (newState ? "ON" : "OFF"))
        SetTimer(() => ToolTip(), -2000)
    }
}

; Show or hide an icon, save the pref to ini
SetIconVisible(id, visible) {
    ; Guard: refuse to hide the last visible icon
    if !visible {
        visibleCount := g_showCaps + g_showNum + g_showScroll
        if (visibleCount <= 1) {
            ToolTip("At least one icon must remain visible")
            SetTimer(() => ToolTip(), -3000)
            return
        }
    }
    if (id = ID_CAPS) {
        g_showCaps := visible
        IniWrite(visible ? "1" : "0", g_ini, "Visibility", "ShowCaps")
    } else if (id = ID_NUM) {
        g_showNum := visible
        IniWrite(visible ? "1" : "0", g_ini, "Visibility", "ShowNum")
    } else if (id = ID_SCROLL) {
        g_showScroll := visible
        IniWrite(visible ? "1" : "0", g_ini, "Visibility", "ShowScroll")
    }
    if visible {
        TrayAdd(id)
        SyncIcons()
    } else {
        TrayRemove(id)
    }
}

; ── VERSION_4 TRAY CALLBACK ───────────────────────────────────────────────────
; With NIM_SETVERSION (NOTIFYICON_VERSION_4):
;   lParam LOWORD = mouse event (WM_LBUTTONUP, WM_CONTEXTMENU, …)
;   lParam HIWORD = icon ID
;   wParam        = click coordinates (LOWORD=X, HIWORD=Y), sign-extended
OnTrayMsg(wParam, lParam, *) {
    event  := lParam & 0xFFFF
    iconID := (lParam >> 16) & 0xFFFF
    clickX := wParam & 0xFFFF
    clickY := (wParam >> 16) & 0xFFFF
    ; Sign-extend for negative coords on multi-monitor setups
    if (clickX > 32767)  clickX -= 65536
    if (clickY > 32767)  clickY -= 65536

    WM_LBUTTONUP   := 0x202
    WM_CONTEXTMENU := 0x007B

    if (event = WM_LBUTTONUP) {
        if      (iconID = ID_CAPS)   ToggleCapsLock()
        else if (iconID = ID_NUM)    ToggleNumLock()
        else if (iconID = ID_SCROLL) ToggleScrollLock()
        return
    }

    if (event != WM_CONTEXTMENU)
        return

    m := Menu()
    m.Add("CapsNumTray v" g_version, (*) => 0)
    m.Disable("CapsNumTray v" g_version)
    m.Add()

    if (iconID = ID_CAPS) {
        capOn := GetKeyState("CapsLock", "T")
        m.Add("Caps Lock is " (capOn ? "ON  — click to turn Off" : "OFF — click to turn On"), (*) => ToggleCapsLock())
        m.Add()
        if g_showNum
            m.Add("Hide Num Lock icon",    (*) => SetIconVisible(ID_NUM, false))
        else
            m.Add("Show Num Lock icon",    (*) => SetIconVisible(ID_NUM, true))
        if g_showScroll
            m.Add("Hide Scroll Lock icon", (*) => SetIconVisible(ID_SCROLL, false))
        else
            m.Add("Show Scroll Lock icon", (*) => SetIconVisible(ID_SCROLL, true))
    } else if (iconID = ID_NUM) {
        numOn := GetKeyState("NumLock", "T")
        m.Add("Num Lock is " (numOn ? "ON  — click to turn Off" : "OFF — click to turn On"), (*) => ToggleNumLock())
        m.Add()
        if g_showCaps
            m.Add("Hide Caps Lock icon",   (*) => SetIconVisible(ID_CAPS, false))
        else
            m.Add("Show Caps Lock icon",   (*) => SetIconVisible(ID_CAPS, true))
        if g_showScroll
            m.Add("Hide Scroll Lock icon", (*) => SetIconVisible(ID_SCROLL, false))
        else
            m.Add("Show Scroll Lock icon", (*) => SetIconVisible(ID_SCROLL, true))
    } else if (iconID = ID_SCROLL) {
        scrollOn := GetKeyState("ScrollLock", "T")
        m.Add("Scroll Lock is " (scrollOn ? "ON  — click to turn Off" : "OFF — click to turn On"), (*) => ToggleScrollLock())
        m.Add()
        if g_showCaps
            m.Add("Hide Caps Lock icon",   (*) => SetIconVisible(ID_CAPS, false))
        else
            m.Add("Show Caps Lock icon",   (*) => SetIconVisible(ID_CAPS, true))
        if g_showNum
            m.Add("Hide Num Lock icon",    (*) => SetIconVisible(ID_NUM, false))
        else
            m.Add("Show Num Lock icon",    (*) => SetIconVisible(ID_NUM, true))
    }

    m.Add()
    m.Add("Settings...", (*) => ShowSettingsGUI())
    m.Add()
    m.Add("Exit CapsNumTray", (*) => ExitApp())
    m.Show(clickX, clickY)
}

; FIX P1-B: re-add all icons after Explorer crash/restart
OnTaskbarCreated(*) {
    if g_showCaps   TrayAdd(ID_CAPS)
    if g_showNum    TrayAdd(ID_NUM)
    if g_showScroll TrayAdd(ID_SCROLL)
    SyncIcons()
}

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Settings toggles                                                        ║
; ╚══════════════════════════════════════════════════════════════════════════╝

ToggleOSD() {
    global g_showOSD := !g_showOSD
    IniWrite(g_showOSD ? "1" : "0", g_ini, "General", "ShowOSD")
}

ToggleBeep() {
    global g_beepOnToggle := !g_beepOnToggle
    IniWrite(g_beepOnToggle ? "1" : "0", g_ini, "General", "BeepOnToggle")
}

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Settings GUI                                                            ║
; ╚══════════════════════════════════════════════════════════════════════════╝

ShowSettingsGUI() {
    global g_settingsGui
    if g_settingsGui {
        try {
            g_settingsGui.Show()
            return
        }
        g_settingsGui := 0
    }

    dlg := Gui("+AlwaysOnTop", "CapsNumTray v" g_version " — Settings")
    dlg.BackColor := "FFFFFF"
    dlg.SetFont("s9", "Segoe UI")

    ; ── Tray Icons ──
    dlg.SetFont("s9 Bold")
    dlg.Add("Text", "x16 y16", "Tray Icons")
    dlg.SetFont("s9 Normal")
    dlg.Add("CheckBox", "x28 y+10 vChkShowCaps" (g_showCaps ? " Checked" : ""), "Show Caps Lock icon")
    dlg.Add("CheckBox", "x28 y+6 vChkShowNum" (g_showNum ? " Checked" : ""), "Show Num Lock icon")
    dlg.Add("CheckBox", "x28 y+6 vChkShowScroll" (g_showScroll ? " Checked" : ""), "Show Scroll Lock icon")

    ; ── Feedback ──
    dlg.SetFont("s9 Bold")
    dlg.Add("Text", "x16 y+16", "Feedback")
    dlg.SetFont("s9 Normal")
    dlg.Add("CheckBox", "x28 y+10 vChkOSD" (g_showOSD ? " Checked" : ""), "Show OSD tooltip on toggle")
    dlg.Add("CheckBox", "x28 y+6 vChkBeep" (g_beepOnToggle ? " Checked" : ""), "Beep on toggle")

    ; ── Startup ──
    dlg.SetFont("s9 Bold")
    dlg.Add("Text", "x16 y+16", "Startup")
    dlg.SetFont("s9 Normal")
    dlg.Add("CheckBox", "x28 y+10 vChkStartup" (IsStartupEnabled() ? " Checked" : ""), "Run at Windows startup")

    ; ── Buttons ──
    dlg.Add("Button", "x16 y+22 w80", "GitHub").OnEvent("Click", (*) => Run("https://github.com/itsnateai/CaplockNumlock"))
    dlg.Add("Button", "x+6 yp w55", "Help").OnEvent("Click", (*) => ShowHelpWindow())
    dlg.Add("Button", "x200 yp w70 Default", "OK").OnEvent("Click", (*) => ApplySettingsGUI(dlg, true))
    dlg.Add("Button", "x+8 w70", "Apply").OnEvent("Click", (*) => ApplySettingsGUI(dlg, false))
    dlg.Add("Button", "x+8 w70", "Cancel").OnEvent("Click", (*) => CloseSettingsGUI(dlg))

    dlg.OnEvent("Close", (*) => CloseSettingsGUI(dlg))
    g_settingsGui := dlg
    dlg.Show("AutoSize")
}

ApplySettingsGUI(dlg, close := true) {
    global g_showCaps, g_showNum, g_showScroll, g_showOSD, g_beepOnToggle, g_settingsGui
    saved := dlg.Submit(close)

    ; ── Visibility (with last-icon guard) ──
    newCaps   := saved.ChkShowCaps
    newNum    := saved.ChkShowNum
    newScroll := saved.ChkShowScroll
    if !newCaps && !newNum && !newScroll {
        ToolTip("At least one icon must remain visible")
        SetTimer(() => ToolTip(), -3000)
        if close {
            dlg.Show()  ; re-show since Submit() hid it
        }
        return
    }
    if (newCaps != g_showCaps)
        SetIconVisible(ID_CAPS, newCaps)
    if (newNum != g_showNum)
        SetIconVisible(ID_NUM, newNum)
    if (newScroll != g_showScroll)
        SetIconVisible(ID_SCROLL, newScroll)

    ; ── OSD & Beep ──
    g_showOSD := saved.ChkOSD
    IniWrite(g_showOSD ? "1" : "0", g_ini, "General", "ShowOSD")
    g_beepOnToggle := saved.ChkBeep
    IniWrite(g_beepOnToggle ? "1" : "0", g_ini, "General", "BeepOnToggle")

    ; ── Startup ──
    wantStartup := saved.ChkStartup
    hasStartup  := IsStartupEnabled()
    if (wantStartup != hasStartup)
        ToggleStartup()

    if close {
        dlg.Destroy()
        g_settingsGui := 0
    }
    ToolTip("Settings saved.")
    SetTimer(() => ToolTip(), -3000)
}

CloseSettingsGUI(dlg) {
    global g_settingsGui
    try dlg.Destroy()
    g_settingsGui := 0
}

CloseHelpWindow() {
    global g_helpGui
    try g_helpGui.Destroy()
    g_helpGui := 0
}

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Help window                                                             ║
; ╚══════════════════════════════════════════════════════════════════════════╝

ShowHelpWindow() {
    global g_helpGui
    if g_helpGui {
        try {
            g_helpGui.Show()
            return
        }
        g_helpGui := 0
    }
    hlp := Gui("+AlwaysOnTop +Resize +MinSize400x300", "CapsNumTray v" g_version " — Help")
    hlp.BackColor := "FFFFFF"
    hlp.SetFont("s9", "Segoe UI")

    helpText := "
    (
CAPSNUMTRAY — Caps/Num/Scroll Lock Tray Indicators

CapsNumTray adds independent system tray icons that show the current state of your Caps Lock, Num Lock, and Scroll Lock keys. Left-click to toggle, right-click for options.

Green/lit icon = key is ON
Dim/grey icon = key is OFF

─── BASIC USAGE ─────────────────────────────────

• Left-click the Caps Lock tray icon to toggle Caps Lock.
• Left-click the Num Lock tray icon to toggle Num Lock.
• Left-click the Scroll Lock tray icon to toggle Scroll Lock.
• Right-click any icon for a menu with toggle, visibility, settings, and exit.

─── SETTINGS ────────────────────────────────────

Show Caps Lock / Num Lock / Scroll Lock icon: Choose which icons appear in the tray. Scroll Lock is hidden by default (opt-in via Settings). At least one must remain visible.

Show OSD tooltip on toggle: When enabled, a small floating tooltip appears briefly (2 seconds) showing the new state after toggling.

Beep on toggle: Plays a short tone when you toggle a key. Higher pitch = ON, lower pitch = OFF.

Run at Windows startup: Adds CapsNumTray to your Windows startup so it launches automatically at login.

All settings are saved to CapsNumTray.ini next to the script and persist across restarts.

─── TRAY ICONS ──────────────────────────────────

Icons are loaded from the icons/ folder. If missing, compiled .exe versions use embedded resources. As a final fallback, Windows built-in system icons are used.

─── TECHNICAL NOTES ─────────────────────────────

CapsNumTray uses the Win32 Shell_NotifyIconW API directly (not AHK's built-in tray) to support multiple independent tray icons. A 250ms polling timer keeps icons in sync even when keys are changed externally. Icons are automatically re-added if Explorer restarts.
    )"

    hlp.Add("Edit", "x10 y10 w440 h400 ReadOnly -E0x200 Multi +VScroll", helpText)
    hlp.OnEvent("Close", (*) => CloseHelpWindow())
    hlp.OnEvent("Size", HelpResize)
    g_helpGui := hlp
    hlp.Show("w460 h420")
}

HelpResize(hlp, minMax, w, h) {
    if minMax = -1
        return
    try hlp["Edit1"].Move(10, 10, w - 20, h - 20)
}

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Startup management                                                      ║
; ╚══════════════════════════════════════════════════════════════════════════╝

IsStartupEnabled() {
    try {
        val := RegRead("HKCU\Software\Microsoft\Windows\CurrentVersion\Run", "CapsNumTray")
        ; Verify the registered path matches the current script location
        expected := A_IsCompiled ? A_ScriptFullPath
                                 : '"' A_AhkPath '" "' A_ScriptFullPath '"'
        return val = expected
    } catch
        return false
}

ToggleStartup() {
    if IsStartupEnabled() {
        RegDelete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run", "CapsNumTray"
    } else {
        ; Compiled .exe can launch directly; .ahk needs the AHK interpreter path
        target := A_IsCompiled ? A_ScriptFullPath
                               : '"' A_AhkPath '" "' A_ScriptFullPath '"'
        RegWrite target, "REG_SZ", "HKCU\Software\Microsoft\Windows\CurrentVersion\Run", "CapsNumTray"
    }
}

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Shell_NotifyIconW helpers                                               ║
; ╚══════════════════════════════════════════════════════════════════════════╝

BuildNID(id, hIcon := 0, tip := "") {
    nid := Buffer(976, 0)
    NumPut("UInt", 976,          nid,  0)
    NumPut("Ptr",  A_ScriptHwnd, nid,  8)
    NumPut("UInt", id,           nid, 16)
    ; FIX P2-B: 0x87 = NIF_MESSAGE|NIF_ICON|NIF_TIP|NIF_SHOWTIP
    ; NIF_SHOWTIP (0x80) required with NOTIFYICON_VERSION_4 for szTip to display
    NumPut("UInt", 0x87,         nid, 20)
    NumPut("UInt", WM_TRAY,      nid, 24)
    NumPut("Ptr",  hIcon,        nid, 32)
    StrPut(tip, nid.Ptr + 40, 128, "UTF-16")
    return nid
}

TrayAdd(id) {
    ; FIX P2-A: NIF_MESSAGE only (0x1) on NIM_ADD — avoids blank icon flash
    ; icon/tip set immediately after via SyncIcons → TrayModify
    nid := Buffer(976, 0)
    NumPut("UInt", 976,          nid,  0)
    NumPut("Ptr",  A_ScriptHwnd, nid,  8)
    NumPut("UInt", id,           nid, 16)
    NumPut("UInt", 0x1,          nid, 20)   ; NIF_MESSAGE only
    NumPut("UInt", WM_TRAY,      nid, 24)
    ret := DllCall("Shell32\Shell_NotifyIconW", "UInt", 0, "Ptr", nid, "Int")  ; NIM_ADD
    if ret {
        ; Activate NOTIFYICON_VERSION_4 protocol: lParam=event|iconID, wParam=clickXY
        NumPut("UInt", 4, nid, 816)   ; uVersion = NOTIFYICON_VERSION_4
        DllCall("Shell32\Shell_NotifyIconW", "UInt", 4, "Ptr", nid, "Int")     ; NIM_SETVERSION
    }
}

TrayModify(id, hIcon, tip) {
    nid := BuildNID(id, hIcon, tip)
    DllCall("Shell32\Shell_NotifyIconW", "UInt", 1, "Ptr", nid, "Int")  ; NIM_MODIFY
}

TrayRemove(id) {
    nid := Buffer(976, 0)
    NumPut("UInt", 976,          nid,  0)
    NumPut("Ptr",  A_ScriptHwnd, nid,  8)
    NumPut("UInt", id,           nid, 16)
    DllCall("Shell32\Shell_NotifyIconW", "UInt", 2, "Ptr", nid, "Int")  ; NIM_DELETE
}

; FIX P1-A + P1-C + P1-D: 3-stage fallback with ownership tracking
; Stage 1: .ico file on disk (owned handle)
; Stage 2: embedded PE resource when compiled (owned handle)
; Stage 3: shared Windows system icon (NOT owned — do not DestroyIcon)
LoadIco(name, fallbackOrdinal := 32512) {
    ; Stage 1: file on disk (icons/ subfolder)
    path  := A_ScriptDir "\icons\" name ".ico"
    hIcon := DllCall("LoadImage", "Ptr", 0, "WStr", path,
        "UInt", 1, "Int", g_iconSize, "Int", g_iconSize, "UInt", 0x10, "Ptr")
    if hIcon {
        g_ownedIcons[hIcon] := true
        return hIcon
    }
    ; Stage 2: embedded PE resource (compiled .exe only)
    if A_IsCompiled {
        resIDs := Map("CapsLockOn", 210, "CapsLockOff", 211, "NumLockOn", 212, "NumLockOff", 213, "ScrollLockOn", 214, "ScrollLockOff", 215, "CapsLockOff_Light", 216, "NumLockOff_Light", 217, "ScrollLockOff_Light", 218)
        if resIDs.Has(name) {
            hInst := DllCall("GetModuleHandle", "Ptr", 0, "Ptr")
            hIcon := DllCall("LoadImage", "Ptr", hInst,
                "UPtr", resIDs[name], "UInt", 1,
                "Int", g_iconSize, "Int", g_iconSize, "UInt", 0, "Ptr")
            if hIcon {
                g_ownedIcons[hIcon] := true
                return hIcon
            }
        }
    }
    ; Stage 3: shared system icon — integer ordinal required (string ordinals return NULL)
    return DllCall("LoadIcon", "Ptr", 0, "UPtr", fallbackOrdinal, "Ptr")
}

Cleanup(*) {
    if g_showCaps   TrayRemove(ID_CAPS)
    if g_showNum    TrayRemove(ID_NUM)
    if g_showScroll TrayRemove(ID_SCROLL)
    ; FIX P1-D: only DestroyIcon handles we own (not shared system icons)
    for h in [g_hCapOn, g_hCapOff, g_hNumOn, g_hNumOff, g_hScrollOn, g_hScrollOff]
        if h && g_ownedIcons.Has(h)
            DllCall("DestroyIcon", "Ptr", h)
}
