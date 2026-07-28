using System;
using System.Collections.Generic;
using HavensAlmanac.Data;
using BirthdayReminder;
using BirthdayReminder.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class BirthdayDataProvider : IModDataProvider
    {
        public string ModName => "Birthday";
        public string ModIcon => "\u2605";

        private List<BirthdayDisplayInfo> _birthdays = new List<BirthdayDisplayInfo>();
        private int _ungiftedCount;
        private string _hudSummary = "Loading...";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;
        public bool HasBriefingContent => _isReady && _birthdays != null && _birthdays.Count > 0;

        public void Refresh()
        {
            try
            {
                var manager = BirthdayReminder.Plugin.GetManager();
                if (manager == null) { _isReady = false; return; }

                _birthdays = new List<BirthdayDisplayInfo>((IEnumerable<BirthdayDisplayInfo>)manager.TodaysBirthdays ?? Array.Empty<BirthdayDisplayInfo>());
                _ungiftedCount = 0;
                foreach (var b in _birthdays)
                {
                    if (!b.HasBeenGifted) _ungiftedCount++;
                }

                if (_birthdays.Count == 0)
                    _hudSummary = "No birthdays";
                else
                    _hudSummary = $"{_birthdays.Count} birthday{(_birthdays.Count != 1 ? "s" : "")} / {_ungiftedCount} ungifted";

                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[BirthdayProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            if (_birthdays.Count == 0)
            {
                GUILayout.Label(ModLocalization.T("almanac.provider.birthday.noBirthdays"));
                return;
            }

            GUILayout.Label(ModLocalization.T("almanac.provider.birthday.todayList", _birthdays.Count));
            GUILayout.Space(4);

            foreach (var birthday in _birthdays)
            {
                string status = birthday.HasBeenGifted
                    ? ModLocalization.T("almanac.provider.birthday.gifted")
                    : ModLocalization.T("almanac.provider.birthday.notGifted");
                GUILayout.Label($"  {birthday.NPCName}{status}");

                if (!string.IsNullOrEmpty(birthday.GiftHint))
                {
                    GUILayout.Label($"    {birthday.GiftHint}");
                }
            }
        }

        public bool DrawBriefingSection()
        {
            if (_birthdays.Count == 0) return false;

            foreach (var birthday in _birthdays)
            {
                string status = birthday.HasBeenGifted
                    ? ModLocalization.T("almanac.provider.birthday.giftedBriefing")
                    : "";
                GUILayout.Label(ModLocalization.T("almanac.provider.birthday.briefing", birthday.NPCName, status));
            }

            if (_ungiftedCount > 0)
                GUILayout.Label(ModLocalization.T("almanac.provider.birthday.reminder"));

            return true;
        }
    }
}
