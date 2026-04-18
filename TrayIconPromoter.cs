using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace CapsNumTray;

/// <summary>
/// On Windows 11, new tray icons default to hidden-in-overflow until the user
/// manually flips "Show icon in taskbar" under Settings → Personalization →
/// Taskbar → Other system tray icons. For a tray-only app like CapsNumTray
/// that's a painful first-run experience — the whole point of the app is
/// the visible lock-state icon in the taskbar.
///
/// Windows 11 22H2+ stores per-icon visibility at
/// <c>HKCU\Control Panel\NotifyIconSettings\&lt;hash&gt;</c> with a DWORD value
/// named <c>IsPromoted</c> (1 = visible in taskbar, 0 = hidden, missing =
/// Explorer's default, which shows as hidden). The per-icon subkey normally
/// carries <c>ExecutablePath</c>, <c>InitialTooltip</c>, <c>UID</c>, and
/// <c>IconSnapshot</c> fields — Explorer populates them on <c>NIM_ADD</c>
/// when the caller passes <c>NIF_TIP</c>.
///
/// Two-phase identification so we survive Explorer edge cases:
///   Phase 1: match a subkey whose <c>ExecutablePath</c> equals ours — the
///            normal case, works on reruns and whenever Explorer wrote the
///            full schema.
///   Phase 2: if Phase 1 found nothing and exactly one "orphan" subkey
///            (IconSnapshot present but ExecutablePath missing) has appeared
///            since the baseline we captured before NIM_ADD, claim it —
///            write BOTH our <c>ExecutablePath</c> AND <c>IsPromoted=1</c>.
///            Defers when multiple orphans appear (Windows-login race);
///            later ticks disambiguate as other apps' subkeys fill in.
///
/// We never override an explicit <c>IsPromoted=0</c> — that's the user
/// having deliberately hidden us, and we respect it.
///
/// ──────────────────────────────────────────────────────────────────
/// This mechanism is undocumented. It has been stable across Win11
/// 22H2, 23H2, 24H2, and 25H2. All registry interaction is wrapped in
/// try/catch so a schema change in a future build silently no-ops
/// instead of crashing.
///
/// Note for CapsNumTray specifically: this app registers THREE tray
/// icons (Caps/Num/Scroll), so there are three subkeys to promote.
/// TryPromote writes <c>IsPromoted=1</c> for every Phase-1 match in a
/// single pass, so one tick in which Explorer has populated all three
/// ExecutablePaths promotes all three. The caller runs the retry timer
/// to its full cap regardless of a single-icon "identified" result,
/// to give Explorer time to populate any late subkeys.
/// </summary>
internal static class TrayIconPromoter
{
    private const string KeyPath = @"Control Panel\NotifyIconSettings";
    private const int MinWin11Build = 22000;

    /// <summary>
    /// Capture the set of existing subkey names BEFORE the tray icons
    /// register (i.e. before the first <c>Shell_NotifyIconW NIM_ADD</c>).
    /// Anything that appears later is a candidate for Phase-2 orphan matching.
    ///
    /// Returns null on failure (access denied, etc.) — callers should
    /// treat null as "skip Phase 2" rather than "no subkeys existed,"
    /// otherwise we'd confuse every pre-existing subkey with a fresh one.
    /// </summary>
    internal static HashSet<string>? CaptureBaseline()
    {
        if (Environment.OSVersion.Version.Build < MinWin11Build) return null;
        try
        {
            using var root = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
            if (root is null) return new HashSet<string>(StringComparer.Ordinal);
            return new HashSet<string>(root.GetSubKeyNames(), StringComparer.Ordinal);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter.CaptureBaseline: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Ensure the current exe's tray icons are promoted to visible. Returns
    /// true once we've identified at least one of our subkeys (whether we
    /// needed to write anything or not). For a single-icon app the caller's
    /// retry timer can stop at that point; for a multi-icon app (CapsNumTray)
    /// the caller should keep polling to let Explorer populate any late
    /// subkeys. Returns false while we're still waiting for Explorer to
    /// create or populate anything.
    /// </summary>
    internal static bool TryPromote(string exePath, HashSet<string>? baseline)
    {
        if (Environment.OSVersion.Version.Build < MinWin11Build) return false;
        if (string.IsNullOrWhiteSpace(exePath)) return false;

        try
        {
            using var root = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
            if (root is null) return false;

            bool identified = false;
            var orphanCandidates = new List<string>();

            foreach (var subName in root.GetSubKeyNames())
            {
                try
                {
                    using var sub = root.OpenSubKey(subName, writable: true);
                    if (sub is null) continue;

                    var path = sub.GetValue("ExecutablePath") as string;

                    // Phase 1 — path match. Handles reruns and the normal case
                    // where Explorer populated the full schema on NIM_ADD.
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (!string.Equals(path, exePath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        identified = true;
                        var current = sub.GetValue("IsPromoted");
                        if (current is int i0 && i0 == 0)
                        {
                            System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: {subName} IsPromoted=0 — respecting user's choice.");
                            continue;
                        }
                        if (current is int i1 && i1 == 1) continue;

                        sub.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                        System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: promoted {subName} for {path}.");
                        continue;
                    }

                    // Phase 2 candidate — orphan subkey (IconSnapshot but no
                    // ExecutablePath) that appeared AFTER our NIM_ADD.
                    // Skip if we didn't capture a baseline (would false-claim
                    // any pre-existing orphan belonging to another app).
                    if (baseline is null) continue;
                    if (baseline.Contains(subName)) continue;
                    if (sub.GetValue("IconSnapshot") is not byte[]) continue;

                    orphanCandidates.Add(subName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: subkey {subName}: {ex.Message}");
                }
            }

            // Phase 2 commit. Only claim when exactly one orphan appeared —
            // multiple = Windows-login race with other tray apps, wait for
            // the next tick. Their subkeys will fill in ExecutablePath as
            // Explorer does its next pass, leaving ours as the sole orphan.
            if (!identified && orphanCandidates.Count == 1)
            {
                var subName = orphanCandidates[0];
                try
                {
                    using var sub = root.OpenSubKey(subName, writable: true);
                    if (sub is not null)
                    {
                        var current = sub.GetValue("IsPromoted");
                        if (current is int i0 && i0 == 0)
                        {
                            System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: orphan {subName} IsPromoted=0 — respecting user's choice.");
                            identified = true;
                        }
                        else
                        {
                            sub.SetValue("ExecutablePath", exePath, RegistryValueKind.String);
                            sub.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                            System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: claimed orphan {subName} → wrote ExecutablePath + IsPromoted=1 for {exePath}.");
                            identified = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: claim orphan {subName}: {ex.Message}");
                }
            }
            else if (orphanCandidates.Count > 1)
            {
                System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: {orphanCandidates.Count} new orphan subkeys — deferring to next tick.");
            }

            return identified;
        }
        catch (Exception ex)
        {
            // Registry access denied, hive locked, schema moved — anything.
            // This is UX polish, never a crash surface.
            System.Diagnostics.Trace.WriteLine($"CapsNumTray: TrayIconPromoter: {ex.Message}");
            return false;
        }
    }
}
