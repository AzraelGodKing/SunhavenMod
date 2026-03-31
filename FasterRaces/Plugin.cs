using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Wish;

namespace FasterRaces
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        internal static ConfigEntry<bool> EnableMod;
        internal static ConfigEntry<float> SpeedBonusPercent;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            EnableMod = Config.Bind("General", "Enabled", true, "Enable Faster Races movement speed bonus. When enabled, Haven's Birthright will not apply its own movement speed bonuses to avoid double speed.");
            SpeedBonusPercent = Config.Bind("General", "SpeedBonusPercent", 25f, "Percentage bonus to movement speed (e.g. 25 = +25%). Applied after other mods; Haven's Birthright skips its speed buff when this mod is loaded.");

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

        public static class SpeedPatch
        {
            public static void Postfix(Wish.StatType stat, ref float __result)
            {
                if (stat != Wish.StatType.Movespeed)
                    return;
                if (EnableMod == null || !EnableMod.Value)
                    return;
                float pct = SpeedBonusPercent != null ? SpeedBonusPercent.Value : 0f;
                if (pct <= 0f)
                    return;
                __result *= (1f + pct / 100f);
            }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.fasterraces";
        public const string PLUGIN_NAME = "Faster Races";
        public const string PLUGIN_VERSION = "1.1.2";
    }
}
