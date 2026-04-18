using System;
using HavensAlmanac.Data;
using SunHavenMuseumUtilityTracker;
using SunHavenMuseumUtilityTracker.Data;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class MuseumDataProvider : IModDataProvider
    {
        public string ModName => "Museum";
        public string ModIcon => "\u2302";

        private int _donated;
        private int _total;
        private float _completionPercent;
        private int _neededCount;
        private string _hudSummary = "Loading...";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        // Museum progress is persistent (no new donation windows open daily),
        // so the HUD is the right surface for routine progress. The briefing
        // only pipes up when the player is close to finishing (within the
        // threshold below) so they get a useful nudge before bed/today's run.
        private const int BriefingNearCompletionThreshold = 5;
        public bool HasBriefingContent =>
            _isReady && _neededCount > 0 && _neededCount <= BriefingNearCompletionThreshold;

        public void Refresh()
        {
            try
            {
                var dm = SunHavenMuseumUtilityTracker.Plugin.GetDonationManager();
                if (dm == null || !dm.IsLoaded) { _isReady = false; return; }

                var stats = dm.GetOverallStats();
                _donated = stats.donated;
                _total = stats.total;
                _completionPercent = dm.GetOverallCompletionPercent();

                var neededItems = dm.GetAllNeededItems();
                _neededCount = neededItems?.Count ?? 0;

                _hudSummary = $"{_completionPercent:F0}% ({_donated}/{_total})";
                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[MuseumProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            GUILayout.Label($"Donated: {_donated} / {_total}");

            // Progress bar
            var barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            GUI.Box(barRect, "");
            if (_total > 0)
            {
                float fraction = (float)_donated / _total;
                var fillRect = new Rect(barRect.x + 1, barRect.y + 1, (barRect.width - 2) * fraction, barRect.height - 2);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }
            GUILayout.Label($"{_completionPercent:F1}% complete");

            if (_neededCount > 0)
                GUILayout.Label($"{_neededCount} items still needed");
        }

        public bool DrawBriefingSection()
        {
            // Mirror HasBriefingContent: only nudge when within the
            // near-completion threshold so the briefing isn't daily filler.
            if (!_isReady || _neededCount <= 0 || _neededCount > BriefingNearCompletionThreshold)
                return false;

            GUILayout.Label($"Museum: {_completionPercent:F0}% complete ({_donated}/{_total})");
            GUILayout.Label($"  Only {_neededCount} item{(_neededCount != 1 ? "s" : "")} left!");
            return true;
        }
    }
}
