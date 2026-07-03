using System.Collections.Generic;
using CropOptimizer.Data;
using CropOptimizer.Patches;
using UnityEngine;

namespace CropOptimizer.UI
{
    internal enum CropHighlightKind
    {
        NeedsWater,
        NeedsFertilizer
    }

    internal readonly struct CropHighlightTarget
    {
        public readonly Vector3 Center;
        public readonly Vector2Int Tile;
        public readonly CropHighlightKind Kind;

        public CropHighlightTarget(Vector3 center, Vector2Int tile, CropHighlightKind kind)
        {
            Center = center;
            Tile = tile;
            Kind = kind;
        }
    }

    /// <summary>Finds crop tiles that still need water or fertilizer.</summary>
    internal static class CropFieldHighlightScanner
    {
        private const int MaxTargets = 600;
        private const float CacheRefreshSeconds = 0.85f;
        private static readonly List<CropHighlightTarget> _scratch = new List<CropHighlightTarget>(128);

        public static IReadOnlyList<CropHighlightTarget> Scan(bool includeDry, bool includeUnfertilized)
        {
            _scratch.Clear();
            if (!includeDry && !includeUnfertilized)
                return _scratch;

            UnityEngine.Object[] crops = CropSceneCache.GetCrops(CacheRefreshSeconds);
            if (crops == null || crops.Length == 0)
                return _scratch;

            foreach (UnityEngine.Object o in crops)
            {
                if (_scratch.Count >= MaxTargets)
                    break;
                if (o is not Component crop || !CropPresence.IsPresent(crop))
                    continue;

                object inst = crop;
                if (!GameFarmCoords.TryGetCropFarmTile(crop, out Vector2Int farmTile))
                    continue;

                Vector3 center = GameFarmCoords.GetSelectionWorldPosition(farmTile);

                if (includeDry && !CropTileReflection.IsCropTileWatered(crop))
                {
                    _scratch.Add(new CropHighlightTarget(center, farmTile, CropHighlightKind.NeedsWater));
                    continue;
                }

                if (includeUnfertilized
                    && (!CropGrowthPatch.TryGetTooltipFullyGrown(inst, out bool fullyGrown) || !fullyGrown)
                    && CropGrowthPatch.TryGetTooltipFertilized(inst, out bool fertilized)
                    && !fertilized)
                    _scratch.Add(new CropHighlightTarget(center, farmTile, CropHighlightKind.NeedsFertilizer));
            }

            return _scratch;
        }

        public static void InvalidateCache()
        {
            CropSceneCache.Invalidate();
        }
    }
}
