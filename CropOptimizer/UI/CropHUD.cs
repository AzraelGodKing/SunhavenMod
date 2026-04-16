using System;
using CropOptimizer.Data;
using SunhavenMods.Shared;
using UnityEngine;
using Wish;

namespace CropOptimizer.UI
{
    /// <summary>
    /// IMGUI crop summary panel. Draggable; position persisted via config from <see cref="Plugin"/>.
    /// Hidden until a character save is active (not main menu / not pre-load).
    /// </summary>
    internal sealed class CropHUD : PersistentRunnerBase
    {
        private const int HudWindowId = 948301;

        private CropForecast _forecast;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private float _scale = 1f;
        private bool _isVisible = true;
        private bool _stylesDirty = true;

        private Rect _windowRect;
        private bool _hasInitialRect;

        /// <summary>Invoked when the player drags the HUD; parent saves X/Y to config.</summary>
        public event Action<float, float> PlacementChanged;

        protected override string RunnerName => "CropHUD";

        public void Initialize(CropForecast forecast)
        {
            _forecast = forecast;
        }

        /// <summary>Initial anchor from config (pixels).</summary>
        public void SetPlacement(float x, float y)
        {
            _windowRect.x = x;
            _windowRect.y = y;
            _hasInitialRect = true;
        }

        public void SetScale(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.5f, 2.5f);
            _stylesDirty = true;
            _windowStyle = null;
            _labelStyle = null;
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
        }

        private static bool IsCharacterSessionActive()
        {
            if (!SceneHelpers.IsInGame())
                return false;
            try
            {
                var gs = GameSave.Instance;
                if (gs == null)
                    return false;
                var save = gs.CurrentSave;
                if (save == null)
                    return false;
                return save.characterData != null;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeStyles()
        {
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = Mathf.RoundToInt(12f * _scale)
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(11f * _scale),
                richText = true
            };

            _stylesDirty = false;
        }

        private void EnsureWindowSize()
        {
            _windowRect.width = 340f * _scale;
            _windowRect.height = 78f * _scale;
        }

        private void OnGUI()
        {
            if (!_isVisible || _forecast == null || !IsCharacterSessionActive())
                return;

            if (_stylesDirty || _windowStyle == null || _labelStyle == null)
                InitializeStyles();

            EnsureWindowSize();
            if (!_hasInitialRect)
            {
                _windowRect.x = 20f;
                _windowRect.y = 80f;
                _hasInitialRect = true;
            }

            GUI.depth = -50;
            Rect before = _windowRect;
            _windowRect = GUI.Window(HudWindowId, _windowRect, DrawHudWindow, "Crop Optimizer", _windowStyle);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));

            if (Mathf.Abs(_windowRect.x - before.x) > 0.5f || Mathf.Abs(_windowRect.y - before.y) > 0.5f)
                PlacementChanged?.Invoke(_windowRect.x, _windowRect.y);
        }

        private void DrawHudWindow(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"Tracked crops: {_forecast.Snapshot().Count}", _labelStyle);
            GUILayout.Label($"Projected sell value: {_forecast.GetProjectedSellTotal()}g", _labelStyle);
            GUILayout.EndVertical();

            // Drag from title bar only (window chrome); avoids stealing clicks from labels.
            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 22f * _scale));
        }

        protected override void OnGameTransition()
        {
            _stylesDirty = true;
            _windowStyle = null;
            _labelStyle = null;
        }

        protected override void OnMenuTransition()
        {
            _stylesDirty = true;
            _windowStyle = null;
            _labelStyle = null;
        }

        protected override void Log(string message)
        {
            Plugin.Log?.LogDebug(message);
        }

        protected override void LogWarning(string message)
        {
            Plugin.Log?.LogWarning(message);
        }
    }
}
