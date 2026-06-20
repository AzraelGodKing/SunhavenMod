using System;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class DevToolsDataProvider : IModDataProvider
    {
        public string ModName => "DevTools";
        public string ModIcon => "\u2692";

        private bool _isAuthorized;
        private string _playerName = "";
        private string _hudSummary = "Loading...";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        // Dev tools authorization is not daily-relevant.
        public bool HasBriefingContent => false;

        public void Refresh()
        {
            try
            {
                _isAuthorized = HavenDevTools.Plugin.IsAuthorized;
                _playerName = HavenDevTools.Plugin.CurrentPlayerName ?? "";
                _hudSummary = _isAuthorized ? "Active" : "Inactive";
                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[DevToolsProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            string status = _isAuthorized
                ? ModLocalization.T("almanac.provider.devtools.authorized")
                : ModLocalization.T("almanac.provider.devtools.notAuthorized");
            GUILayout.Label(ModLocalization.T("almanac.provider.devtools.status", status));
            if (!string.IsNullOrEmpty(_playerName))
                GUILayout.Label(ModLocalization.T("almanac.provider.devtools.player", _playerName));
        }

        public bool DrawBriefingSection()
        {
            // Dev tools status is not daily-relevant
            return false;
        }
    }
}
