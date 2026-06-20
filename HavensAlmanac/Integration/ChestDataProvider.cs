using System;
using System.Collections;
using System.Reflection;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.Integration
{
    /// <summary>
    /// Data provider for Senpai's Chest.
    /// Uses reflection because SenpaisChest.Plugin.GetManager() is internal,
    /// and caches all reflected members so Refresh() only pays resolution cost
    /// on the very first successful probe (and once more if the manager ever
    /// returns a save-data object whose concrete type differs from the one we
    /// cached, which realistically never happens mid-session).
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

        public bool HasBriefingContent => false;

        // Cached reflection handles. Any of these may be null until the
        // plugin/manager types first become resolvable.
        private Type _pluginType;
        private MethodInfo _getManagerMethod;
        private Type _managerType;
        private MethodInfo _getSaveDataMethod;
        private Type _saveDataType;
        private PropertyInfo _chestConfigsProperty;
        private FieldInfo _chestConfigsField;

        public void Refresh()
        {
            try
            {
                if (!TryResolvePluginType()) { _isReady = false; return; }

                var manager = GetManagerCached();
                if (manager == null) { _isReady = false; return; }

                var saveData = GetSaveDataCached(manager);
                _smartChestCount = saveData == null
                    ? 0
                    : ReadChestConfigCount(saveData);

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

        private bool TryResolvePluginType()
        {
            if (_pluginType != null) return true;

            // Only resolve once per session; if the type still isn't loaded
            // we bail quickly and try again on the next refresh tick.
            _pluginType = ReflectionHelper.FindType("SenpaisChest.Plugin");
            return _pluginType != null;
        }

        private object GetManagerCached()
        {
            if (_getManagerMethod == null)
            {
                _getManagerMethod = _pluginType.GetMethod(
                    "GetManager",
                    ReflectionHelper.AllBindingFlags,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                if (_getManagerMethod == null)
                    return null;
            }

            return _getManagerMethod.Invoke(null, null);
        }

        private object GetSaveDataCached(object manager)
        {
            // Manager's runtime type should be stable across a session; if it
            // ever drifts (reinit, reload) we re-resolve defensively.
            var currentManagerType = manager.GetType();
            if (_managerType != currentManagerType || _getSaveDataMethod == null)
            {
                _managerType = currentManagerType;
                _getSaveDataMethod = _managerType.GetMethod(
                    "GetSaveData",
                    ReflectionHelper.AllBindingFlags,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
            }

            return _getSaveDataMethod?.Invoke(manager, null);
        }

        private int ReadChestConfigCount(object saveData)
        {
            var currentSaveDataType = saveData.GetType();
            if (_saveDataType != currentSaveDataType)
            {
                _saveDataType = currentSaveDataType;
                _chestConfigsProperty = _saveDataType.GetProperty("ChestConfigs", ReflectionHelper.AllBindingFlags);
                _chestConfigsField = _chestConfigsProperty == null
                    ? _saveDataType.GetField("ChestConfigs", ReflectionHelper.AllBindingFlags)
                    : null;
            }

            object value = _chestConfigsProperty != null
                ? _chestConfigsProperty.GetValue(saveData)
                : _chestConfigsField?.GetValue(saveData);

            return value is ICollection collection ? collection.Count : 0;
        }

        public void DrawDashboardSection()
        {
            GUILayout.Label(ModLocalization.T("almanac.provider.chest.count", _smartChestCount));
        }

        public bool DrawBriefingSection()
        {
            return false;
        }
    }
}
