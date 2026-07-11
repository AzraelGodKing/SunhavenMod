using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    /// <summary>
    /// Soft bridge to Haven Dev Tools → Health tab. Registered only when Dev Tools is installed.
    /// </summary>
    public class ModHealthBridgeProvider : IModDataProvider
    {
        private const string DevToolsGuid = "com.azraelgodking.havendevtools";

        public string ModName => ModLocalization.T("almanac.provider.healthbridge.title");
        public string ModIcon => "+";

        private string _hudSummary = "";
        private string _detailLine = "";
        private string _pointerLine = "";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;
        public bool HasBriefingContent => false;

        public void Refresh()
        {
            _isReady = false;
            _hudSummary = "";
            _detailLine = "";
            _pointerLine = "";

            if (!IsDevToolsInstalled())
                return;

            try
            {
                var rows = ModHealthAggregator.CollectMergedRows(ResolveDisplayName, ResolveInstalledVersion);
                var skew = ModHealthAggregator.AnalyzeSharedCodeSkew(rows);

                int issues = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].HasIssue)
                        issues++;
                }

                _hudSummary = ModLocalization.T(
                    "almanac.provider.healthbridge.summary",
                    issues.ToString());

                _detailLine = skew.HasSkew
                    ? ModLocalization.T("almanac.provider.healthbridge.skew")
                    : ModLocalization.T("almanac.provider.healthbridge.ok");

                _pointerLine = ModLocalization.T("almanac.provider.healthbridge.pointer");
                _isReady = true;
            }
            catch (Exception ex)
            {
                HavensAlmanac.Plugin.Log?.LogWarning($"[ModHealthBridge] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            if (!_isReady)
                return;

            GUILayout.Label(_detailLine);
            GUILayout.Label(_pointerLine);
        }

        public bool DrawBriefingSection() => false;

        private static bool IsDevToolsInstalled()
        {
            try
            {
                return Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey(DevToolsGuid);
            }
            catch
            {
                return false;
            }
        }

        private static string ResolveDisplayName(string pluginGuid)
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
                // ignore
            }

            return null;
        }

        private static string ResolveInstalledVersion(string pluginGuid)
        {
            try
            {
                if (Chainloader.PluginInfos != null
                    && Chainloader.PluginInfos.TryGetValue(pluginGuid, out var info)
                    && info?.Metadata != null)
                {
                    return info.Metadata.Version.ToString();
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }
    }
}
