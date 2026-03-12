; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  CapsNumTray.ahk  —  Caps Lock + Num Lock tray indicators               ║
; ║  v1.2.0  |  Requires: AutoHotkey v2 64-bit                               ║
; ║                                                                          ║
; ║  • Left-click  Caps icon  → toggle Caps Lock                            ║
; ║  • Left-click  Num  icon  → toggle Num Lock                             ║
; ║  • Right-click either     → menu (toggle / show-hide / startup / exit)  ║
; ║  Visibility prefs saved to CapsNumTray.ini (next to script)             ║
; ╚══════════════════════════════════════════════════════════════════════════╝

;@Ahk2Exe-AddResource CapsLockOn.ico,  210
;@Ahk2Exe-AddResource CapsLockOff.ico, 211
;@Ahk2Exe-AddResource NumLockOn.ico,   212
;@Ahk2Exe-AddResource NumLockOff.ico,  213

#Requires AutoHotkey v2.0 64-bit
#SingleInstance Force
Persistent
#NoTrayIcon   ; suppress AHK's own icon — we manage ours manually

; ── VERSION ───────────────────────────────────────────────────────────────────
global g_version := "1.2.0"

; ── ICON IDs ──────────────────────────────────────────────────────────────────
global ID_CAPS := 10
global ID_NUM  := 11

; ── TRAY CALLBACK MESSAGE ─────────────────────────────────────────────────────
global WM_TRAY := 0x8010

; ── INI FILE (saved next to the script) ──────────────────────────────────────
global g_ini := A_ScriptDir "\CapsNumTray.ini"

; ── LOAD VISIBILITY PREFS FROM INI ───────────────────────────────────────────
global g_showCaps     := IniRead(g_ini, "Visibility", "ShowCaps",     "1") = "1"
global g_showNum      := IniRead(g_ini, "Visibility", "ShowNum",      "1") = "1"
global g_showOSD      := IniRead(g_ini, "General",    "ShowOSD",      "1") = "1"
global g_beepOnToggle := IniRead(g_ini, "General",    "BeepOnToggle", "0") = "1"

; ── DPI-AWARE ICON SIZE ───────────────────────────────────────────────────────
; 16px at 96 DPI, scales to 20px at 125%, 24px at 150%, etc.
global g_iconSize := Round(16 * DllCall("GetDpiForSystem", "UInt") / 96)

; ── OWNERSHIP TRACKING ────────────────────────────────────────────────────────
; LoadImage (from file or resource) returns owned handles → must DestroyIcon
; LoadIcon  (system shared icons)   returns shared handles → must NOT DestroyIcon
global g_ownedIcons := Map()

; ── LOAD ICO FILES ────────────────────────────────────────────────────────────
; FIX P1-A: integer ordinals (32516=IDI_INFORMATION, 32515=IDI_WARNING)
; String ordinals like "IDI_WARNING" always return NULL from LoadIcon
global g_hCapOn  := LoadIco("CapsLockOn",  32516)   ; fallback: IDI_INFORMATION
global g_hCapOff := LoadIco("CapsLockOff", 32515)   ; fallback: IDI_WARNING
global g_hNumOn  := LoadIco("NumLockOn",   32516)
global g_hNumOff := LoadIco("NumLockOff",  32515)

; ── ADD TRAY ICONS ────────────────────────────────────────────────────────────
if g_showCaps
    TrayAdd(ID_CAPS)
if g_showNum
    TrayAdd(ID_NUM)

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
    capOn := GetKeyState("CapsLock", "T")
    numOn := GetKeyState("NumLock",  "T")
    if g_showCaps
        TrayModify(ID_CAPS, capOn ? g_hCapOn : g_hCapOff, capOn ? "Caps Lock: ON"  : "Caps Lock: OFF")
    if g_showNum
        TrayModify(ID_NUM,  numOn ? g_hNumOn : g_hNumOff, numOn ? "Num Lock: ON"   : "Num Lock: OFF")
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

; Show or hide an icon, save the pref to ini
SetIconVisible(id, visible) {
    ; Guard: refuse to hide the last visible icon
    if !visible {
        otherVisible := (id = ID_CAPS) ? g_showNum : g_showCaps
        if !otherVisible {
            ToolTip("At least one icon must remain visible")
            SetTimer(() => ToolTip(), -3000)
            return
        }
    }
    if (id = ID_CAPS) {
        g_showCaps := visible
        IniWrite(visible ? "1" : "0", g_ini, "Visibility", "ShowCaps")
        if visible {
            TrayAdd(ID_CAPS)
            SyncIcons()
        } else {
            TrayRemove(ID_CAPS)
        }
    } else {
        g_showNum := visible
        IniWrite(visible ? "1" : "0", g_ini, "Visibility", "ShowNum")
        if visible {
            TrayAdd(ID_NUM)
            SyncIcons()
        } else {
            TrayRemove(ID_NUM)
        }
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
        if      (iconID = ID_CAPS)  ToggleCapsLock()
        else if (iconID = ID_NUM)   ToggleNumLock()
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
            m.Add("Hide Num Lock icon",  (*) => SetIconVisible(ID_NUM, false))
        else
            m.Add("Show Num Lock icon",  (*) => SetIconVisible(ID_NUM, true))
    } else if (iconID = ID_NUM) {
        numOn := GetKeyState("NumLock", "T")
        m.Add("Num Lock is " (numOn ? "ON  — click to turn Off" : "OFF — click to turn On"), (*) => ToggleNumLock())
        m.Add()
        if g_showCaps
            m.Add("Hide Caps Lock icon", (*) => SetIconVisible(ID_CAPS, false))
        else
            m.Add("Show Caps Lock icon", (*) => SetIconVisible(ID_CAPS, true))
    }

    m.Add()
    m.Add("Show OSD on toggle", (*) => ToggleOSD())
    if g_showOSD
        m.Check("Show OSD on toggle")
    m.Add("Beep on toggle", (*) => ToggleBeep())
    if g_beepOnToggle
        m.Check("Beep on toggle")
    m.Add()
    m.Add("Run at startup", (*) => ToggleStartup())
    if IsStartupEnabled()
        m.Check("Run at startup")
    m.Add()
    m.Add("Exit CapsNumTray", (*) => ExitApp())
    m.Show(clickX, clickY)
}

; FIX P1-B: re-add all icons after Explorer crash/restart
OnTaskbarCreated(*) {
    if g_showCaps  TrayAdd(ID_CAPS)
    if g_showNum   TrayAdd(ID_NUM)
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
    ; Stage 1: file on disk
    path  := A_ScriptDir "\" name ".ico"
    hIcon := DllCall("LoadImage", "Ptr", 0, "WStr", path,
        "UInt", 1, "Int", g_iconSize, "Int", g_iconSize, "UInt", 0x10, "Ptr")
    if hIcon {
        g_ownedIcons[hIcon] := true
        return hIcon
    }
    ; Stage 2: embedded PE resource (compiled .exe only)
    if A_IsCompiled {
        resIDs := Map("CapsLockOn", 210, "CapsLockOff", 211, "NumLockOn", 212, "NumLockOff", 213)
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
    if g_showCaps  TrayRemove(ID_CAPS)
    if g_showNum   TrayRemove(ID_NUM)
    ; FIX P1-D: only DestroyIcon handles we own (not shared system icons)
    for h in [g_hCapOn, g_hCapOff, g_hNumOn, g_hNumOff]
        if h && g_ownedIcons.Has(h)
            DllCall("DestroyIcon", "Ptr", h)
}
