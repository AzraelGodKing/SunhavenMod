using System;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using CropOptimizer.Config;
using CropOptimizer.Data;
using CropOptimizer.Integration;
using CropOptimizer.Patches;
using CropOptimizer.UI;
using HarmonyLib;
using UnityEngine;

namespace CropOptimizer
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.azraelgodking.sunhaventodo", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.squirrelsbirthdayreminder", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.thevault", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        public static Plugin Instance { get; private set; }

        private Harmony _harmony;
        private CropOptimizerConfig _config;
        private CropForecast _forecast;
        private CropHUD _hud;
        private TodoIntegration _todoIntegration;
        private BirthdayIntegration _birthdayIntegration;
        private VaultIntegration _vaultIntegration;
        private bool _hudVisible = true;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            _config = new CropOptimizerConfig(CreateNamedConfig());
            if (!_config.Enabled.Value)
            {
                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} disabled in config.");
                return;
            }

            _forecast = new CropForecast();
            _todoIntegration = new TodoIntegration();
            _birthdayIntegration = new BirthdayIntegration();
            _vaultIntegration = new VaultIntegration();

            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            CropGrowthPatch.Apply(_harmony, _forecast);

            var hudObject = new GameObject("CropOptimizer_HUD");
            DontDestroyOnLoad(hudObject);
            _hud = hudObject.AddComponent<CropHUD>();
            _hud.Initialize(_forecast);
            _hud.SetScale(_config.HudScale.Value);
            _hud.SetVisible(_config.HudEnabled.Value);
            _hudVisible = _config.HudEnabled.Value;

            if (_vaultIntegration.IsAvailable)
                _vaultIntegration.TryRegisterProjectedValueCurrency();

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded");
        }

        private void Update()
        {
            if (_config == null || _hud == null)
                return;

            if (_config.ToggleHudKey.Value != KeyCode.None && Input.GetKeyDown(_config.ToggleHudKey.Value))
            {
                _hudVisible = !_hudVisible;
                _hud.SetVisible(_hudVisible);
            }
        }

        public static string GetHudSummary()
        {
            if (Instance?._forecast == null)
                return "Not ready";
            return $"Crops: {Instance._forecast.Snapshot().Count}, Value: {Instance._forecast.GetProjectedSellTotal()}g";
        }

        private static ConfigFile CreateNamedConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "CropOptimizer.cfg");
            string legacyPath = Path.Combine(Paths.ConfigPath, $"{PluginInfo.PLUGIN_GUID}.cfg");
            try
            {
                if (!File.Exists(configPath) && File.Exists(legacyPath))
                    File.Copy(legacyPath, configPath);
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"[Config] Migration to CropOptimizer.cfg failed: {ex.Message}");
            }
            return new ConfigFile(configPath, true);
        }
    }
}
