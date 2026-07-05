using HavenDevTools.Config;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavenDevTools.UI
{
    /// <summary>Minimal always-available FPS readout in a screen corner (independent of the F6 overlay).</summary>
    public class FpsCounterOverlay : MonoBehaviour
    {
        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInitialized;

        private void Update()
        {
            PerformanceSampler.Tick();
        }

        private void OnGUI()
        {
            if (!(ModConfig.ShowFpsCounter?.Value ?? false))
                return;

            InitializeStyles();

            Rect rect = GetCounterRect(ModConfig.GetFpsCounterPosition());
            GUI.Box(rect, GUIContent.none, _boxStyle);

            _labelStyle.normal.textColor = PerformanceSampler.FpsColor(PerformanceSampler.SmoothedFps);
            GUI.Label(
                new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, rect.height - 6f),
                ModLocalization.T("devtools.perf.fps", PerformanceSampler.SmoothedFps),
                _labelStyle);
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized)
                return;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.1f, 0.72f));
            bg.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = bg }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _stylesInitialized = true;
        }

        private static Rect GetCounterRect(OverlayPositionType position)
        {
            const float width = 92f;
            const float height = 24f;
            const float margin = 8f;

            return position switch
            {
                OverlayPositionType.TopLeft => new Rect(margin, margin, width, height),
                OverlayPositionType.TopRight => new Rect(Screen.width - width - margin, margin, width, height),
                OverlayPositionType.BottomLeft => new Rect(margin, Screen.height - height - margin, width, height),
                OverlayPositionType.BottomRight => new Rect(
                    Screen.width - width - margin,
                    Screen.height - height - margin,
                    width,
                    height),
                _ => new Rect(margin, margin, width, height)
            };
        }
    }
}
