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

        private void OnDestroy()
        {
            try
            {
                _controller?.Uninstall();
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"{PluginInfo.PLUGIN_NAME} OnDestroy swallowed: {ex.Message}");
            }
        }

        private void Update()
        {
            if (_controller == null || _config == null)
                return;

            var resetKey = _config.ResetCurrentTabHotkey.Value;
            if (resetKey != KeyCode.None && Input.GetKeyDown(resetKey))
            {
                var profession = TryGetActiveProfessionTab();
                if (profession.HasValue)
                    _controller.TryResetCurrentTab(profession.Value, bypassConfirm: false);
            }

            var undoKey = _config.UndoHotkey.Value;
            if (undoKey != KeyCode.None && Input.GetKeyDown(undoKey))
            {
                var profession = TryGetActiveProfessionTab();
                if (profession.HasValue)
                    _controller.TryUndoCurrentTab(profession.Value);
            }
        }

        /// <summary>
        /// Best-effort resolution of which profession tab is currently visible. The game has no
        /// single API for this, so we peek at each per-profession panel GameObject (via our
        /// reset service's reflection cache) and return the first one whose root is active.
        /// </summary>
        private static ProfessionType? TryGetActiveProfessionTab()
        {
            try
            {
                var player = UnityEngine.Object.FindObjectOfType<Skills>();
                if (player == null) return null;

                foreach (ProfessionType profession in Enum.GetValues(typeof(ProfessionType)))
                {
                    var panel = AccessTools.Field(typeof(Skills), ResolvePanelFieldName(profession))?.GetValue(player) as Component;
                    if (panel != null && panel.gameObject.activeInHierarchy)
                        return profession;
                }
            }
            catch
            {
                // best-effort
            }
            return null;
        }

        private static string ResolvePanelFieldName(ProfessionType profession) => profession switch
        {
            ProfessionType.Combat => "_combatPanel",
            ProfessionType.Farming => "_farmingPanel",
            ProfessionType.Mining => "_miningPanel",
            ProfessionType.Exploration => "_artisanryPanel",
            ProfessionType.Fishing => "_fishingPanel",
            _ => null,
        };
    }
}
