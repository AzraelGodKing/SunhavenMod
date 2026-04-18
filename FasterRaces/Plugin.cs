using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.IO;
using UnityEngine;
using Wish;

namespace FasterRaces
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        internal static ConfigEntry<bool> EnableMod;
        internal static ConfigEntry<float> SpeedBonusPercent;
        private const float MinSpeedBonusPercent = 0f;
        private const float MaxSpeedBonusPercent = 300f;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            var configFile = CreateNamedConfig();
            SunhavenMods.Shared.ConfigFileHelper.ReplacePluginConfig(this, configFile, Log.LogWarning);
            EnableMod = configFile.Bind("General", "Enabled", true, "Enable Faster Races movement speed bonus. When enabled, Haven's Birthright will not apply its own movement speed bonuses to avoid double speed.");
            SpeedBonusPercent = configFile.Bind(
                "General",
                "SpeedBonusPercent",
                25f,
                new ConfigDescription(
                    "Percentage bonus to movement speed (e.g. 25 = +25%). Applied after other mods; Haven's Birthright skips its speed buff when this mod is loaded.",
                    new AcceptableValueRange<float>(MinSpeedBonusPercent, MaxSpeedBonusPercent)
                )
            );

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            var playerType = typeof(Player);
            var getStat = AccessTools.Method(playerType, "GetStat", new[] { typeof(Wish.StatType) });
            var postfix = AccessTools.Method(typeof(SpeedPatch), nameof(SpeedPatch.Postfix));
            if (getStat != null && postfix != null)
            {
                _harmony.Patch(getStat, postfix: new HarmonyMethod(postfix));
                Log.LogInfo("Patched Player.GetStat for movement speed");
            }
            else
                Log.LogWarning("Could not find Player.GetStat or SpeedPatch.Postfix");

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded. Speed bonus: {SpeedBonusPercent.Value}%");
        }

        /// <summary>
        /// Compatibility hook for other mods (e.g. Haven's Birthright):
        /// true only when this mod is currently enabled and has a positive bonus.
        /// </summary>
        public static bool IsSpeedBonusActive
        {
            get
            {
                if (EnableMod == null || !EnableMod.Value)
                    return false;

                float pct = SpeedBonusPercent != null ? SpeedBonusPercent.Value : 0f;
                return pct > 0f;
            }
        }

        private static ConfigFile CreateNamedConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "FasterRaces.cfg");
            string legacyPath = Path.Combine(Paths.ConfigPath, $"{PluginInfo.PLUGIN_GUID}.cfg");
            try
            {
                if (!File.Exists(configPath) && File.Exists(legacyPath))
                    File.Copy(legacyPath, configPath);
            }
            catch (System.Exception ex)
            {
                Log?.LogWarning($"[Config] Migration to FasterRaces.cfg failed: {ex.Message}");
            }
            return new ConfigFile(configPath, true);
        }

        public static class SpeedPatch
        {
            public static void Postfix(Wish.StatType stat, ref float __result)
            {
                if (stat != Wish.StatType.Movespeed)
                    return;
                if (EnableMod == null || !EnableMod.Value)
                    return;
                float pct = SpeedBonusPercent != null ? Mathf.Clamp(SpeedBonusPercent.Value, MinSpeedBonusPercent, MaxSpeedBonusPercent) : 0f;
                if (pct <= 0f)
                    return;
                __result *= (1f + pct / 100f);
            }
        }
    }

}
