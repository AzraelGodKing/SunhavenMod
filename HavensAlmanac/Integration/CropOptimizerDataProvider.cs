using System;
using System.Collections.Generic;
using HavensAlmanac.Data;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    public class CropOptimizerDataProvider : IModDataProvider
    {
        public string ModName => "Crop Optimizer";
        public string ModIcon => "o";

        private string _hudSummary = "Loading...";
        private bool _isReady;
        private List<(string name, int totalGold, int count)> _topCrops = new List<(string, int, int)>();

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        // Crop Optimizer surfaces its own hover tooltip + forecast HUD in-game;
        // Almanac's morning briefing stays out of its way.
        public bool HasBriefingContent => false;

        public void Refresh()
        {
            try
            {
                _hudSummary = CropOptimizer.Data.CropOptimizerDataProvider.GetSummary();
                _topCrops.Clear();
                var tops = CropOptimizer.Data.CropOptimizerDataProvider.GetTopCrops(5);
                foreach (var t in tops)
                {
                    CropOptimizer.Data.CropOptimizerDataProvider.TryGetCropDisplayName(t.ItemId, out string name);
                    _topCrops.Add((name ?? $"Item #{t.ItemId}", t.TotalGold, t.CropCount));
                }
                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[CropOptimizerProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            GUILayout.Label(_hudSummary);
            if (_topCrops.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("Top crops by projected value:");
                for (int i = 0; i < _topCrops.Count; i++)
                {
                    var (name, gold, count) = _topCrops[i];
                    GUILayout.Label($"  {i + 1}. {name} — {gold}g ({count} plant{(count == 1 ? "" : "s")})");
                }
            }
        }

        public bool DrawBriefingSection()
        {
            return false;
        }
    }
}
