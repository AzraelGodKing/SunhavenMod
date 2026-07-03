using System;
using System.Collections.Generic;
using System.Linq;

namespace CropOptimizer.Data
{
    internal sealed class CropForecast
    {
        private readonly Dictionary<int, CropState> _cropStateByInstanceId = new Dictionary<int, CropState>();
        /// <summary>Running sum of <see cref="CropState.ProjectedSellGold"/> so the HUD never walks the full dict each frame.</summary>
        private int _runningProjectedSellTotal;

        internal readonly struct CropState
        {
            public CropState(float nextHarvestEtaHours, float qualityMultiplier, int projectedSellGold, int itemId)
            {
                NextHarvestEtaHours = nextHarvestEtaHours;
                QualityMultiplier = qualityMultiplier;
                ProjectedSellGold = projectedSellGold;
                ItemId = itemId;
            }

            public float NextHarvestEtaHours { get; }
            public float QualityMultiplier { get; }
            public int ProjectedSellGold { get; }
            public int ItemId { get; }
        }

        public readonly struct CropTypeSummary
        {
            public CropTypeSummary(int itemId, int totalGold, int cropCount)
            {
                ItemId = itemId;
                TotalGold = totalGold;
                CropCount = cropCount;
            }

            public int ItemId { get; }
            public int TotalGold { get; }
            public int CropCount { get; }
        }

        public void UpdateCropState(int cropInstanceId, float nextHarvestEtaHours, float qualityMultiplier, int projectedSellGold, int itemId = 0)
        {
            int add = Math.Max(0, projectedSellGold);
            if (_cropStateByInstanceId.TryGetValue(cropInstanceId, out CropState prev))
                _runningProjectedSellTotal -= Math.Max(0, prev.ProjectedSellGold);
            _cropStateByInstanceId[cropInstanceId] = new CropState(nextHarvestEtaHours, qualityMultiplier, projectedSellGold, itemId);
            _runningProjectedSellTotal += add;
        }

        public IReadOnlyDictionary<int, CropState> Snapshot()
        {
            return _cropStateByInstanceId;
        }

        public int GetProjectedSellTotal() => _runningProjectedSellTotal;

        public bool TryGetState(int cropInstanceId, out CropState state)
        {
            return _cropStateByInstanceId.TryGetValue(cropInstanceId, out state);
        }

        public bool RemoveCropState(int cropInstanceId)
        {
            if (!_cropStateByInstanceId.TryGetValue(cropInstanceId, out CropState previous))
                return false;

            _runningProjectedSellTotal -= Math.Max(0, previous.ProjectedSellGold);
            return _cropStateByInstanceId.Remove(cropInstanceId);
        }

        /// <summary>
        /// Returns the top <paramref name="count"/> crop types ranked by total projected gold,
        /// aggregated across all tracked instances. Types with itemId == 0 are skipped.
        /// </summary>
        public List<CropTypeSummary> GetTopCropsByValue(int count = 5)
        {
            var byType = new Dictionary<int, (int totalGold, int cropCount)>();
            foreach (var kvp in _cropStateByInstanceId)
            {
                var state = kvp.Value;
                if (state.ItemId <= 0) continue;
                if (!byType.TryGetValue(state.ItemId, out var acc))
                    acc = (0, 0);
                byType[state.ItemId] = (acc.totalGold + Math.Max(0, state.ProjectedSellGold), acc.cropCount + 1);
            }
            return byType
                .Select(kvp => new CropTypeSummary(kvp.Key, kvp.Value.totalGold, kvp.Value.cropCount))
                .OrderByDescending(s => s.TotalGold)
                .Take(count)
                .ToList();
        }

        public void Clear()
        {
            _cropStateByInstanceId.Clear();
            _runningProjectedSellTotal = 0;
        }

        public void PruneExcept(HashSet<int> liveIds)
        {
            if (liveIds == null || liveIds.Count == 0)
            {
                Clear();
                return;
            }

            var stale = new List<int>();
            foreach (int id in _cropStateByInstanceId.Keys)
            {
                if (!liveIds.Contains(id))
                    stale.Add(id);
            }

            for (int i = 0; i < stale.Count; i++)
                RemoveCropState(stale[i]);
        }
    }
}
