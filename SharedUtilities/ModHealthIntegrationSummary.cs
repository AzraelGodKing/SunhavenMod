using System.Collections.Generic;
using BepInEx.Bootstrap;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Suite plugin GUIDs for soft-dependency health summaries.
    /// </summary>
    public static class SuitePluginGuids
    {
        public const string DevTools = "com.azraelgodking.havendevtools";
        public const string SenpaisChest = "com.azraelgodking.senpaischest";
        public const string BirthdayReminder = "com.azraelgodking.squirrelsbirthdayreminder";
        public const string HavensBirthright = "com.azraelgodking.havensbirthright";
        public const string Smut = "com.azraelgodking.sunhavenmuseumutilitytracker";
        public const string SunhavenTodo = "com.azraelgodking.sunhaventodo";
        public const string TheVault = "com.azraelgodking.thevault";
        public const string HavensAlmanac = "com.azraelgodking.havensalmanac";
        public const string FasterRaces = "com.azraelgodking.fasterraces";
        public const string TrinketFortune = "com.azraelgodking.trinketfortune";
        public const string CropOptimizer = "com.azraelgodking.cropoptimizer";
        public const string HavensRespec = "com.azraelgodking.havensrespec";
        public const string GiftingAssistant = "com.azraelgodking.giftingassistant";
    }

    /// <summary>
    /// Builds a comma-separated list of detected soft integrations for Mod Health reports.
    /// </summary>
    public static class ModHealthIntegrationSummary
    {
        public static string Build(params (string label, string pluginGuid)[] integrations)
        {
            if (integrations == null || integrations.Length == 0)
                return "standalone";

            var infos = Chainloader.PluginInfos;
            if (infos == null)
                return "standalone";

            var present = new List<string>(integrations.Length);
            foreach (var (label, guid) in integrations)
            {
                if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(guid) && infos.ContainsKey(guid))
                    present.Add(label);
            }

            return present.Count == 0 ? "standalone" : string.Join(", ", present);
        }
    }
}
