using System;
using System.Collections.Generic;

namespace CropOptimizer.Data
{
    internal sealed class CropForecast
    {
        private readonly Dictionary<int, CropState> _cropStateByInstanceId = new Dictionary<int, CropState>();
        /// <summary>Running sum of <see cref="CropState.ProjectedSellGold"/> so the HUD never walks the full dict each frame.</summary>
        private int _runningProjectedSellTotal;

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
            int add = Math.Max(0, projectedSellGold);
            if (_cropStateByInstanceId.TryGetValue(cropInstanceId, out CropState prev))
                _runningProjectedSellTotal -= Math.Max(0, prev.ProjectedSellGold);
            _cropStateByInstanceId[cropInstanceId] = new CropState(nextHarvestEtaHours, qualityMultiplier, projectedSellGold);
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

        public void Clear()
        {
            _cropStateByInstanceId.Clear();
            _runningProjectedSellTotal = 0;
        }
    }
}
