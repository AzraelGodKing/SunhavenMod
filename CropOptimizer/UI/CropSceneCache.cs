using System.Collections.Generic;
using CropOptimizer.Data;
using CropOptimizer.Patches;
using HarmonyLib;
using UnityEngine;

namespace CropOptimizer.UI
{
    /// <summary>Shared crop scene scan with registry / forecast reconciliation.</summary>
    internal static class CropSceneCache
    {
        private static UnityEngine.Object[] _cachedCrops = System.Array.Empty<UnityEngine.Object>();
        private static float _nextRefreshTime;

        public static void Invalidate()
        {
            _cachedCrops = System.Array.Empty<UnityEngine.Object>();
            _nextRefreshTime = 0f;
        }

        public static UnityEngine.Object[] GetCrops(float refreshIntervalSeconds)
        {
            float now = Time.unscaledTime;
            if (_cachedCrops.Length > 0 && now < _nextRefreshTime)
                return _cachedCrops;

            _nextRefreshTime = now + Mathf.Max(0.1f, refreshIntervalSeconds);

            var cropType = AccessTools.TypeByName("Wish.Crop");
            if (cropType == null)
            {
                _cachedCrops = System.Array.Empty<UnityEngine.Object>();
                PruneStale(new HashSet<int>());
                return _cachedCrops;
            }

            var discovered = UnityEngine.Object.FindObjectsOfType(cropType);
            var liveIds = new HashSet<int>();
            var presentCrops = new List<UnityEngine.Object>(discovered.Length);

            for (int i = 0; i < discovered.Length; i++)
            {
                if (discovered[i] is not Component crop || !CropPresence.IsPresent(crop))
                    continue;

                bool forecastable = CropPresence.IsTrackable(crop);

                CropInstanceRegistry.Register(crop);
                liveIds.Add(crop.GetInstanceID());
                presentCrops.Add(crop);

                if (forecastable
                    && Plugin.Instance?._forecast != null
                    && !Plugin.Instance._forecast.TryGetState(crop.GetInstanceID(), out _))
                {
                    CropGrowthPatch.TryGetTooltipHarvestItemId(crop, out int itemId);
                    float eta = 24f;
                    CropGrowthPatch.TryGetTooltipEtaHours(crop, out eta, out _);
                    float quality = 1f;
                    CropGrowthPatch.TryGetTooltipQualityInfo(crop, out _, out quality);
                    int gold = 0;
                    CropGrowthPatch.TryGetTooltipProjectedGold(crop, out gold, out _);
                    Plugin.Instance._forecast.UpdateCropState(crop.GetInstanceID(), eta, quality, gold, itemId);
                }
            }

            PruneStale(liveIds);
            _cachedCrops = presentCrops.ToArray();
            return _cachedCrops;
        }

        private static void PruneStale(HashSet<int> liveIds)
        {
            CropInstanceRegistry.PruneExcept(liveIds);
            Plugin.Instance?._forecast?.PruneExcept(liveIds);
        }
    }
}
