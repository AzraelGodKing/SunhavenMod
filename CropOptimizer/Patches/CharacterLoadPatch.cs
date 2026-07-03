using System;
using System.Linq;
using System.Reflection;
using CropOptimizer.Data;
using CropOptimizer.UI;
using HarmonyLib;
using Wish;

namespace CropOptimizer.Patches
{
    internal static class CharacterLoadPatch
    {
        private static CropForecast _forecast;

        public static void Apply(Harmony harmony, CropForecast forecast)
        {
            if (harmony == null)
                return;

            _forecast = forecast;

            try
            {
                var loadMethods = AccessTools.GetDeclaredMethods(typeof(GameSave))
                    .Where(m => string.Equals(m.Name, "LoadCharacter", StringComparison.Ordinal))
                    .ToList();

                if (loadMethods.Count == 0)
                {
                    Plugin.Log?.LogWarning("[CharacterLoadPatch] No GameSave.LoadCharacter overloads found.");
                    return;
                }

                var postfix = AccessTools.Method(typeof(CharacterLoadPatch), nameof(OnAfterLoadCharacter));
                if (postfix == null)
                {
                    Plugin.Log?.LogWarning("[CharacterLoadPatch] Postfix method not found.");
                    return;
                }

                foreach (var method in loadMethods)
                    harmony.Patch(method, postfix: new HarmonyMethod(postfix));

                Plugin.Log?.LogInfo($"[CharacterLoadPatch] Patched {loadMethods.Count} GameSave.LoadCharacter overload(s).");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CharacterLoadPatch] Failed to patch GameSave.LoadCharacter: {ex.Message}");
            }
        }

        private static void OnAfterLoadCharacter()
        {
            try
            {
                CropInstanceRegistry.Clear();
                CropSceneCache.Invalidate();
                CropHoverQuery.InvalidateHoverAssist();

                if (_forecast != null)
                {
                    _forecast.Clear();
                    return;
                }

                Plugin.Instance?._forecast?.Clear();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[CharacterLoadPatch] Forecast clear failed: {ex.Message}");
            }
        }
    }
}
