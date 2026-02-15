using System;
using System.Collections.Generic;
using HavensAlmanac.Data;
using BirthdayReminder;
using BirthdayReminder.Data;
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

        public void Refresh()
        {
            try
            {
                var manager = BirthdayReminder.Plugin.GetManager();
                if (manager == null) { _isReady = false; return; }

                _birthdays = manager.TodaysBirthdays ?? new List<BirthdayDisplayInfo>();
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
                GUILayout.Label("No birthdays today.");
                return;
            }

            GUILayout.Label($"Today's Birthdays ({_birthdays.Count}):");
            GUILayout.Space(4);

            foreach (var birthday in _birthdays)
            {
                string status = birthday.HasBeenGifted ? " [Gifted]" : " [Not gifted]";
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
                string status = birthday.HasBeenGifted ? " (gifted)" : "";
                GUILayout.Label($"  It's {birthday.NPCName}'s birthday!{status}");
            }

            if (_ungiftedCount > 0)
                GUILayout.Label($"  Don't forget to bring a gift!");

            return true;
        }
    }
}
