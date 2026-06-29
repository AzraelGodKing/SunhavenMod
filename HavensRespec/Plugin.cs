using System;
using System.Reflection;
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
        public static bool IsDebugLoggingEnabled => _staticConfig?.DebugLogging?.Value == true;

        private static Harmony _staticHarmony;
        private static RespecConfig _staticConfig;
        private static SkillResetService _staticResetService;
        private static CostService _staticCostService;
        private static RespecController _staticController;
        private static GameObject _persistentRunner;
        private static RespecPersistentRunner _persistentRunnerComponent;

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
                _staticConfig = new RespecConfig(namedConfig);
                LocalizationBootstrap.BindForceEnglish(namedConfig);

                if (!_staticConfig.Enabled.Value)
                {
                    Log.LogInfo($"{PluginInfo.PLUGIN_NAME} disabled in config.");
                    return;
                }

                EnsureModServices();
                EnsureHarmonyPatched();
                CreatePersistentRunner();

                SceneManager.sceneLoaded += OnSceneLoaded;

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
            TeardownMod(fullShutdown: true);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            string sceneLower = sceneName.ToLowerInvariant();
            bool expectedTeardown = _applicationQuitting || !Application.isPlaying || sceneLower.Contains("menu") || sceneLower.Contains("title");
            if (expectedTeardown)
                Log?.LogInfo($"[Lifecycle] {PluginInfo.PLUGIN_NAME} OnDestroy during expected teardown (scene: {sceneName})");
            else
                Log?.LogWarning($"[Lifecycle] {PluginInfo.PLUGIN_NAME} OnDestroy outside expected teardown (scene: {sceneName})");

            if (_applicationQuitting)
            {
                TeardownMod(fullShutdown: true);
                return;
            }

            // Scene transition: keep Harmony patches, controller hooks, and localization alive.
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log?.LogDebug($"[Respec] Scene loaded: {scene.name}");

            if (scene.name == "MainMenu" || scene.name == "Bootstrap")
                return;

            EnsureModReady();
        }

        /// <summary>
        /// Re-wire hooks and the persistent runner after the BepInEx plugin MonoBehaviour is destroyed on scene load.
        /// </summary>
        public static void EnsureModReady()
        {
            if (_staticConfig == null || !_staticConfig.Enabled.Value)
                return;

            try
            {
                EnsureModServices();
                EnsureHarmonyPatched();
                CreatePersistentRunner();

                if (_staticController != null && SkillsSetupProfessionPatch.OnSetup == null)
                {
                    Log?.LogInfo("[EnsureMod] Re-installing RespecController hooks after plugin recreation.");
                    _staticController.Install();
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureMod] Error: {ex.Message}");
            }
        }

        private static void EnsureModServices()
        {
            if (_staticResetService == null)
                _staticResetService = new SkillResetService(Log, () => IsDebugLoggingEnabled);

            if (_staticCostService == null)
                _staticCostService = new CostService(Log, _staticConfig);

            if (_staticController == null)
            {
                _staticController = new RespecController(Log, _staticConfig, _staticResetService, _staticCostService);
                _staticController.Install();
            }
        }

        private static void EnsureHarmonyPatched()
        {
            if (_staticHarmony != null)
                return;

            _staticHarmony = new Harmony(PluginInfo.PLUGIN_GUID);
            LocalizationBootstrap.Init(PluginInfo.PLUGIN_GUID, _staticHarmony, Log, Assembly.GetExecutingAssembly());
            ModLocalization.LanguageChanged += OnLanguageChanged;
            _staticHarmony.PatchAll(typeof(SkillsSetupProfessionPatch));
            Log?.LogInfo("[Respec] Harmony patches applied.");
        }

        private static void CreatePersistentRunner()
        {
            if (_persistentRunner != null && _persistentRunnerComponent != null)
                return;

            _persistentRunner = new GameObject("HavensRespec_PersistentRunner");
            UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            SceneRootSurvivor.TryRegisterPersistentRunnerGameObject(_persistentRunner);
            _persistentRunnerComponent = _persistentRunner.AddComponent<RespecPersistentRunner>();
            Log?.LogInfo("[PersistentRunner] Created");
        }

        private static void TeardownMod(bool fullShutdown)
        {
            ModLocalization.Shutdown();
            ModLocalization.LanguageChanged -= OnLanguageChanged;

            if (fullShutdown)
            {
                try
                {
                    _staticController?.Uninstall();
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"{PluginInfo.PLUGIN_NAME} teardown: Uninstall failed: {ex.Message}");
                }

                _staticController = null;
                _staticResetService = null;
                _staticCostService = null;

                try
                {
                    _staticHarmony?.UnpatchSelf();
                }
                catch (Exception ex)
                {
                    Log?.LogWarning($"{PluginInfo.PLUGIN_NAME} teardown: UnpatchSelf failed: {ex.Message}");
                }

                _staticHarmony = null;
            }
        }

        private static void OnLanguageChanged(string _)
        {
            _staticController?.RefreshLocalizedUi();
        }

        internal static void TickHotkeys()
        {
            if (_staticController == null || _staticConfig == null)
                return;

            var resetKey = _staticConfig.ResetCurrentTabHotkey.Value;
            if (resetKey != KeyCode.None && Input.GetKeyDown(resetKey))
            {
                var profession = _staticController.TryGetActiveProfessionTab();
                if (profession.HasValue)
                    _staticController.TryResetCurrentTab(profession.Value, bypassConfirm: false);
            }

            var undoKey = _staticConfig.UndoHotkey.Value;
            if (undoKey != KeyCode.None && Input.GetKeyDown(undoKey))
            {
                var profession = _staticController.TryGetActiveProfessionTab();
                if (profession.HasValue)
                    _staticController.TryUndoCurrentTab(profession.Value);
            }
        }
    }

    /// <summary>
    /// Survives scene transitions so hotkeys keep working when the BepInEx plugin MonoBehaviour is destroyed.
    /// </summary>
    public class RespecPersistentRunner : MonoBehaviour
    {
        private void Update()
        {
            Plugin.TickHotkeys();
        }

        private void OnDestroy()
        {
            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            string sceneLower = sceneName.ToLowerInvariant();
            bool expectedTeardown = !Application.isPlaying || sceneLower.Contains("menu") || sceneLower.Contains("title");
            if (expectedTeardown)
                Plugin.Log?.LogInfo("[PersistentRunner] OnDestroy during app quit/menu unload (expected).");
            else
                Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy outside quit/menu (unexpected).");
        }
    }
}
