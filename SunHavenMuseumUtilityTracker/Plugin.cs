using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SunHavenMuseumUtilityTracker.Data;
using SunHavenMuseumUtilityTracker.DebugTools;
using SunHavenMuseumUtilityTracker.Patches;
using SunHavenMuseumUtilityTracker.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SunHavenMuseumUtilityTracker
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // Static references for access from patches
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Static references that survive plugin destruction
        private static DonationManager _staticDonationManager;
        private static DonationSaveSystem _staticSaveSystem;
        private static MuseumTrackerUI _staticTrackerUI;
        private static GameObject _persistentRunner;
        private static PersistentRunner _persistentRunnerComponent;

        // Instance references
        private DonationManager _donationManager;
        private DonationSaveSystem _saveSystem;
        private MuseumTrackerUI _trackerUI;
        private Harmony _harmony;

        // Configuration
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _requireCtrl;
        private ConfigEntry<KeyCode> _altToggleKey;

        // Static config for PersistentRunner
        public static KeyCode StaticToggleKey { get; private set; }
        public static bool StaticRequireCtrl { get; private set; }
        public static KeyCode StaticAltToggleKey { get; private set; }

        private string _lastScene = "";

        public DonationManager DonationManager => _donationManager;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} loading...");

            // Bind configuration
            BindConfiguration();

            // Create persistent runner first
            CreatePersistentRunner();

            // Initialize managers
            _donationManager = new DonationManager();
            _staticDonationManager = _donationManager;

            _saveSystem = new DonationSaveSystem(_donationManager);
            _staticSaveSystem = _saveSystem;

            // Create UI
            CreateUIComponents();

            // Apply Harmony patches
            ApplyPatches();

            // Subscribe to scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;

            Logger.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
            Logger.LogInfo($"Press {(_requireCtrl.Value ? "Ctrl+" : "")}{_toggleKey.Value} or {_altToggleKey.Value} to open the tracker");
        }

        private void BindConfiguration()
        {
            _toggleKey = Config.Bind(
                "Hotkeys",
                "ToggleKey",
                KeyCode.C,
                "Key to toggle the Museum Tracker window"
            );

            _requireCtrl = Config.Bind(
                "Hotkeys",
                "RequireCtrl",
                true,
                "Require Ctrl to be held when pressing the toggle key"
            );

            _altToggleKey = Config.Bind(
                "Hotkeys",
                "AltToggleKey",
                KeyCode.F7,
                "Alternative key to toggle the Museum Tracker (no modifier required). Useful for Steam Deck."
            );

            // Set static values for PersistentRunner
            StaticToggleKey = _toggleKey.Value;
            StaticRequireCtrl = _requireCtrl.Value;
            StaticAltToggleKey = _altToggleKey.Value;

            // Listen for config changes
            _toggleKey.SettingChanged += (_, _) =>
            {
                StaticToggleKey = _toggleKey.Value;
                _trackerUI?.SetToggleKey(_toggleKey.Value, _requireCtrl.Value);
            };
            _requireCtrl.SettingChanged += (_, _) =>
            {
                StaticRequireCtrl = _requireCtrl.Value;
                _trackerUI?.SetToggleKey(_toggleKey.Value, _requireCtrl.Value);
            };
            _altToggleKey.SettingChanged += (_, _) =>
            {
                StaticAltToggleKey = _altToggleKey.Value;
            };
        }

        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null && _persistentRunnerComponent != null) return;

            _persistentRunner = new GameObject("MuseumTracker_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
            Logger.LogInfo("[PersistentRunner] Created");
        }

        private void CreateUIComponents()
        {
            var uiObject = new GameObject("MuseumTracker_UI");
            DontDestroyOnLoad(uiObject);

            _trackerUI = uiObject.AddComponent<MuseumTrackerUI>();
            _trackerUI.Initialize(_donationManager);
            _trackerUI.SetToggleKey(_toggleKey.Value, _requireCtrl.Value);
            _staticTrackerUI = _trackerUI;

            // Create Debug Mode (only activates for authorized users via F10)
            uiObject.AddComponent<DebugMode>();

            Logger.LogInfo("UI components created");
        }

        /// <summary>
        /// Ensure UI components exist (recreate if destroyed by game cleanup).
        /// Called from PlayerPatches when a character loads.
        /// </summary>
        public static void EnsureUIComponentsExist()
        {
            try
            {
                // Check if PersistentRunner was destroyed and recreate it
                if (_persistentRunner == null || _persistentRunnerComponent == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("MuseumTracker_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
                    Log?.LogInfo("[EnsureUI] PersistentRunner recreated");
                }

                // Check if TrackerUI was destroyed and recreate it
                if (_staticTrackerUI == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating TrackerUI...");
                    var uiObject = new GameObject("MuseumTracker_UI");
                    UnityEngine.Object.DontDestroyOnLoad(uiObject);
                    // NOTE: Do NOT use HideFlags.HideAndDontSave on TrackerUI!
                    // That flag prevents Unity's OnGUI from being called, which breaks the UI rendering.
                    // Only PersistentRunner needs HideFlags (it only uses Update, not OnGUI).

                    _staticTrackerUI = uiObject.AddComponent<MuseumTrackerUI>();
                    _staticTrackerUI.Initialize(_staticDonationManager);
                    _staticTrackerUI.SetToggleKey(StaticToggleKey, StaticRequireCtrl);

                    // Recreate DebugMode component
                    uiObject.AddComponent<DebugTools.DebugMode>();

                    Log?.LogInfo("[EnsureUI] TrackerUI recreated");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error recreating UI: {ex.Message}");
            }
        }

        private void ApplyPatches()
        {
            try
            {
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

                // Patch Player.InitializeAsOwner for data loading
                var playerType = typeof(Wish.Player);
                var initMethod = AccessTools.Method(playerType, "InitializeAsOwner");
                if (initMethod != null)
                {
                    var patchMethod = AccessTools.Method(typeof(PlayerPatches), "OnPlayerInitialized");
                    _harmony.Patch(initMethod, postfix: new HarmonyMethod(patchMethod));
                    Logger.LogInfo("Patched Player.InitializeAsOwner");
                }
                else
                {
                    Logger.LogWarning("Could not find Player.InitializeAsOwner");
                }

                // Patch GameSave.LoadCharacter for character name extraction
                var gameSaveType = typeof(Wish.GameSave);
                var loadCharMethod = AccessTools.Method(gameSaveType, "LoadCharacter", new[] { typeof(int) });
                if (loadCharMethod != null)
                {
                    var patchMethod = AccessTools.Method(typeof(GameSavePatches), "OnLoadCharacter");
                    _harmony.Patch(loadCharMethod, postfix: new HarmonyMethod(patchMethod));
                    Logger.LogInfo("Patched GameSave.LoadCharacter");
                }

                Logger.LogInfo("Harmony patches applied");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error applying patches: {ex.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"[SceneChange] Scene loaded: '{scene.name}'");

            string sceneName = scene.name.ToLowerInvariant();
            if (sceneName.Contains("menu") || sceneName.Contains("title") || sceneName.Contains("mainmenu"))
            {
                Logger.LogInfo($"Menu scene detected: {scene.name}");
                PlayerPatches.SaveAndReset();
            }
        }

        private void Update()
        {
            // Check auto-save
            if (PlayerPatches.IsDataLoaded)
            {
                _saveSystem?.CheckAutoSave();
            }

            // Scene polling backup
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != _lastScene)
            {
                Logger.LogInfo($"[ScenePoll] Scene changed: '{_lastScene}' -> '{currentScene}'");
                _lastScene = currentScene;

                string lower = currentScene.ToLowerInvariant();
                if (lower.Contains("menu") || lower.Contains("title"))
                {
                    PlayerPatches.SaveAndReset();
                }
            }
        }

        private void OnApplicationQuit()
        {
            Logger.LogInfo("Application quitting, saving data...");
            _saveSystem?.ForceSave();
        }

        private void OnDestroy()
        {
            Logger.LogWarning("[CRITICAL] Plugin OnDestroy called!");
            _saveSystem?.ForceSave();
            // NOTE: Do NOT call _harmony?.UnpatchSelf() here!
            // Patches must survive plugin destruction so OnPlayerInitialized
            // can trigger EnsureUIComponentsExist() on character reload.
        }

        #region Public API

        public static DonationManager GetDonationManager() => _staticDonationManager;
        public static MuseumTrackerUI GetTrackerUI() => _staticTrackerUI;

        public static void SaveData()
        {
            _staticSaveSystem?.ForceSave();
        }

        public static void LoadDataForPlayer(string playerName)
        {
            var data = _staticSaveSystem?.Load(playerName);
            _staticDonationManager?.LoadForCharacter(playerName, data);
        }

        public static void ToggleUI()
        {
            _staticTrackerUI?.Toggle();
        }

        #endregion
    }

    /// <summary>
    /// Persistent runner that survives game cleanup.
    /// </summary>
    public class PersistentRunner : MonoBehaviour
    {
        private string _lastScene = "";

        private void Update()
        {
            CheckHotkeys();
            CheckSceneChange();
        }

        private void CheckHotkeys()
        {
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool togglePressed = Input.GetKeyDown(Plugin.StaticToggleKey);
            bool altTogglePressed = Input.GetKeyDown(Plugin.StaticAltToggleKey);

            // Check main toggle key (with optional Ctrl modifier)
            if (togglePressed && (ctrlPressed == Plugin.StaticRequireCtrl))
            {
                Plugin.ToggleUI();
            }
            // Check alt toggle key (no modifier required)
            else if (altTogglePressed)
            {
                Plugin.ToggleUI();
            }
        }

        private void CheckSceneChange()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != _lastScene)
            {
                Plugin.Log?.LogInfo($"[PersistentRunner] Scene changed: '{_lastScene}' -> '{currentScene}'");
                _lastScene = currentScene;

                string lower = currentScene.ToLowerInvariant();
                if (lower.Contains("menu") || lower.Contains("title"))
                {
                    Plugin.Log?.LogInfo("[PersistentRunner] Menu scene detected");
                    PlayerPatches.SaveAndReset();
                }
            }
        }

        private void OnDestroy()
        {
            Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy called - this should NOT happen!");
        }
    }

    /// <summary>
    /// Plugin information.
    /// </summary>
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.sunhavenmuseumutilitytracker";
        public const string PLUGIN_NAME = "Sun Haven Museum Utility Tracker";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
