using System;
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

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        public void Refresh()
        {
            try
            {
                _hudSummary = CropOptimizer.Data.CropOptimizerDataProvider.GetSummary();
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
        }

        public bool DrawBriefingSection()
        {
            return false;
        }
    }
}
