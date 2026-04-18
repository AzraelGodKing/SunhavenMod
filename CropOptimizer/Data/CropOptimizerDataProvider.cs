using System.Collections.Generic;

namespace CropOptimizer.Data
{
    // Lightweight data surface consumed by Haven's Almanac integration.
    public static class CropOptimizerDataProvider
    {
        public static string GetSummary()
        {
            return Plugin.GetHudSummary();
        }

        public static List<CropForecast.CropTypeSummary> GetTopCrops(int count = 5)
        {
            return Plugin.GetTopCrops(count);
        }

        public static bool TryGetCropDisplayName(int itemId, out string name)
        {
            return Patches.CropGrowthPatch.TryGetItemDisplayName(itemId, out name);
        }
    }
}
