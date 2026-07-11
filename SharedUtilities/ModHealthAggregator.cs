using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Merges VersionChecker telemetry and ModDiagnostics reports across every
    /// loaded assembly copy of the shared utilities (linked-source pattern).
    /// </summary>
    public static class ModHealthAggregator
    {
        private static readonly object CacheLock = new object();
        private static List<FieldInfo> _cachedVersionCheckerFields;
        private static List<FieldInfo> _cachedDiagnosticsFields;
        private static int _cachedAssemblyCount;
        private static DateTime _cacheBuiltAt = DateTime.MinValue;
        private static readonly TimeSpan CacheMaxAge = TimeSpan.FromMinutes(5);
        private static readonly Dictionary<Type, SnapshotShape> VersionSnapshotShapeCache = new Dictionary<Type, SnapshotShape>();
        private static readonly Dictionary<Type, DiagnosticsShape> DiagnosticsShapeCache = new Dictionary<Type, DiagnosticsShape>();

        public sealed class MergedModHealthRow
        {
            public string PluginGuid { get; set; }
            public string DisplayName { get; set; }
            public string InstalledVersion { get; set; }
            public string SharedCodeRevision { get; set; }
            public string IntegrationSummary { get; set; }
            public string Mode { get; set; }
            public string CharacterName { get; set; }
            public bool? DataLoaded { get; set; }
            public string LastPersistenceOutcome { get; set; }
            public DateTime LastCheckUtc { get; set; }
            public int ExceptionCount { get; set; }
            public string LastError { get; set; }
            public DateTime ReportedUtc { get; set; }
            public bool HasDiagnosticsReport { get; set; }

            public bool HasIssue => ExceptionCount > 0 || !string.IsNullOrEmpty(LastError);

            public void AppendDiagnosticDump(StringBuilder sb)
            {
                sb.AppendLine($"=== {DisplayName ?? PluginGuid} ===");
                sb.AppendLine($"installed: {InstalledVersion ?? "—"}");
                sb.AppendLine($"shared code: {SharedCodeRevision ?? "—"}");
                sb.AppendLine($"version check: {(LastCheckUtc == default ? "never" : LastCheckUtc.ToLocalTime().ToString("u"))}, exceptions {ExceptionCount}");
                if (!string.IsNullOrEmpty(LastError))
                    sb.AppendLine($"last error: {LastError}");
                sb.AppendLine($"integrations: {IntegrationSummary ?? "—"}");
                sb.AppendLine($"mode: {Mode ?? "—"}");
                sb.AppendLine($"character: {(string.IsNullOrEmpty(CharacterName) ? "—" : CharacterName)}");
                sb.AppendLine($"data loaded: {(DataLoaded.HasValue ? DataLoaded.Value.ToString() : "—")}");
                sb.AppendLine($"last save: {LastPersistenceOutcome ?? "—"}");
                sb.AppendLine($"diagnostics reported: {(ReportedUtc == default ? "—" : ReportedUtc.ToLocalTime().ToString("u"))}");
                sb.AppendLine();
            }
        }

        public sealed class SharedCodeSkewAnalysis
        {
            public bool HasSkew { get; set; }
            public int VersionCheckerCopyCount { get; set; }
            public int ModDiagnosticsCopyCount { get; set; }
            public IReadOnlyDictionary<string, IReadOnlyList<string>> ModsByRevision { get; set; }
                = new Dictionary<string, IReadOnlyList<string>>();
        }

        public static IReadOnlyList<MergedModHealthRow> CollectMergedRows(
            Func<string, string> resolveDisplayName = null,
            Func<string, string> resolveInstalledVersion = null)
        {
            resolveDisplayName ??= _ => null;
            resolveInstalledVersion ??= _ => null;

            var versionTelemetry = CollectVersionCheckerTelemetry();
            var diagnostics = CollectDiagnosticsReports();

            var merged = new Dictionary<string, MergedModHealthRow>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in versionTelemetry)
            {
                merged[entry.PluginGuid] = new MergedModHealthRow
                {
                    PluginGuid = entry.PluginGuid,
                    DisplayName = resolveDisplayName(entry.PluginGuid) ?? entry.PluginGuid,
                    InstalledVersion = resolveInstalledVersion(entry.PluginGuid),
                    LastCheckUtc = entry.LastCheckUtc,
                    ExceptionCount = entry.ExceptionCount,
                    LastError = entry.LastError
                };
            }

            foreach (var report in diagnostics)
            {
                if (string.IsNullOrEmpty(report.PluginGuid))
                    continue;

                if (!merged.TryGetValue(report.PluginGuid, out var row))
                {
                    row = new MergedModHealthRow { PluginGuid = report.PluginGuid };
                    merged[report.PluginGuid] = row;
                }

                row.HasDiagnosticsReport = true;
                row.DisplayName = report.DisplayName ?? row.DisplayName ?? resolveDisplayName(report.PluginGuid) ?? report.PluginGuid;
                row.InstalledVersion = report.PluginVersion ?? row.InstalledVersion ?? resolveInstalledVersion(report.PluginGuid);
                row.SharedCodeRevision = report.SharedCodeRevision ?? row.SharedCodeRevision;
                row.IntegrationSummary = report.IntegrationSummary ?? row.IntegrationSummary;
                row.Mode = report.Mode ?? row.Mode;
                row.CharacterName = report.CharacterName ?? row.CharacterName;
                row.DataLoaded = report.DataLoaded ?? row.DataLoaded;
                row.LastPersistenceOutcome = report.LastPersistenceOutcome ?? row.LastPersistenceOutcome;
                row.ReportedUtc = report.ReportedUtc > row.ReportedUtc ? report.ReportedUtc : row.ReportedUtc;
                if (!string.IsNullOrEmpty(report.LastError))
                    row.LastError = report.LastError;
            }

            foreach (var row in merged.Values)
            {
                if (string.IsNullOrEmpty(row.DisplayName))
                    row.DisplayName = resolveDisplayName(row.PluginGuid) ?? row.PluginGuid;
                if (string.IsNullOrEmpty(row.InstalledVersion))
                    row.InstalledVersion = resolveInstalledVersion(row.PluginGuid);
            }

            return merged.Values
                .OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static SharedCodeSkewAnalysis AnalyzeSharedCodeSkew(IEnumerable<MergedModHealthRow> rows)
        {
            var fields = GetCachedDiagnosticsFields();
            var byRevision = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (string.IsNullOrEmpty(row.SharedCodeRevision))
                        continue;

                    if (!byRevision.TryGetValue(row.SharedCodeRevision, out var names))
                    {
                        names = new List<string>();
                        byRevision[row.SharedCodeRevision] = names;
                    }

                    names.Add(row.DisplayName ?? row.PluginGuid);
                }
            }

            return new SharedCodeSkewAnalysis
            {
                HasSkew = byRevision.Count > 1,
                VersionCheckerCopyCount = GetCachedVersionCheckerFields().Count,
                ModDiagnosticsCopyCount = fields.Count,
                ModsByRevision = byRevision.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IReadOnlyList<string>)kvp.Value.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList())
            };
        }

        public static string BuildCombinedDiagnosticDump(IEnumerable<MergedModHealthRow> rows)
        {
            var sb = new StringBuilder();
            if (rows == null)
                return sb.ToString();

            foreach (var row in rows)
                row.AppendDiagnosticDump(sb);

            return sb.ToString();
        }

        private struct VersionTelemetryEntry
        {
            public string PluginGuid;
            public DateTime LastCheckUtc;
            public int ExceptionCount;
            public string LastError;
        }

        private static List<VersionTelemetryEntry> CollectVersionCheckerTelemetry()
        {
            var fields = GetCachedVersionCheckerFields();
            var merged = new Dictionary<string, VersionTelemetryEntry>(StringComparer.OrdinalIgnoreCase);

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

                    var candidate = ReadVersionSnapshot(guid, snap);
                    if (!merged.TryGetValue(guid, out var existing))
                    {
                        merged[guid] = candidate;
                    }
                    else
                    {
                        merged[guid] = new VersionTelemetryEntry
                        {
                            PluginGuid = guid,
                            LastCheckUtc = candidate.LastCheckUtc > existing.LastCheckUtc
                                ? candidate.LastCheckUtc
                                : existing.LastCheckUtc,
                            ExceptionCount = Math.Max(existing.ExceptionCount, candidate.ExceptionCount),
                            LastError = string.IsNullOrEmpty(candidate.LastError)
                                ? existing.LastError
                                : candidate.LastError
                        };
                    }
                }
            }

            return merged.Values.ToList();
        }

        private static List<ModHealthReport> CollectDiagnosticsReports()
        {
            var fields = GetCachedDiagnosticsFields();
            var merged = new Dictionary<string, ModHealthReport>(StringComparer.OrdinalIgnoreCase);

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

                    object reportObj = kv.Value;
                    if (reportObj == null) continue;

                    var candidate = ReadDiagnosticsReport(guid, reportObj);
                    if (!merged.TryGetValue(guid, out var existing))
                    {
                        merged[guid] = candidate;
                    }
                    else
                    {
                        merged[guid] = MergeDiagnosticsReports(existing, candidate);
                    }
                }
            }

            return merged.Values.ToList();
        }

        private static ModHealthReport MergeDiagnosticsReports(ModHealthReport a, ModHealthReport b)
        {
            var newer = a.ReportedUtc >= b.ReportedUtc ? a : b;
            var older = ReferenceEquals(newer, a) ? b : a;
            var merged = newer.Clone();
            merged.IntegrationSummary = newer.IntegrationSummary ?? older.IntegrationSummary;
            merged.Mode = newer.Mode ?? older.Mode;
            merged.CharacterName = newer.CharacterName ?? older.CharacterName;
            merged.DataLoaded = newer.DataLoaded ?? older.DataLoaded;
            merged.LastPersistenceOutcome = newer.LastPersistenceOutcome ?? older.LastPersistenceOutcome;
            merged.SharedCodeRevision = newer.SharedCodeRevision ?? older.SharedCodeRevision;
            merged.LastError = newer.LastError ?? older.LastError;
            merged.DisplayName = newer.DisplayName ?? older.DisplayName;
            merged.PluginVersion = newer.PluginVersion ?? older.PluginVersion;
            return merged;
        }

        private static ModHealthReport ReadDiagnosticsReport(string guid, object reportObj)
        {
            var type = reportObj.GetType();
            var shape = GetDiagnosticsShape(type);
            var report = new ModHealthReport { PluginGuid = guid };

            report.DisplayName = shape.DisplayName?.GetValue(reportObj) as string;
            report.PluginVersion = shape.PluginVersion?.GetValue(reportObj) as string;
            report.SharedCodeRevision = shape.SharedCodeRevision?.GetValue(reportObj) as string;
            report.IntegrationSummary = shape.IntegrationSummary?.GetValue(reportObj) as string;
            report.Mode = shape.Mode?.GetValue(reportObj) as string;
            report.CharacterName = shape.CharacterName?.GetValue(reportObj) as string;
            report.LastPersistenceOutcome = shape.LastPersistenceOutcome?.GetValue(reportObj) as string;
            report.LastError = shape.LastError?.GetValue(reportObj) as string;

            if (shape.DataLoaded?.GetValue(reportObj) is bool dataLoaded)
                report.DataLoaded = dataLoaded;

            if (shape.ReportedUtc?.GetValue(reportObj) is DateTime reported)
                report.ReportedUtc = reported;

            return report;
        }

        private static VersionTelemetryEntry ReadVersionSnapshot(string guid, object snap)
        {
            var type = snap.GetType();
            var entry = new VersionTelemetryEntry { PluginGuid = guid };
            var shape = GetVersionSnapshotShape(type);

            if (shape.LastCheckUtc?.GetValue(snap) is DateTime dt)
                entry.LastCheckUtc = dt;

            if (shape.ExceptionCount?.GetValue(snap) is int n)
                entry.ExceptionCount = n;

            entry.LastError = shape.LastError?.GetValue(snap) as string;
            return entry;
        }

        private struct SnapshotShape
        {
            public PropertyInfo LastCheckUtc;
            public PropertyInfo ExceptionCount;
            public PropertyInfo LastError;
        }

        private struct DiagnosticsShape
        {
            public PropertyInfo DisplayName;
            public PropertyInfo PluginVersion;
            public PropertyInfo SharedCodeRevision;
            public PropertyInfo IntegrationSummary;
            public PropertyInfo Mode;
            public PropertyInfo CharacterName;
            public PropertyInfo DataLoaded;
            public PropertyInfo LastPersistenceOutcome;
            public PropertyInfo LastError;
            public PropertyInfo ReportedUtc;
        }

        private static SnapshotShape GetVersionSnapshotShape(Type snapshotType)
        {
            lock (CacheLock)
            {
                if (VersionSnapshotShapeCache.TryGetValue(snapshotType, out var shape))
                    return shape;

                shape = new SnapshotShape
                {
                    LastCheckUtc = snapshotType.GetProperty("LastCheckUtc"),
                    ExceptionCount = snapshotType.GetProperty("ExceptionCount"),
                    LastError = snapshotType.GetProperty("LastError")
                };
                VersionSnapshotShapeCache[snapshotType] = shape;
                return shape;
            }
        }

        private static DiagnosticsShape GetDiagnosticsShape(Type reportType)
        {
            lock (CacheLock)
            {
                if (DiagnosticsShapeCache.TryGetValue(reportType, out var shape))
                    return shape;

                shape = new DiagnosticsShape
                {
                    DisplayName = reportType.GetProperty("DisplayName"),
                    PluginVersion = reportType.GetProperty("PluginVersion"),
                    SharedCodeRevision = reportType.GetProperty("SharedCodeRevision"),
                    IntegrationSummary = reportType.GetProperty("IntegrationSummary"),
                    Mode = reportType.GetProperty("Mode"),
                    CharacterName = reportType.GetProperty("CharacterName"),
                    DataLoaded = reportType.GetProperty("DataLoaded"),
                    LastPersistenceOutcome = reportType.GetProperty("LastPersistenceOutcome"),
                    LastError = reportType.GetProperty("LastError"),
                    ReportedUtc = reportType.GetProperty("ReportedUtc")
                };
                DiagnosticsShapeCache[reportType] = shape;
                return shape;
            }
        }

        private static List<FieldInfo> GetCachedVersionCheckerFields() =>
            GetCachedStaticDictionaryFields(
                "SunhavenMods.Shared.VersionChecker",
                "HealthByPluginGuid",
                ref _cachedVersionCheckerFields);

        private static List<FieldInfo> GetCachedDiagnosticsFields() =>
            GetCachedStaticDictionaryFields(
                "SunhavenMods.Shared.ModDiagnostics",
                "ReportsByPluginGuid",
                ref _cachedDiagnosticsFields);

        private static List<FieldInfo> GetCachedStaticDictionaryFields(
            string typeName,
            string fieldName,
            ref List<FieldInfo> cache)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            lock (CacheLock)
            {
                bool stale = cache == null
                    || assemblies.Length != _cachedAssemblyCount
                    || DateTime.UtcNow - _cacheBuiltAt > CacheMaxAge;

                if (!stale)
                    return cache;

                var found = new List<FieldInfo>();
                foreach (var asm in assemblies)
                {
                    Type type = null;
                    try { type = asm.GetType(typeName, throwOnError: false); }
                    catch { /* some assemblies can't be introspected */ }
                    if (type == null) continue;

                    var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
                    if (field == null) continue;

                    found.Add(field);
                }

                cache = found;
                _cachedAssemblyCount = assemblies.Length;
                _cacheBuiltAt = DateTime.UtcNow;
                return cache;
            }
        }
    }
}
