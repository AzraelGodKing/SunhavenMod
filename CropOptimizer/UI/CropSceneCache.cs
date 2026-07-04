using System.Collections.Generic;
using CropOptimizer.Data;
using CropOptimizer.Patches;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CropOptimizer.UI
{
    /// <summary>Shared crop scene scan with registry / forecast reconciliation.</summary>
    internal static class CropSceneCache
    {
        private static UnityEngine.Object[] _cachedCrops = System.Array.Empty<UnityEngine.Object>();
        private static float _nextRefreshTime;
        private static int _cachedSceneHandle = -1;
        private static System.Type _cropType;

        /// <summary>When the active scene has no crops, avoid hammering <c>FindObjectsOfType</c> off-farm.</summary>
        private const float EmptySceneCacheRefreshSeconds = 4f;

        public static void Invalidate()
        {
            _cachedCrops = System.Array.Empty<UnityEngine.Object>();
            _nextRefreshTime = 0f;
            _cachedSceneHandle = -1;
        }

        public static bool HasActiveSceneCrops(float refreshIntervalSeconds)
        {
            UnityEngine.Object[] crops = GetCrops(refreshIntervalSeconds);
            return crops != null && crops.Length > 0;
        }

        public static UnityEngine.Object[] GetCrops(float refreshIntervalSeconds)
        {
            float now = Time.unscaledTime;
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.handle != _cachedSceneHandle)
            {
                _cachedSceneHandle = activeScene.handle;
                _cachedCrops = System.Array.Empty<UnityEngine.Object>();
                _nextRefreshTime = 0f;
            }

            if (now < _nextRefreshTime)
                return _cachedCrops;

            if (_cropType == null)
                _cropType = AccessTools.TypeByName("Wish.Crop");
            if (_cropType == null)
            {
                _cachedCrops = System.Array.Empty<UnityEngine.Object>();
                _nextRefreshTime = now + EmptySceneCacheRefreshSeconds;
                return _cachedCrops;
            }

            var discovered = Object.FindObjectsOfType(_cropType);
            var liveIds = new HashSet<int>();
            var presentCrops = new List<Object>(discovered.Length);

            for (int i = 0; i < discovered.Length; i++)
            {
                if (discovered[i] is not Component crop || !CropPresence.IsPresent(crop))
                    continue;
                if (crop.gameObject.scene != activeScene)
                    continue;

                CropInstanceRegistry.Register(crop);
                liveIds.Add(crop.GetInstanceID());
                presentCrops.Add(crop);
            }

            PruneStale(liveIds, discovered.Length);
            ReconcileForecast(presentCrops);
            _cachedCrops = presentCrops.ToArray();
            _nextRefreshTime = now + (presentCrops.Count == 0
                ? EmptySceneCacheRefreshSeconds
                : Mathf.Max(0.1f, refreshIntervalSeconds));
            return _cachedCrops;
        }

        private static void ReconcileForecast(List<Object> presentCrops)
        {
            CropForecast forecast = Plugin.Instance?._forecast;
            if (forecast == null || presentCrops == null || presentCrops.Count == 0)
                return;

            for (int i = 0; i < presentCrops.Count; i++)
            {
                if (presentCrops[i] is not Component crop || !CropPresence.IsTrackable(crop))
                    continue;

                int id = crop.GetInstanceID();
                if (!forecast.TryGetState(id, out CropForecast.CropState state) || state.ItemId <= 0)
                    CropGrowthPatch.SyncCropToForecast(crop);
            }
        }

        private static void PruneStale(HashSet<int> liveIds, int discoveredCount)
        {
            // Empty scan with no Wish.Crop instances — drop stale rows (left the farm / no crops).
            // When instances exist but fail IsPresent, keep forecast until a successful pass.
            if (liveIds == null || liveIds.Count == 0)
            {
                if (discoveredCount == 0)
                {
                    CropInstanceRegistry.PruneExcept(liveIds);
                    Plugin.Instance?._forecast?.PruneExcept(liveIds);
                }
                return;
            }

            CropInstanceRegistry.PruneExcept(liveIds);
            Plugin.Instance?._forecast?.PruneExcept(liveIds);
        }
    }
}
