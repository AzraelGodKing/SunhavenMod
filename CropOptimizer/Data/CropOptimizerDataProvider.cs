using System.Collections.Generic;

namespace CropOptimizer.Data
{
    // Lightweight data surface consumed by Haven's Almanac integration.
    public static class CropOptimizerDataProvider
    {
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

        public static string GetSummary()
        {
            return Plugin.GetHudSummary();
        }

        public static List<CropTypeSummary> GetTopCrops(int count = 5)
        {
            var topCrops = Plugin.GetTopCrops(count);
            var result = new List<CropTypeSummary>(topCrops.Count);
            foreach (var crop in topCrops)
            {
                int displayValue = crop.Total.Gold;
                if (displayValue <= 0 && crop.Total.Orbs > 0)
                    displayValue = crop.Total.Orbs;
                else if (displayValue <= 0 && crop.Total.Tickets > 0)
                    displayValue = crop.Total.Tickets;
                result.Add(new CropTypeSummary(crop.ItemId, displayValue, crop.CropCount));
            }
            return result;
        }

        public static bool TryGetCropDisplayName(int itemId, out string name)
        {
            return Patches.CropGrowthPatch.TryGetItemDisplayName(itemId, out name);
        }
    }
}
