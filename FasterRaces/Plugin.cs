using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        // Per-race overrides. -1 means "use global SpeedBonusPercent".
        internal static readonly Dictionary<string, ConfigEntry<float>> RaceSpeedOverrides
            = new Dictionary<string, ConfigEntry<float>>();

        private static readonly string[] KnownRaces =
        {
            "Human", "Elf", "Angel", "Demon",
            "FireElemental", "WaterElemental", "MagmaElemental", "Shade"
        };

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            var configFile = CreateNamedConfig();
            SunhavenMods.Shared.ConfigFileHelper.ReplacePluginConfig(this, configFile, Log.LogWarning);
            EnableMod = configFile.Bind("General", "Enabled", true,
                "Enable Faster Races movement speed bonus. When enabled, Haven's Birthright will not apply its own movement speed bonuses to avoid double speed.");

            SpeedBonusPercent = configFile.Bind(
                "General",
                "SpeedBonusPercent",
                25f,
                new ConfigDescription(
                    "Global percentage bonus to movement speed (e.g. 25 = +25%). Used for any race that does not have a per-race override set.",
                    new AcceptableValueRange<float>(MinSpeedBonusPercent, MaxSpeedBonusPercent)
                )
            );

            foreach (var race in KnownRaces)
            {
                var entry = configFile.Bind(
                    "PerRace",
                    $"{race}SpeedBonusPercent",
                    -1f,
                    new ConfigDescription(
                        $"Speed bonus % for {race}. Set to -1 to use the global SpeedBonusPercent.",
                        new AcceptableValueRange<float>(-1f, MaxSpeedBonusPercent)
                    )
                );
                RaceSpeedOverrides[race] = entry;
            }

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
            {
                if (getStat == null)
                    Log.LogWarning("[Reflect] Player.GetStat(StatType) not found - speed bonus inactive. Check game version.");
                if (postfix == null)
                    Log.LogWarning("[Reflect] SpeedPatch.Postfix not found - internal error.");
            }

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded. Global speed bonus: {SpeedBonusPercent.Value}%");
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

        /// <summary>
        /// Returns the effective speed bonus % for the given race name.
        /// Falls back to global if no per-race override is configured.
        /// </summary>
        internal static float GetEffectiveSpeedBonus(string raceName)
        {
            if (!string.IsNullOrEmpty(raceName)
                && RaceSpeedOverrides.TryGetValue(raceName, out var entry)
                && entry.Value >= 0f)
            {
                return entry.Value;
            }
            return SpeedBonusPercent?.Value ?? 0f;
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
            // Cache the SubRace property for fast per-frame access.
            private static PropertyInfo _subRaceProp;
            private static bool _subRaceChecked;

            public static void Postfix(Wish.StatType stat, ref float __result)
            {
                if (stat != Wish.StatType.Movespeed)
                    return;
                if (EnableMod == null || !EnableMod.Value)
                    return;

                string raceName = GetCurrentRaceName();
                float pct = Mathf.Clamp(GetEffectiveSpeedBonus(raceName), MinSpeedBonusPercent, MaxSpeedBonusPercent);
                if (pct <= 0f)
                    return;

                __result *= (1f + pct / 100f);
            }

            private static string GetCurrentRaceName()
            {
                try
                {
                    var player = Player.Instance;
                    if (player == null) return null;

                    if (!_subRaceChecked)
                    {
                        _subRaceChecked = true;
                        _subRaceProp = player.GetType().GetProperty("SubRace",
                            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    }

                    var subRace = _subRaceProp?.GetValue(player);
                    return subRace?.ToString();
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
