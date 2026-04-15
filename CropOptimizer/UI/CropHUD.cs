using CropOptimizer.Data;
using UnityEngine;

namespace CropOptimizer.UI
{
    internal sealed class CropHUD : MonoBehaviour
    {
        private CropForecast _forecast;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private float _scale = 1f;
        private bool _isVisible = true;

        public void Initialize(CropForecast forecast)
        {
            _forecast = forecast;
        }

        public void SetScale(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.5f, 2.5f);
            InitializeStyles();
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
        }

        private void Awake()
        {
            InitializeStyles();
        }

        private void OnEnable()
        {
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            if (_windowStyle == null)
            {
                _windowStyle = new GUIStyle(GUI.skin.window)
                {
                    fontSize = Mathf.RoundToInt(12f * _scale)
                };
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(11f * _scale),
                    richText = true
                };
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _forecast == null)
                return;
            if (_windowStyle == null || _labelStyle == null)
                InitializeStyles();

            GUILayout.BeginArea(new Rect(20, 80, 340 * _scale, 120 * _scale), "Crop Optimizer", _windowStyle);
            GUILayout.Label($"Tracked crops: {_forecast.Snapshot().Count}", _labelStyle);
            GUILayout.Label($"Projected sell value: {_forecast.GetProjectedSellTotal()}g", _labelStyle);
            GUILayout.EndArea();
        }
    }
}
