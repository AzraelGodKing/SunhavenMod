using System;
using System.Collections.Generic;

namespace CropOptimizer.Data
{
    internal sealed class CropForecast
    {
        private readonly Dictionary<int, CropState> _cropStateByInstanceId = new Dictionary<int, CropState>();

        internal readonly struct CropState
        {
            public CropState(float nextHarvestEtaHours, float qualityMultiplier, int projectedSellGold)
            {
                NextHarvestEtaHours = nextHarvestEtaHours;
                QualityMultiplier = qualityMultiplier;
                ProjectedSellGold = projectedSellGold;
            }

            public float NextHarvestEtaHours { get; }
            public float QualityMultiplier { get; }
            public int ProjectedSellGold { get; }
        }

        public void UpdateCropState(int cropInstanceId, float nextHarvestEtaHours, float qualityMultiplier, int projectedSellGold)
        {
            _cropStateByInstanceId[cropInstanceId] = new CropState(nextHarvestEtaHours, qualityMultiplier, projectedSellGold);
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

        public void Clear()
        {
            _cropStateByInstanceId.Clear();
        }
    }
}
