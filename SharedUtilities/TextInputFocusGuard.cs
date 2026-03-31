using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Detects when the player is typing in chat, a console, or uGUI/IMGUI text fields so mod hotkeys can defer.
    /// Also treats QFSW Quantum Console as active when present. Designed for low overhead: throttled polling,
    /// cached reflection, and no per-scan GetComponents allocations.
    /// </summary>
    public static class TextInputFocusGuard
    {
        /// <summary>How often a full focus scan runs (all mods share one cache).</summary>
        private const float DefaultPollIntervalSeconds = 0.25f;

        private static float _nextPollTime = -1f;
        private static bool _cachedDefer;

        private static bool _tmpTypeLookupDone;
        private static Type _tmpInputFieldType;

        private static bool _qcLookupDone;
        private static Type _qcType;
        private static PropertyInfo _qcInstanceProp;
        private static PropertyInfo _qcIsActiveProp;
        private static FieldInfo _qcIsActiveField;

        /// <summary>
        /// When true, skip handling custom mod hotkeys (polling is throttled for performance).
        /// </summary>
        /// <param name="debugLog">Optional log for focus exceptions at Debug level.</param>
        /// <param name="pollIntervalSeconds">Minimum seconds between full scans (default 0.25).</param>
        public static bool ShouldDeferModHotkeys(ManualLogSource debugLog = null, float pollIntervalSeconds = DefaultPollIntervalSeconds)
        {
            float now = Time.realtimeSinceStartup;
            if (now < _nextPollTime)
                return _cachedDefer;

            _nextPollTime = now + Mathf.Max(0.05f, pollIntervalSeconds);
            bool defer = false;
            try
            {
                if (GUIUtility.keyboardControl != 0)
                    defer = true;

                if (!defer)
                {
                    var es = EventSystem.current;
                    var go = es?.currentSelectedGameObject;
                    if (go != null)
                    {
                        if (go.GetComponent<InputField>() != null)
                            defer = true;
                        else if (TryGetTmpInputField(go))
                            defer = true;
                    }
                }

                if (!defer && IsQuantumConsoleActive())
                    defer = true;
            }
            catch (Exception ex)
            {
                debugLog?.LogDebug($"[TextInputFocusGuard] {ex.Message}");
            }

            _cachedDefer = defer;
            return defer;
        }

        private static bool TryGetTmpInputField(GameObject go)
        {
            if (!_tmpTypeLookupDone)
            {
                _tmpTypeLookupDone = true;
                _tmpInputFieldType = AccessTools.TypeByName("TMPro.TMP_InputField");
            }

            if (_tmpInputFieldType == null)
                return false;

            return go.GetComponent(_tmpInputFieldType) != null;
        }

        private static bool IsQuantumConsoleActive()
        {
            try
            {
                if (!_qcLookupDone)
                {
                    _qcLookupDone = true;
                    _qcType = AccessTools.TypeByName("QFSW.QC.QuantumConsole");
                    if (_qcType != null)
                    {
                        _qcInstanceProp = AccessTools.Property(_qcType, "Instance");
                        _qcIsActiveProp = AccessTools.Property(_qcType, "IsActive");
                        _qcIsActiveField = AccessTools.Field(_qcType, "isActive") ?? AccessTools.Field(_qcType, "_isActive");
                    }
                }

                if (_qcType == null)
                    return false;

                var instance = _qcInstanceProp?.GetValue(null);
                if (instance == null)
                    return false;

                if (_qcIsActiveProp != null && _qcIsActiveProp.PropertyType == typeof(bool))
                    return (bool)_qcIsActiveProp.GetValue(instance);

                if (_qcIsActiveField != null && _qcIsActiveField.FieldType == typeof(bool))
                    return (bool)_qcIsActiveField.GetValue(instance);
            }
            catch
            {
            }

            return false;
        }
    }
}
