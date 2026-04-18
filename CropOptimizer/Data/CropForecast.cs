using System;
using System.Collections.Generic;
using System.Linq;

namespace CropOptimizer.Data
{
    internal sealed class CropForecast
    {
        private readonly Dictionary<int, CropState> _cropStateByInstanceId = new Dictionary<int, CropState>();

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
            _cropStateByInstanceId[cropInstanceId] = new CropState(nextHarvestEtaHours, qualityMultiplier, projectedSellGold, itemId);
        }

        public IReadOnlyDictionary<int, CropState> Snapshot()
        {
            return _cropStateByInstanceId;
        }

        public int GetProjectedSellTotal()
        {
            int total = 0;
            foreach (var kvp in _cropStateByInstanceId)
                total += Math.Max(0, kvp.Value.ProjectedSellGold);
            return total;
        }

        public bool TryGetState(int cropInstanceId, out CropState state)
        {
            return _cropStateByInstanceId.TryGetValue(cropInstanceId, out state);
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
        }
    }
}
