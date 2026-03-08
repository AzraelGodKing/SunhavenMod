using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using HavenDevTools.API;
using TrinketFortune.DevTools;
using TrinketFortune.Patches;

namespace TrinketFortune
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            TrinketFortune.Config.Bind(Config);

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            FishingTrinketPatches.ApplyPatches(_harmony);

            if (Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey("com.azraelgodking.havendevtools"))
            {
                DevToolsRegistry.Register(new TrinketFortunePanel());
                Log.LogInfo("Registered Trinket Fortune panel with HavenDevTools");
            }

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded. Fishing loot bias active when S.M.U.T. is installed.");
        }

        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "com.azraelgodking.trinketfortune";
            public const string PLUGIN_NAME = "Trinket Fortune";
            public const string PLUGIN_VERSION = "1.0.0";
        }
    }
}
