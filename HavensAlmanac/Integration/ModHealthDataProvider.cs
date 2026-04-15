using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class ModHealthDataProvider : IModDataProvider
    {
        public string ModName => "Mod Health";
        public string ModIcon => "+";

        private readonly List<string> _lines = new List<string>();
        private string _hudSummary = "No checks";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        public void Refresh()
        {
            _lines.Clear();
            try
            {
                int tracked = 0;
                int issues = 0;
                foreach (var kvp in Chainloader.PluginInfos)
                {
                    string guid = kvp.Key;
                    var snapshot = VersionChecker.GetHealthSnapshot(guid);
                    if (snapshot == null)
                        continue;

                    tracked++;
                    if (snapshot.ExceptionCount > 0)
                        issues++;

                    string when = snapshot.LastCheckUtc == default
                        ? "never"
                        : snapshot.LastCheckUtc.ToLocalTime().ToString("HH:mm:ss");
                    _lines.Add($"{kvp.Value.Metadata.Name}: last check {when}, exceptions {snapshot.ExceptionCount}");
                }

                _hudSummary = tracked == 0 ? "No checks" : $"{tracked} checked / {issues} issues";
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
    }
}
