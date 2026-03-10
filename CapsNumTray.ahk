; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  CapsNumTray.ahk  —  Caps Lock + Num Lock tray indicators               ║
; ║  Requires: AutoHotKey v2  (https://www.autohotkey.com/)                 ║
; ║                                                                          ║
; ║  Files (place in the same folder as this script):                       ║
; ║    CapsLockOn.ico  CapsLockOff.ico                                      ║
; ║    NumLockOn.ico   NumLockOff.ico                                        ║
; ║    (missing icons fall back to Windows built-in icons)                  ║
; ║                                                                          ║
; ║  • Left-click  Caps icon  → toggle Caps Lock                            ║
; ║  • Left-click  Num  icon  → toggle Num Lock                             ║
; ║  • Right-click either     → menu (toggle / show-hide / exit)            ║
; ║  Visibility prefs saved to CapsNumTray.ini (next to script)             ║
; ╚══════════════════════════════════════════════════════════════════════════╝

#Requires AutoHotkey v2.0
#SingleInstance Force
Persistent
#NoTrayIcon   ; suppress AHK's own icon — we manage ours manually

; ── ICON IDs ──────────────────────────────────────────────────────────────────
global ID_CAPS := 10
global ID_NUM  := 11

; ── TRAY CALLBACK MESSAGE ─────────────────────────────────────────────────────
global WM_TRAY := 0x8010

; ── INI FILE (saved next to the script) ──────────────────────────────────────
global g_ini := A_ScriptDir "\CapsNumTray.ini"

; ── LOAD VISIBILITY PREFS FROM INI (default both visible) ────────────────────
; FIX: default must be string "1" so the = "1" comparison works on first run
; before the .ini file has been created yet.
global g_showCaps := IniRead(g_ini, "Visibility", "ShowCaps", "1") = "1" ? true : false
global g_showNum  := IniRead(g_ini, "Visibility", "ShowNum",  "1") = "1" ? true : false

; ── LOAD ICO FILES ────────────────────────────────────────────────────────────
global g_hCapOn  := LoadIco("CapsLockOn",  "IDI_EXCLAMATION")
global g_hCapOff := LoadIco("CapsLockOff", "IDI_WARNING")
global g_hNumOn  := LoadIco("NumLockOn",   "IDI_INFORMATION")
global g_hNumOff := LoadIco("NumLockOff",  "IDI_WARNING")

; ── ADD TRAY ICONS (only the ones that should be visible) ────────────────────
if g_showCaps
    TrayAdd(ID_CAPS)
if g_showNum
    TrayAdd(ID_NUM)

; ── INITIAL SYNC ─────────────────────────────────────────────────────────────
SyncIcons()

; ── POLL FOR STATE CHANGES ───────────────────────────────────────────────────
SetTimer(SyncIcons, 250)

; ── TRAY MESSAGE HANDLER ─────────────────────────────────────────────────────
OnMessage(WM_TRAY, OnTrayMsg)

OnExit(Cleanup)

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Core functions                                                          ║
; ╚══════════════════════════════════════════════════════════════════════════╝

SyncIcons() {
    global g_hCapOn, g_hCapOff, g_hNumOn, g_hNumOff, ID_CAPS, ID_NUM, g_showCaps, g_showNum
    capOn := GetKeyState("CapsLock", "T")
    numOn := GetKeyState("NumLock",  "T")
    if g_showCaps
        TrayModify(ID_CAPS, capOn ? g_hCapOn : g_hCapOff, capOn ? "Caps Lock: ON"  : "Caps Lock: OFF")
    if g_showNum
        TrayModify(ID_NUM,  numOn ? g_hNumOn : g_hNumOff, numOn ? "Num Lock: ON"   : "Num Lock: OFF")
}

ToggleCapsLock() {
    SetCapsLockState(GetKeyState("CapsLock", "T") ? "Off" : "On")
    SyncIcons()
}

ToggleNumLock() {
    SetNumLockState(GetKeyState("NumLock", "T") ? "Off" : "On")
    SyncIcons()
}

; Show or hide an icon, save the pref to ini
SetIconVisible(id, visible) {
    global g_showCaps, g_showNum, g_ini
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

; Left-click or right-click on either icon
OnTrayMsg(wParam, lParam, *) {
    WM_LBUTTONUP := 0x202
    WM_RBUTTONUP := 0x205

    ; Left-click → toggle directly
    if (lParam = WM_LBUTTONUP) {
        if (wParam = ID_CAPS)
            ToggleCapsLock()
        else if (wParam = ID_NUM)
            ToggleNumLock()
        return
    }

    if (lParam != WM_RBUTTONUP)
        return

    global g_showCaps, g_showNum, ID_CAPS, ID_NUM
    m := Menu()

    if (wParam = ID_CAPS) {
        capOn := GetKeyState("CapsLock", "T")
        m.Add("Caps Lock is " (capOn ? "ON  — click to turn Off" : "OFF — click to turn On"), (*) => ToggleCapsLock())
        m.Add()
        if g_showNum
            m.Add("Hide Num Lock icon",  (*) => SetIconVisible(ID_NUM, false))
        else
            m.Add("Show Num Lock icon",  (*) => SetIconVisible(ID_NUM, true))

    } else if (wParam = ID_NUM) {
        numOn := GetKeyState("NumLock", "T")
        m.Add("Num Lock is " (numOn ? "ON  — click to turn Off" : "OFF — click to turn On"), (*) => ToggleNumLock())
        m.Add()
        if g_showCaps
            m.Add("Hide Caps Lock icon", (*) => SetIconVisible(ID_CAPS, false))
        else
            m.Add("Show Caps Lock icon", (*) => SetIconVisible(ID_CAPS, true))
    }

    m.Add()
    m.Add("Exit CapsNumTray", (*) => ExitApp())
    m.Show()
}

; ╔══════════════════════════════════════════════════════════════════════════╗
; ║  Shell_NotifyIconW helpers                                               ║
; ╚══════════════════════════════════════════════════════════════════════════╝

BuildNID(id, hIcon := 0, tip := "") {
    nid := Buffer(976, 0)
    NumPut("UInt", 976,          nid,  0)
    NumPut("Ptr",  A_ScriptHwnd, nid,  8)
    NumPut("UInt", id,           nid, 16)
    ; FIX: 0x7 = NIF_MESSAGE|NIF_ICON|NIF_TIP (removed NIF_STATE which was
    ; causing erratic icon behaviour with zeroed-out state fields)
    NumPut("UInt", 0x7,          nid, 20)
    NumPut("UInt", WM_TRAY,      nid, 24)
    NumPut("Ptr",  hIcon,        nid, 32)
    StrPut(tip, nid.Ptr + 40, 128, "UTF-16")
    return nid
}

TrayAdd(id) {
    nid := BuildNID(id)
    DllCall("Shell32\Shell_NotifyIconW", "UInt", 0, "Ptr", nid)  ; NIM_ADD
}

TrayModify(id, hIcon, tip) {
    nid := BuildNID(id, hIcon, tip)
    DllCall("Shell32\Shell_NotifyIconW", "UInt", 1, "Ptr", nid)  ; NIM_MODIFY
}

TrayRemove(id) {
    nid := Buffer(976, 0)
    NumPut("UInt", 976,          nid,  0)
    NumPut("Ptr",  A_ScriptHwnd, nid,  8)
    NumPut("UInt", id,           nid, 16)
    DllCall("Shell32\Shell_NotifyIconW", "UInt", 2, "Ptr", nid)  ; NIM_DELETE
}

; Load a custom .ico file. If the file is missing, fall back to a Windows
; built-in icon so the script keeps running instead of crashing.
LoadIco(name, fallbackOrdinal := "IDI_APPLICATION") {
    path  := A_ScriptDir "\" name ".ico"
    hIcon := DllCall("LoadImage", "Ptr", 0, "WStr", path,
        "UInt", 1,
        "Int",  32,
        "Int",  32,
        "UInt", 0x10,
        "Ptr")
    if !hIcon {
        ; File missing — load the requested Windows built-in icon instead
        hIcon := DllCall("LoadIcon", "Ptr", 0, "Ptr", fallbackOrdinal, "Ptr")
    }
    return hIcon
}

Cleanup(*) {
    global ID_CAPS, ID_NUM, g_hCapOn, g_hCapOff, g_hNumOn, g_hNumOff, g_showCaps, g_showNum
    if g_showCaps
        TrayRemove(ID_CAPS)
    if g_showNum
        TrayRemove(ID_NUM)
    for h in [g_hCapOn, g_hCapOff, g_hNumOn, g_hNumOff]
        DllCall("DestroyIcon", "Ptr", h)
}
