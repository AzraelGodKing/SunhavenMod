using System;
using System.Collections.Generic;
using HavensAlmanac.Data;
using HavensBirthright;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class BirthrightDataProvider : IModDataProvider
    {
        public string ModName => "Birthright";
        public string ModIcon => "\u2694";

        private string _raceName = "None";
        private int _bonusCount;
        private List<RacialBonus> _bonuses = new List<RacialBonus>();
        private string _hudSummary = "Loading...";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        // Racial bonuses are static per-character; nothing daily to brief.
        public bool HasBriefingContent => false;

        public void Refresh()
        {
            try
            {
                var manager = HavensBirthright.Plugin.GetRacialBonusManager();
                if (manager == null) { _isReady = false; return; }

                var race = manager.GetPlayerRace();
                if (race.HasValue)
                {
                    _raceName = race.Value.ToString();
                    _bonuses = manager.GetCurrentPlayerBonuses() ?? new List<RacialBonus>();
                    _bonusCount = _bonuses.Count;
                    _hudSummary = $"{_raceName} ({_bonusCount} bonus{(_bonusCount != 1 ? "es" : "")})";
                }
                else
                {
                    _raceName = "None";
                    _bonusCount = 0;
                    _bonuses.Clear();
                    _hudSummary = "No race set";
                }

                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[BirthrightProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            GUILayout.Label(ModLocalization.T("almanac.provider.birthright.race", _raceName));

            if (_bonusCount == 0)
            {
                GUILayout.Label(ModLocalization.T("almanac.provider.birthright.noBonuses"));
                return;
            }

            GUILayout.Label(ModLocalization.T("almanac.provider.birthright.activeBonuses", _bonusCount));
            GUILayout.Space(4);

            foreach (var bonus in _bonuses)
            {
                string value = bonus.GetFormattedValue();
                GUILayout.Label($"  {bonus.Description}: {value}");
            }
        }

        public bool DrawBriefingSection()
        {
            // Racial data is static, not daily-relevant
            return false;
        }
    }
}
