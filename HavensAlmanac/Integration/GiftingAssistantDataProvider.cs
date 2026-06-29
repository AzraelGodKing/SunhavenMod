using System;
using System.Collections.Generic;
using GiftingAssistant.Data;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class GiftingAssistantDataProvider : IModDataProvider
    {
        public string ModName => "Gifting";
        public string ModIcon => "\u2665";

        private string _hudSummary = "Loading...";
        private bool _isReady;
        private bool _integrationEnabled;
        private int _rosterCount;
        private int _pendingCount;
        private int _highPriorityPending;
        private int _urgentPending;
        private readonly List<GiftingAssistantAlmanacData.RosterEntrySnapshot> _pendingEntries =
            new List<GiftingAssistantAlmanacData.RosterEntrySnapshot>();

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;
        public bool HasBriefingContent => _isReady && _integrationEnabled && _pendingCount > 0;

        public void Refresh()
        {
            _integrationEnabled = GiftingAssistantAlmanacData.IsIntegrationEnabled;
            _rosterCount = 0;
            _pendingCount = 0;
            _highPriorityPending = 0;
            _urgentPending = 0;
            _pendingEntries.Clear();

            try
            {
                if (!_integrationEnabled)
                {
                    _hudSummary = ModLocalization.T("almanac.provider.gifting.disabled");
                    _isReady = true;
                    return;
                }

                if (!GiftingAssistantAlmanacData.TryGetSummary(out _hudSummary, out _rosterCount, out _pendingCount))
                {
                    _hudSummary = "Not ready";
                    _isReady = false;
                    return;
                }

                foreach (var entry in GiftingAssistantAlmanacData.GetSortedRosterEntries())
                {
                    if (entry.IsGiftedToday)
                        continue;

                    _pendingEntries.Add(entry);
                    if (entry.Priority >= GiftPriority.High)
                        _highPriorityPending++;
                    if (entry.Priority >= GiftPriority.Urgent)
                        _urgentPending++;
                }

                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[GiftingProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            if (!_integrationEnabled)
            {
                GUILayout.Label(ModLocalization.T("almanac.provider.gifting.disabled"));
                return;
            }

            if (_rosterCount == 0)
            {
                GUILayout.Label(ModLocalization.T("almanac.provider.gifting.noRoster"));
                return;
            }

            GUILayout.Label(ModLocalization.T("almanac.provider.gifting.stats", _rosterCount, _pendingCount));

            if (_pendingEntries.Count == 0)
                return;

            GUILayout.Space(4);
            GUILayout.Label(ModLocalization.T("almanac.provider.gifting.pendingHeader"), GUI.skin.label);
            foreach (var entry in _pendingEntries)
            {
                GUILayout.Label(ModLocalization.T("almanac.provider.gifting.priorityRow",
                    entry.Priority, entry.NpcName, ModLocalization.T("almanac.provider.gifting.notGifted")));
            }
        }

        public bool DrawBriefingSection()
        {
            if (!_integrationEnabled || _pendingCount == 0)
                return false;

            string npcWord = _pendingCount == 1
                ? ModLocalization.T("almanac.provider.gifting.npc")
                : ModLocalization.T("almanac.provider.gifting.npcs");
            GUILayout.Label(ModLocalization.T("almanac.provider.gifting.briefing.remaining", _pendingCount, npcWord));

            if (_highPriorityPending > 0)
                GUILayout.Label(ModLocalization.T("almanac.provider.gifting.briefing.high", _highPriorityPending));
            if (_urgentPending > 0)
                GUILayout.Label(ModLocalization.T("almanac.provider.gifting.briefing.urgent", _urgentPending));

            return true;
        }
    }
}
