using System;
using HavensAlmanac.Config;
using HavensAlmanac.Data;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavensAlmanac.UI
{
    /// <summary>
    /// Morning summary popup that auto-shows when the player wakes up.
    /// Displays key info from each installed mod and auto-dismisses.
    /// </summary>
    public class DailyBriefing : MonoBehaviour
    {
        private const int WINDOW_ID = 98782;
        private const float BASE_WIDTH = 500f;
        private const float BASE_MIN_HEIGHT = 250f;

        private float _scale = 1f;
        private AlmanacDataAggregator _aggregator;
        private Rect _windowRect;
        private bool _isVisible;
        private float _contentHeight = BASE_MIN_HEIGHT;

        // Snapshot of auto-dismiss deadline taken when the window becomes
        // visible. Using unscaled time so the countdown keeps ticking even if
        // something (overnight fade, UI pause) zeros Time.timeScale.
        private float _visibleSinceUnscaledTime;
        private float _autoDismissSeconds;

        private float Width => BASE_WIDTH * _scale;
        private float MinHeight => BASE_MIN_HEIGHT * _scale;
        private float Scaled(float value) => value * _scale;
        private int ScaledFont(int baseSize) => Mathf.Max(8, Mathf.RoundToInt(baseSize * _scale));
        private int ScaledInt(float value) => Mathf.RoundToInt(value * _scale);

        // Styles
        private bool _stylesInitialized;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionTitleStyle;
        private GUIStyle _contentStyle;
        private GUIStyle _dismissButtonStyle;
        private Texture2D _bgTexture;

        public bool IsVisible => _isVisible;

        public void Initialize(AlmanacDataAggregator aggregator)
        {
            _aggregator = aggregator;
            CenterWindow();
        }

        public void ShowBriefing()
        {
            if (!AlmanacConfig.StaticBriefingEnabled) return;
            if (_aggregator == null || _aggregator.InstalledModCount == 0) return;

            _aggregator.RefreshAll();

            // Gate the window on the stricter HasBriefingContent contract rather
            // than plain IsReady: Mod Health (and other non-briefing providers)
            // are always ready but never emit briefing content, so IsReady would
            // open the window only to show a "Nothing noteworthy today." shell.
            if (!_aggregator.HasAnyBriefingContent) return;

            CenterWindow();
            _isVisible = true;
            _visibleSinceUnscaledTime = Time.unscaledTime;
            _autoDismissSeconds = Mathf.Max(0f, AlmanacConfig.StaticBriefingAutoDismiss);
        }

        public void Hide() => _isVisible = false;

        public void SetScale(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.5f, 2.5f);
            _stylesInitialized = false;
        }

        private void CenterWindow()
        {
            _windowRect = new Rect(
                (Screen.width - Width) / 2f,
                (Screen.height - MinHeight) / 2f - Scaled(50f),
                Width, MinHeight);
        }

        private void Update()
        {
            if (!_isVisible) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
                return;
            }

            // Auto-dismiss timer (0 = disabled, wait for manual dismiss).
            if (_autoDismissSeconds > 0f)
            {
                float elapsed = Time.unscaledTime - _visibleSinceUnscaledTime;
                if (elapsed >= _autoDismissSeconds)
                    Hide();
            }
        }

        private float GetAutoDismissRemaining()
        {
            if (_autoDismissSeconds <= 0f) return -1f;
            float remaining = _autoDismissSeconds - (Time.unscaledTime - _visibleSinceUnscaledTime);
            return Mathf.Max(0f, remaining);
        }

        private void OnGUI()
        {
            if (!_isVisible || _aggregator == null) return;

            if (!_stylesInitialized)
                InitializeStyles();

            float maxHeight = Screen.height - Scaled(60f);
            _windowRect.height = Mathf.Clamp(_contentHeight, MinHeight, maxHeight);

            _windowRect.x = (Screen.width - _windowRect.width) / 2f;
            _windowRect.y = Mathf.Clamp(_windowRect.y, Scaled(20f), Screen.height - _windowRect.height - Scaled(20f));

            _windowRect = GUI.Window(WINDOW_ID, _windowRect, DrawWindow, "", _windowStyle);
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(ModLocalization.T("almanac.briefing.title"), _titleStyle);
            GUILayout.Space(Scaled(6));

            bool anyContent = false;

            foreach (var provider in _aggregator.Providers)
            {
                // HasBriefingContent is the single source of truth for both
                // gating the window AND deciding whether to render this
                // provider's section, which guarantees no orphan section
                // headers when the provider has nothing to say.
                if (!provider.HasBriefingContent) continue;

                if (TryDrawProviderSection(provider))
                {
                    anyContent = true;
                    GUILayout.Space(Scaled(6));
                }
            }

            if (!anyContent)
            {
                // Defensive: HasAnyBriefingContent in ShowBriefing normally
                // prevents us from reaching here, but if a provider's content
                // evaporated between the gate and the draw pass we still want
                // a graceful fallback rather than a blank window.
                GUILayout.Label(ModLocalization.T("almanac.briefing.empty"), _contentStyle);
            }

            GUILayout.Space(Scaled(10));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(ModLocalization.T("almanac.briefing.dismiss"), _dismissButtonStyle, GUILayout.Width(Scaled(120)), GUILayout.Height(Scaled(30))))
                Hide();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            float remaining = GetAutoDismissRemaining();
            if (remaining >= 0f)
            {
                int seconds = Mathf.CeilToInt(remaining);
                GUILayout.Space(Scaled(4));
                GUILayout.Label(
                    $"Auto-dismissing in {seconds}s ({_autoDismissSeconds:F0}s total)",
                    _contentStyle);
            }

            if (Event.current.type == UnityEngine.EventType.Repaint)
            {
                var lastRect = GUILayoutUtility.GetLastRect();
                _contentHeight = lastRect.yMax + Scaled(28f);
            }

            GUI.DragWindow(new Rect(0, 0, _windowRect.width, Scaled(30)));
        }

        /// <summary>
        /// Draws one provider's section with balanced IMGUI layout guarantees.
        /// Returns true when the provider actually emitted briefing content.
        /// </summary>
        private bool TryDrawProviderSection(IModDataProvider provider)
        {
            // Snapshot IMGUI's BeginGroup/Vertical stack depth as a recovery
            // anchor. If the provider throws mid-draw we pop back down to this
            // depth instead of blindly calling EndVertical a second time, which
            // is the latent bug the old catch-block had.
            bool verticalOpened = false;
            try
            {
                GUILayout.BeginVertical();
                verticalOpened = true;

                GUILayout.Label($"{provider.ModIcon} {provider.ModName}", _sectionTitleStyle);
                bool drew = provider.DrawBriefingSection();

                GUILayout.EndVertical();
                verticalOpened = false;
                return drew;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Briefing] {provider.ModName} threw during DrawBriefingSection: {ex.Message}");
            }
            finally
            {
                if (verticalOpened)
                {
                    // Swallow any inner exception from the unmatched EndVertical:
                    // if the layout stack was already unwound by the outer throw,
                    // calling EndVertical here would just re-raise.
                    try { GUILayout.EndVertical(); }
                    catch (Exception innerEx)
                    {
                        Plugin.Log?.LogWarning($"[Briefing] Layout recovery: {innerEx.Message}");
                    }
                }
            }

            return false;
        }

        private void InitializeStyles()
        {
            _stylesInitialized = true;

            var bgColor = new Color(0.15f, 0.12f, 0.09f, 0.96f);
            var goldText = new Color(0.95f, 0.85f, 0.55f);
            var lightText = new Color(0.91f, 0.87f, 0.82f);
            var dimText = new Color(0.6f, 0.55f, 0.45f);

            _bgTexture = GUIStyleHelper.MakeSolidTexture(bgColor);

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(ScaledInt(16), ScaledInt(16), ScaledInt(12), ScaledInt(12)),
                border = new RectOffset(ScaledInt(2), ScaledInt(2), ScaledInt(2), ScaledInt(2))
            };
            _windowStyle.normal.background = _bgTexture;
            _windowStyle.onNormal.background = _bgTexture;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = ScaledFont(18),
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = goldText;

            _sectionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = ScaledFont(13)
            };
            _sectionTitleStyle.normal.textColor = goldText;

            _contentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = ScaledFont(12),
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _contentStyle.normal.textColor = lightText;

            _dismissButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = ScaledFont(13),
                fontStyle = FontStyle.Bold
            };
        }

        private void OnDestroy()
        {
            if (_bgTexture != null) Destroy(_bgTexture);
        }
    }
}
