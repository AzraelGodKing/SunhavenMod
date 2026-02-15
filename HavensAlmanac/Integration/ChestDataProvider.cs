using System;
using System.Collections;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    /// <summary>
    /// Data provider for Senpai's Chest.
    /// Uses reflection because SenpaisChest.Plugin.GetManager() is internal.
    /// </summary>
    public class ChestDataProvider : IModDataProvider
    {
        public string ModName => "Chests";
        public string ModIcon => "\u25A3";

        private int _smartChestCount;
        private string _hudSummary = "Loading...";
        private bool _isReady;

        public string HudSummary => _hudSummary;
        public bool IsReady => _isReady;

        public void Refresh()
        {
            try
            {
                // Access SenpaisChest.Plugin.GetManager() via reflection (it's internal)
                var pluginType = ReflectionHelper.FindType("SenpaisChest.Plugin");
                if (pluginType == null) { _isReady = false; return; }

                var manager = ReflectionHelper.InvokeStaticMethod(pluginType, "GetManager");
                if (manager == null) { _isReady = false; return; }

                // Get save data to count configured smart chests
                var saveData = ReflectionHelper.InvokeMethod(manager, "GetSaveData");
                if (saveData != null)
                {
                    // SmartChestSaveData has a ChestConfigs property/field
                    var chestConfigs = ReflectionHelper.GetInstanceValue(saveData, "ChestConfigs");
                    if (chestConfigs is ICollection collection)
                        _smartChestCount = collection.Count;
                    else
                        _smartChestCount = 0;
                }
                else
                {
                    _smartChestCount = 0;
                }

                _hudSummary = _smartChestCount == 0
                    ? "No smart chests"
                    : $"{_smartChestCount} smart chest{(_smartChestCount != 1 ? "s" : "")}";
                _isReady = true;
            }
            catch (Exception ex)
            {
                _hudSummary = "Error";
                _isReady = false;
                HavensAlmanac.Plugin.Log?.LogWarning($"[ChestProvider] Refresh error: {ex.Message}");
            }
        }

        public void DrawDashboardSection()
        {
            GUILayout.Label($"Configured Smart Chests: {_smartChestCount}");
        }

        public bool DrawBriefingSection()
        {
            // Chest data is not daily-relevant
            return false;
        }
    }
}
