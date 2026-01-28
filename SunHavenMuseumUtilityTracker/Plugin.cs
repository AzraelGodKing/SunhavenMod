using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SunHavenMuseumUtilityTracker.Data;
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

        // Instance references
        private DonationManager _donationManager;
        private DonationSaveSystem _saveSystem;
        private MuseumTrackerUI _trackerUI;
        private Harmony _harmony;

        // Configuration
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _requireCtrl;

        // Static config for PersistentRunner
        public static KeyCode StaticToggleKey { get; private set; }
        public static bool StaticRequireCtrl { get; private set; }

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
            Logger.LogInfo($"Press {(_requireCtrl.Value ? "Ctrl+" : "")}{_toggleKey.Value} to open the tracker");
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

            // Set static values for PersistentRunner
            StaticToggleKey = _toggleKey.Value;
            StaticRequireCtrl = _requireCtrl.Value;

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
        }

        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null) return;

            _persistentRunner = new GameObject("MuseumTracker_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            _persistentRunner.AddComponent<PersistentRunner>();
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

            Logger.LogInfo("UI components created");
        }

        /// <summary>
        /// Ensure UI components exist (recreate if destroyed by game cleanup).
        /// </summary>
        public static void EnsureUIComponentsExist()
        {
            if (_staticTrackerUI == null)
            {
                Log?.LogInfo("[EnsureUI] Recreating UI...");
                var uiObject = new GameObject("MuseumTracker_UI");
                UnityEngine.Object.DontDestroyOnLoad(uiObject);

                _staticTrackerUI = uiObject.AddComponent<MuseumTrackerUI>();
                _staticTrackerUI.Initialize(_staticDonationManager);
                _staticTrackerUI.SetToggleKey(StaticToggleKey, StaticRequireCtrl);
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
            _harmony?.UnpatchSelf();
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

            if (togglePressed && (ctrlPressed == Plugin.StaticRequireCtrl))
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
        public const string PLUGIN_GUID = "com.myleek.sunhavenmuseumutilitytracker";
        public const string PLUGIN_NAME = "Sun Haven Museum Utility Tracker";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
