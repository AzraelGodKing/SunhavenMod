using System;
using UnityEngine;

namespace HavenDevTools.UI
{
    /// <summary>Smoothed FPS / memory samples for overlay, HUD counter, and Perf tab.</summary>
    internal static class PerformanceSampler
    {
        private const float Smoothing = 0.12f;

        private static float _smoothedFps;
        private static float _minFps = float.MaxValue;
        private static float _maxFps;
        private static float _resetTimer;
        private const float MinMaxResetSeconds = 3f;

        public static float SmoothedFps => _smoothedFps;
        public static float MinFps => _minFps == float.MaxValue ? _smoothedFps : _minFps;
        public static float MaxFps => _maxFps;

        public static float MemoryMb => GC.GetTotalMemory(false) / (1024f * 1024f);

        public static void Tick()
        {
            float instant = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            if (_smoothedFps <= 0f)
                _smoothedFps = instant;
            else
                _smoothedFps = Mathf.Lerp(_smoothedFps, instant, Smoothing);

            _minFps = Mathf.Min(_minFps, instant);
            _maxFps = Mathf.Max(_maxFps, instant);

            _resetTimer += Time.unscaledDeltaTime;
            if (_resetTimer >= MinMaxResetSeconds)
            {
                _resetTimer = 0f;
                _minFps = instant;
                _maxFps = instant;
            }
        }

        public static Color FpsColor(float fps)
        {
            if (fps >= 55f)
                return new Color(0.45f, 0.95f, 0.55f);
            if (fps >= 40f)
                return new Color(0.95f, 0.85f, 0.35f);
            return new Color(0.95f, 0.45f, 0.45f);
        }
    }
}
