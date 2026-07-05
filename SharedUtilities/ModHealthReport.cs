using System;
using System.Text;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Structured startup / runtime health snapshot per MOD_LIFECYCLE_AND_LOGGING_CONTRACT §5.
    /// </summary>
    public sealed class ModHealthReport
    {
        public string PluginGuid { get; set; }
        public string DisplayName { get; set; }
        public string PluginVersion { get; set; }
        public string SharedCodeRevision { get; set; }
        public string IntegrationSummary { get; set; }
        public string Mode { get; set; }
        public string CharacterName { get; set; }
        public bool? DataLoaded { get; set; }
        public string LastPersistenceOutcome { get; set; }
        public string LastError { get; set; }
        public DateTime ReportedUtc { get; set; }

        public ModHealthReport Clone()
        {
            return new ModHealthReport
            {
                PluginGuid = PluginGuid,
                DisplayName = DisplayName,
                PluginVersion = PluginVersion,
                SharedCodeRevision = SharedCodeRevision,
                IntegrationSummary = IntegrationSummary,
                Mode = Mode,
                CharacterName = CharacterName,
                DataLoaded = DataLoaded,
                LastPersistenceOutcome = LastPersistenceOutcome,
                LastError = LastError,
                ReportedUtc = ReportedUtc
            };
        }

        public void AppendDiagnosticDump(StringBuilder sb)
        {
            if (sb == null)
                return;

            sb.AppendLine($"guid: {PluginGuid ?? "—"}");
            sb.AppendLine($"name: {DisplayName ?? "—"}");
            sb.AppendLine($"version: {PluginVersion ?? "—"}");
            sb.AppendLine($"shared: {SharedCodeRevision ?? "—"}");
            sb.AppendLine($"integrations: {IntegrationSummary ?? "—"}");
            sb.AppendLine($"mode: {Mode ?? "—"}");
            sb.AppendLine($"character: {(string.IsNullOrEmpty(CharacterName) ? "—" : CharacterName)}");
            sb.AppendLine($"data loaded: {(DataLoaded.HasValue ? DataLoaded.Value.ToString() : "—")}");
            sb.AppendLine($"last save: {LastPersistenceOutcome ?? "—"}");
            sb.AppendLine($"last error: {LastError ?? "—"}");
            sb.AppendLine($"reported: {(ReportedUtc == default ? "—" : ReportedUtc.ToLocalTime().ToString("u"))}");
        }
    }
}
