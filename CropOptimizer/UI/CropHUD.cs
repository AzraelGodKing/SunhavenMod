using CropOptimizer.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace CropOptimizer.UI
{
    internal sealed class CropHUD : PersistentRunnerBase
    {
        private CropForecast _forecast;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private float _scale = 1f;
        private bool _isVisible = true;
        private bool _stylesDirty = true;

        protected override string RunnerName => "CropHUD";

        public void Initialize(CropForecast forecast)
        {
            _forecast = forecast;
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

        private void OnGUI()
        {
            if (!_isVisible || _forecast == null)
                return;
            if (_stylesDirty || _windowStyle == null || _labelStyle == null)
                InitializeStyles();

            GUILayout.BeginArea(new Rect(20, 80, 340 * _scale, 120 * _scale), "Crop Optimizer", _windowStyle);
            GUILayout.Label($"Tracked crops: {_forecast.Snapshot().Count}", _labelStyle);
            GUILayout.Label($"Projected sell value: {_forecast.GetProjectedSellTotal()}g", _labelStyle);
            GUILayout.EndArea();
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
