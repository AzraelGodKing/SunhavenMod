using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HavensRespec.Config;
using HavensRespec.Patches;
using HavensRespec.Services;
using HavensRespec.UI;
using SunhavenMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wish;

namespace HavensRespec
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }
        public static Plugin Instance { get; private set; }
        public static bool IsDebugLoggingEnabled => Instance?._config?.DebugLogging?.Value == true;

        private Harmony _harmony;
        private RespecConfig _config;
        private SkillResetService _resetService;
        private CostService _costService;
        private RespecController _controller;
        private bool _applicationQuitting;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            try
            {
                var namedConfig = ConfigFileHelper.CreateNamedConfig(
                    PluginInfo.PLUGIN_GUID,
                    "HavensRespec.cfg",
                    Log.LogWarning);
                ConfigFileHelper.ReplacePluginConfig(this, namedConfig, Log.LogWarning);
                _config = new RespecConfig(namedConfig);

                if (!_config.Enabled.Value)
                {
                    Log.LogInfo($"{PluginInfo.PLUGIN_NAME} disabled in config.");
                    return;
                }

                _resetService = new SkillResetService(Log, () => IsDebugLoggingEnabled);
                _costService = new CostService(Log, _config);
                _controller = new RespecController(Log, _config, _resetService, _costService);
                _controller.Install();

                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                _harmony.PatchAll(typeof(SkillsSetupProfessionPatch));

                try
                {
                    VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log);
                }
                catch (Exception ex)
                {
                    Log.LogDebug($"[Respec] VersionChecker swallowed: {ex.Message}");
                }

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loaded.");
            }
            catch (Exception ex)
            {
                Log.LogError($"{PluginInfo.PLUGIN_NAME} Awake failed: {ex}");
            }
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
                Log?.LogInfo($"[Lifecycle] {PluginInfo.PLUGIN_NAME} OnDestroy during expected teardown (scene: {sceneName})");
            else
                Log?.LogWarning($"[Lifecycle] {PluginInfo.PLUGIN_NAME} OnDestroy outside expected teardown (scene: {sceneName})");

            try
            {
                _controller?.Uninstall();
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"{PluginInfo.PLUGIN_NAME} OnDestroy: Uninstall failed: {ex.Message}");
            }
            finally
            {
                try
                {
                    _harmony?.UnpatchSelf();
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"{PluginInfo.PLUGIN_NAME} OnDestroy: UnpatchSelf failed: {ex.Message}");
                }
            }
        }

        private void Update()
        {
            if (_controller == null || _config == null)
                return;

            var resetKey = _config.ResetCurrentTabHotkey.Value;
            if (resetKey != KeyCode.None && Input.GetKeyDown(resetKey))
            {
                var profession = _controller.TryGetActiveProfessionTab();
                if (profession.HasValue)
                    _controller.TryResetCurrentTab(profession.Value, bypassConfirm: false);
            }

            var undoKey = _config.UndoHotkey.Value;
            if (undoKey != KeyCode.None && Input.GetKeyDown(undoKey))
            {
                var profession = _controller.TryGetActiveProfessionTab();
                if (profession.HasValue)
                    _controller.TryUndoCurrentTab(profession.Value);
            }
        }
    }
}
