namespace CropOptimizer.Data
{
    // Lightweight data surface consumed by Haven's Almanac integration.
    public static class CropOptimizerDataProvider
    {
        public static string GetSummary()
        {
            return Plugin.GetHudSummary();
        }
    }
}
