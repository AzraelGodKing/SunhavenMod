using System;
using System.Reflection;
using CropOptimizer.Data;
using HarmonyLib;
using UnityEngine;

namespace CropOptimizer.Patches
{
    internal static class CropGrowthPatch
    {
        private static CropForecast _forecast;

        public static void Apply(Harmony harmony, CropForecast forecast)
        {
            _forecast = forecast;
            Type cropType = AccessTools.TypeByName("Wish.Crop");
            if (cropType == null)
            {
                Plugin.Log?.LogWarning("[CropGrowthPatch] Wish.Crop not found");
                return;
            }

            MethodInfo updateGrowth = AccessTools.Method(cropType, "UpdateGrowth", Type.EmptyTypes)
                                      ?? AccessTools.Method(cropType, "GrowCrop", Type.EmptyTypes);
            if (updateGrowth == null)
            {
                Plugin.Log?.LogWarning("[CropGrowthPatch] Could not find crop growth method");
                return;
            }

            var postfix = AccessTools.Method(typeof(CropGrowthPatch), nameof(OnAfterCropGrowth));
            if (postfix == null)
            {
                Plugin.Log?.LogWarning("[CropGrowthPatch] Postfix method not found");
                return;
            }

            try
            {
                harmony.Patch(updateGrowth, postfix: new HarmonyMethod(postfix));
                Plugin.Log?.LogInfo($"[CropGrowthPatch] Patched {cropType.Name}.{updateGrowth.Name}()");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[CropGrowthPatch] Harmony patch failed: {ex.Message}");
            }
        }

        private static void OnAfterCropGrowth(object __instance)
        {
            if (__instance == null || _forecast == null)
                return;

            try
            {
                var unityObj = __instance as UnityEngine.Object;
                if (unityObj == null)
                    return;

                int id = unityObj.GetInstanceID();
                // Conservative defaults until deeper crop stat extraction is mapped.
                _forecast.UpdateCropState(id, nextHarvestEtaHours: 24f, qualityMultiplier: 1f, projectedSellGold: 0);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[CropGrowthPatch] Update failed: {ex.Message}");
            }
        }
    }
}
