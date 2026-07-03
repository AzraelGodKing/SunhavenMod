using System;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using Wish;

namespace HavenDevTools.Services
{
    /// <summary>
    /// Detects the community Ultra Polygamy mod (<c>vurawnica.sunhaven.polygamy</c>) and whether its
    /// <see cref="NPCAI.MarryPlayer"/> Harmony prefix is active (required for multi-marriage).
    /// </summary>
    public static class UltraPolygamyHelper
    {
        public const string PluginGuid = "vurawnica.sunhaven.polygamy";

        public static bool IsPluginLoaded =>
            Chainloader.PluginInfos.ContainsKey(PluginGuid);

        public static bool IsMarryPatchActive()
        {
            try
            {
                var method = AccessTools.Method(typeof(NPCAI), "MarryPlayer");
                if (method == null)
                    return false;

                var patches = Harmony.GetPatchInfo(method);
                if (patches?.Prefixes == null || patches.Prefixes.Count == 0)
                    return false;

                return patches.Prefixes.Any(p =>
                    p.owner.IndexOf("Polygamy", StringComparison.OrdinalIgnoreCase) >= 0
                    || p.owner.IndexOf("UltraPolygamy", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Plugin loaded and MarryPlayer poly patch is present.</summary>
        public static bool IsAvailable => IsPluginLoaded && IsMarryPatchActive();

        public static string StatusLocalizationKey
        {
            get
            {
                if (!IsPluginLoaded)
                    return "devtools.marriable.polyStatus.notLoaded";
                if (!IsMarryPatchActive())
                    return "devtools.marriable.polyStatus.patchMissing";
                return "devtools.marriable.polyStatus.ready";
            }
        }
    }
}
