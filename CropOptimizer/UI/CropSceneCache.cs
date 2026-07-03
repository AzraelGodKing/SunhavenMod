using System.Collections.Generic;
using CropOptimizer.Data;
using HarmonyLib;
using UnityEngine;

namespace CropOptimizer.UI
{
    /// <summary>Shared crop scene scan with registry / forecast reconciliation.</summary>
    internal static class CropSceneCache
    {
        private static UnityEngine.Object[] _cachedCrops = System.Array.Empty<UnityEngine.Object>();
        private static float _nextRefreshTime;
        private static System.Type _cropType;

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

            if (_cropType == null)
                _cropType = AccessTools.TypeByName("Wish.Crop");
            if (_cropType == null)
            {
                _cachedCrops = System.Array.Empty<UnityEngine.Object>();
                return _cachedCrops;
            }

            var discovered = Object.FindObjectsOfType(_cropType);
            var liveIds = new HashSet<int>();
            var presentCrops = new List<Object>(discovered.Length);

            for (int i = 0; i < discovered.Length; i++)
            {
                if (discovered[i] is not Component crop || !CropPresence.IsPresent(crop))
                    continue;

                CropInstanceRegistry.Register(crop);
                liveIds.Add(crop.GetInstanceID());
                presentCrops.Add(crop);
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
