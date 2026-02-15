using System;
using System.Collections.Generic;
using HavensAlmanac.Config;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.UI
{
    /// <summary>
    /// Full expandable dashboard window with detailed sections for each installed mod.
    /// </summary>
    public class AlmanacDashboard : MonoBehaviour
    {
        private const int WINDOW_ID = 98781;
        private const float WIDTH = 500f;
        private const float HEIGHT = 550f;

        private AlmanacDataAggregator _aggregator;
        private Rect _windowRect;
        private bool _isVisible;
        private Vector2 _scrollPosition;
        private Dictionary<string, bool> _sectionExpanded = new Dictionary<string, bool>();

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionHeaderStyle;
        private GUIStyle _sectionHeaderExpandedStyle;
        private GUIStyle _contentStyle;
        private GUIStyle _closeButtonStyle;
        private GUIStyle _noModsStyle;
        private Texture2D _bgTexture;
        private Texture2D _sectionBgTexture;
        private Texture2D _sectionExpandedBgTexture;

        public bool IsVisible => _isVisible;

        public void Initialize(AlmanacDataAggregator aggregator)
        {
            _aggregator = aggregator;
            _windowRect = new Rect(
                (Screen.width - WIDTH) / 2f,
                (Screen.height - HEIGHT) / 2f,
                WIDTH, HEIGHT);
        }

        public void Show()
        {
            _aggregator?.RefreshAll();
            _isVisible = true;
        }

        public void Hide() => _isVisible = false;

        public void Toggle()
        {
            if (_isVisible) Hide();
            else Show();
        }

        private void Update()
        {
            if (_isVisible && Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        private void OnGUI()
        {
            if (!_isVisible || _aggregator == null) return;

            if (!_stylesInitialized)
                InitializeStyles();

            _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow, "", _windowStyle);
        }

        private void DrawWindow(int id)
        {
            // Header bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Haven's Almanac - Dashboard", _titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", _closeButtonStyle, GUILayout.Width(24), GUILayout.Height(24)))
                Hide();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (_aggregator.InstalledModCount == 0)
            {
                GUILayout.Label("No mods detected. Install at least one supported mod to see data here.", _noModsStyle);
                GUI.DragWindow(new Rect(0, 0, _windowRect.width, 30));
                return;
            }

            // Scrollable content
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (var provider in _aggregator.Providers)
            {
                if (!_sectionExpanded.ContainsKey(provider.ModName))
                    _sectionExpanded[provider.ModName] = true;

                bool expanded = _sectionExpanded[provider.ModName];
                var headerStyle = expanded ? _sectionHeaderExpandedStyle : _sectionHeaderStyle;
                string arrow = expanded ? "\u25BC" : "\u25B6";

                if (GUILayout.Button($"{arrow}  {provider.ModIcon}  {provider.ModName}  —  {provider.HudSummary}", headerStyle))
                {
                    _sectionExpanded[provider.ModName] = !expanded;
                }

                if (expanded)
                {
                    GUILayout.BeginVertical(_contentStyle);
                    try
                    {
                        provider.DrawDashboardSection();
                    }
                    catch (Exception ex)
                    {
                        GUILayout.Label($"Error: {ex.Message}");
                    }
                    GUILayout.EndVertical();
                }

                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();

            // Drag header area only
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 30));
        }

        private void InitializeStyles()
        {
            _stylesInitialized = true;

            var bgColor = new Color(0.12f, 0.10f, 0.08f, 0.96f);
            var sectionColor = new Color(0.18f, 0.15f, 0.11f, 0.9f);
            var sectionExpandedColor = new Color(0.22f, 0.18f, 0.13f, 0.9f);
            var goldText = new Color(0.95f, 0.85f, 0.55f);
            var lightText = new Color(0.91f, 0.87f, 0.82f);

            _bgTexture = GUIStyleHelper.MakeSolidTexture(bgColor);
            _sectionBgTexture = GUIStyleHelper.MakeSolidTexture(sectionColor);
            _sectionExpandedBgTexture = GUIStyleHelper.MakeSolidTexture(sectionExpandedColor);

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(12, 12, 10, 10),
                border = new RectOffset(2, 2, 2, 2)
            };
            _windowStyle.normal.background = _bgTexture;
            _windowStyle.onNormal.background = _bgTexture;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
            _titleStyle.normal.textColor = goldText;

            _sectionHeaderStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(8, 8, 6, 6)
            };
            _sectionHeaderStyle.normal.background = _sectionBgTexture;
            _sectionHeaderStyle.normal.textColor = lightText;
            _sectionHeaderStyle.hover.background = _sectionExpandedBgTexture;
            _sectionHeaderStyle.hover.textColor = goldText;

            _sectionHeaderExpandedStyle = new GUIStyle(_sectionHeaderStyle);
            _sectionHeaderExpandedStyle.normal.background = _sectionExpandedBgTexture;
            _sectionHeaderExpandedStyle.normal.textColor = goldText;

            _contentStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 8, 8)
            };
            _contentStyle.normal.textColor = lightText;

            _closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            _noModsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _noModsStyle.normal.textColor = new Color(0.7f, 0.6f, 0.5f);
        }

        private void OnDestroy()
        {
            if (_bgTexture != null) Destroy(_bgTexture);
            if (_sectionBgTexture != null) Destroy(_sectionBgTexture);
            if (_sectionExpandedBgTexture != null) Destroy(_sectionExpandedBgTexture);
        }
    }
}
