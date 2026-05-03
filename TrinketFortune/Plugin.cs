using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using TrinketFortune.Patches;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrinketFortune
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        private Harmony _harmony;
        private bool _applicationQuitting;

        private void Awake()
        {
            Log = Logger;
            var namedConfig = CreateNamedConfig();
            SunhavenMods.Shared.ConfigFileHelper.ReplacePluginConfig(this, namedConfig, Log.LogWarning);
            TrinketFortune.Config.Bind(namedConfig);

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            FishingTrinketPatches.ApplyPatches(_harmony);

            bool hasHavenDevTools = Chainloader.PluginInfos != null &&
                                    Chainloader.PluginInfos.ContainsKey("com.azraelgodking.havendevtools");
            if (hasHavenDevTools)
                Log.LogInfo("HavenDevTools detected. Trinket Fortune runs in standalone-safe mode (no hard API dependency).");

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded. Fishing loot bias active when S.M.U.T. is installed.");
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        private void OnDestroy()
        {
            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            string sceneLower = sceneName.ToLowerInvariant();
            bool expectedTeardown = _applicationQuitting || !Application.isPlaying || sceneLower.Contains("menu") || sceneLower.Contains("title");
            if (expectedTeardown)
                Log?.LogInfo($"[Lifecycle] Plugin OnDestroy during expected teardown (scene: {sceneName})");
            else
                Log?.LogWarning($"[Lifecycle] Plugin OnDestroy outside expected teardown (scene: {sceneName})");

            _harmony?.UnpatchSelf();
        }

        private static BepInEx.Configuration.ConfigFile CreateNamedConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "TrinketFortune.cfg");
            string legacyPath = Path.Combine(Paths.ConfigPath, $"{PluginInfo.PLUGIN_GUID}.cfg");
            try
            {
                if (!File.Exists(configPath) && File.Exists(legacyPath))
                    File.Copy(legacyPath, configPath);
            }
            catch (System.Exception ex)
            {
                Log?.LogWarning($"[Config] Migration to TrinketFortune.cfg failed: {ex.Message}");
            }
            return new BepInEx.Configuration.ConfigFile(configPath, true);
        }

        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "com.azraelgodking.trinketfortune";
            public const string PLUGIN_NAME = "Trinket Fortune";
            public const string PLUGIN_VERSION = "1.2.2";
        }
    }
}
