using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HavensAlmanac.Data;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    /// <summary>
    /// Surfaces <c>SunhavenMods.Shared.VersionChecker</c> health telemetry
    /// (last-check time, exception count) for every mod that participates.
    ///
    /// Why this provider is special: every mod in the repo compiles its own
    /// copy of <c>VersionChecker.cs</c> into its DLL (via the
    /// <c>&lt;Compile Include="..\SharedUtilities\VersionChecker.cs"&gt;</c>
    /// link pattern, with CS0436 explicitly suppressed). That means every
    /// mod DLL has its OWN <c>SunhavenMods.Shared.VersionChecker</c> Type
    /// with its OWN private static <c>HealthByPluginGuid</c> dictionary.
    /// Asking only Haven's Almanac's copy gives you exactly one snapshot
    /// (this plugin's self-registered one).
    ///
    /// To build a true cross-mod health panel we reflect over every loaded
    /// assembly, find its <c>SunhavenMods.Shared.VersionChecker</c> Type if
    /// present, pull the private <c>HealthByPluginGuid</c> dictionary, and
    /// merge every per-GUID snapshot into one view. The merge keeps the
    /// freshest <c>LastCheckUtc</c> and the max <c>ExceptionCount</c> per
    /// GUID so rerunning a version check in one DLL can't lose signal that
    /// was already captured in another's copy.
    /// </summary>
    public class ModHealthDataProvider : IModDataProvider
    {
        public string ModName => "Mod Health";
        public string ModIcon => "+";

        private readonly List<string> _lines = new List<string>();
        private string _hudSummary = "No checks";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        // Mod Health telemetry is a dashboard concern, never surfaced in the
        // morning briefing (DrawBriefingSection always returns false).
        public bool HasBriefingContent => false;

        // Cached cross-assembly probes. Each tuple is (Type, FieldInfo for
        // the private static HealthByPluginGuid dictionary). Cached because
        // loaded assemblies don't come and go after BepInEx chainload, and
        // we refresh every ~5 seconds — no point re-walking AppDomain each
        // tick.
        private static readonly object CacheLock = new object();
        private static List<FieldInfo> _cachedHealthFields;
        private static int _cachedAssemblyCount;

        public void Refresh()
        {
            _lines.Clear();
            try
            {
                var merged = CollectHealthSnapshotsAcrossLoadedMods();

                int tracked = 0;
                int issues = 0;
                foreach (var entry in merged)
                {
                    tracked++;
                    if (entry.ExceptionCount > 0)
                        issues++;

                    string when = entry.LastCheckUtc == default
                        ? "never"
                        : entry.LastCheckUtc.ToLocalTime().ToString("HH:mm:ss");

                    string displayName = ResolvePluginDisplayName(entry.PluginGuid) ?? entry.PluginGuid;
                    _lines.Add($"{displayName}: last check {when}, exceptions {entry.ExceptionCount}");
                }

                _hudSummary = tracked == 0
                    ? "No checks"
                    : $"{tracked} checked / {issues} issue{(issues == 1 ? string.Empty : "s")}";
                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[ModHealthProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            if (_lines.Count == 0)
            {
                GUILayout.Label("No VersionChecker telemetry yet.");
                return;
            }

            foreach (string line in _lines)
                GUILayout.Label(line);
        }

        public bool DrawBriefingSection()
        {
            return false;
        }

        // -------- cross-assembly merge -------------------------------------

        private struct HealthEntry
        {
            public string PluginGuid;
            public DateTime LastCheckUtc;
            public int ExceptionCount;
            public string LastError;
        }

        private static List<HealthEntry> CollectHealthSnapshotsAcrossLoadedMods()
        {
            var fields = GetCachedHealthFields();

            // Keyed by GUID so the same mod registering in multiple copies
            // (unlikely but possible) doesn't produce duplicate rows.
            var merged = new Dictionary<string, HealthEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in fields)
            {
                IDictionary dict;
                try { dict = field.GetValue(null) as IDictionary; }
                catch { continue; }
                if (dict == null) continue;

                foreach (DictionaryEntry kv in dict)
                {
                    string guid = kv.Key as string;
                    if (string.IsNullOrEmpty(guid)) continue;

                    object snap = kv.Value;
                    if (snap == null) continue;

                    var candidate = ReadSnapshotShape(guid, snap);

                    // Merge: keep freshest LastCheckUtc + max ExceptionCount
                    // per GUID so snapshots captured in different DLLs for
                    // the same plugin still combine into a coherent picture.
                    if (!merged.TryGetValue(guid, out var existing))
                    {
                        merged[guid] = candidate;
                    }
                    else
                    {
                        merged[guid] = new HealthEntry
                        {
                            PluginGuid = guid,
                            LastCheckUtc = candidate.LastCheckUtc > existing.LastCheckUtc
                                ? candidate.LastCheckUtc
                                : existing.LastCheckUtc,
                            ExceptionCount = Math.Max(existing.ExceptionCount, candidate.ExceptionCount),
                            LastError = string.IsNullOrEmpty(candidate.LastError)
                                ? existing.LastError
                                : candidate.LastError,
                        };
                    }
                }
            }

            return merged.Values
                .OrderBy(e => e.PluginGuid, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<FieldInfo> GetCachedHealthFields()
        {
            // Rebuild the cache only when the assembly count changes (new
            // plugin loaded mid-session, which shouldn't happen with BepInEx
            // but is cheap to be safe about).
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            lock (CacheLock)
            {
                if (_cachedHealthFields != null && assemblies.Length == _cachedAssemblyCount)
                    return _cachedHealthFields;

                var found = new List<FieldInfo>();
                foreach (var asm in assemblies)
                {
                    Type vcType = null;
                    try { vcType = asm.GetType("SunhavenMods.Shared.VersionChecker", throwOnError: false); }
                    catch { /* some assemblies can't be introspected */ }
                    if (vcType == null) continue;

                    var field = vcType.GetField(
                        "HealthByPluginGuid",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    if (field == null) continue;

                    found.Add(field);
                }

                _cachedHealthFields = found;
                _cachedAssemblyCount = assemblies.Length;
                HavensAlmanac.Plugin.Log?.LogInfo(
                    $"[ModHealthProvider] Discovered {found.Count} VersionChecker copies across loaded assemblies.");
                return _cachedHealthFields;
            }
        }

        private static HealthEntry ReadSnapshotShape(string guid, object snap)
        {
            // ModHealthSnapshot is an identical-shape POCO in every mod's
            // copy of VersionChecker, but each DLL's version is a DIFFERENT
            // runtime Type — so we can't cast. We also can't rely on
            // PropertyInfo lookups being free; the public property getters
            // map cleanly onto the shape we care about.
            var type = snap.GetType();
            var entry = new HealthEntry { PluginGuid = guid };

            var pLast = type.GetProperty("LastCheckUtc");
            if (pLast != null)
            {
                object v = pLast.GetValue(snap);
                if (v is DateTime dt) entry.LastCheckUtc = dt;
            }

            var pEx = type.GetProperty("ExceptionCount");
            if (pEx != null)
            {
                object v = pEx.GetValue(snap);
                if (v is int n) entry.ExceptionCount = n;
            }

            var pErr = type.GetProperty("LastError");
            if (pErr != null)
            {
                entry.LastError = pErr.GetValue(snap) as string;
            }

            return entry;
        }

        private static string ResolvePluginDisplayName(string pluginGuid)
        {
            try
            {
                if (Chainloader.PluginInfos != null
                    && Chainloader.PluginInfos.TryGetValue(pluginGuid, out var info)
                    && info?.Metadata != null
                    && !string.IsNullOrEmpty(info.Metadata.Name))
                {
                    return info.Metadata.Name;
                }
            }
            catch
            {
                // Chainloader may not be ready in exotic startup paths; fall
                // back to the GUID below.
            }
            return null;
        }
    }
}
