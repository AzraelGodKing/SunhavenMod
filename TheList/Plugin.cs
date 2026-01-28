using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using TheList.Data;
using TheList.Patches;
using TheList.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheList
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Static references that survive plugin destruction
        private static ListManager _staticListManager;
        private static ListSaveSystem _staticSaveSystem;
        private static ListUI _staticListUI;
        private static ListHUD _staticListHUD;
        private static GameObject _persistentRunner;
        private static PersistentRunner _updateRunner;

        // Instance references
        private ListManager _listManager;
        private ListSaveSystem _saveSystem;
        private ListUI _listUI;
        private ListHUD _listHUD;
        private Harmony _harmony;

        // Config
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _requireCtrlModifier;
        private ConfigEntry<KeyCode> _altToggleKey;
        private ConfigEntry<bool> _enableHUD;
        private ConfigEntry<ListHUD.HUDPosition> _hudPosition;
        private ConfigEntry<float> _autoSaveInterval;

        // Static config for PersistentRunner
        public static KeyCode StaticToggleKey { get; private set; }
        public static bool StaticRequireCtrl { get; private set; }
        public static KeyCode StaticAltToggleKey { get; private set; }

        private string _lastScene = "";

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            // Initialize config
            InitializeConfig();

            // Create persistent runner first (survives game cleanup)
            CreatePersistentRunner();

            // Initialize core systems
            _listManager = new ListManager();
            _staticListManager = _listManager;

            _saveSystem = new ListSaveSystem(_listManager);
            _staticSaveSystem = _saveSystem;

            // Create UI components on a separate object
            CreateUIComponents();

            // Apply Harmony patches
            ApplyPatches();

            // Subscribe to scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
            Log.LogInfo($"Press {(_requireCtrlModifier.Value ? "Ctrl+" : "")}{_toggleKey.Value} or {_altToggleKey.Value} to open the list");
        }

        private void InitializeConfig()
        {
            _toggleKey = Config.Bind("UI", "ToggleKey", KeyCode.J,
                "Key to toggle the list UI");

            _requireCtrlModifier = Config.Bind("UI", "RequireCtrlModifier", false,
                "Require Ctrl key with toggle key");

            _altToggleKey = Config.Bind("UI", "AltToggleKey", KeyCode.F9,
                "Alternative key to toggle list (no modifier required)");

            _enableHUD = Config.Bind("UI", "EnableHUD", true,
                "Show mini HUD with pending task count");

            _hudPosition = Config.Bind("UI", "HUDPosition", ListHUD.HUDPosition.TopRight,
                "Position of the mini HUD");

            _autoSaveInterval = Config.Bind("Saving", "AutoSaveInterval", 60f,
                "Auto-save interval in seconds");

            // Set static values for PersistentRunner
            StaticToggleKey = _toggleKey.Value;
            StaticRequireCtrl = _requireCtrlModifier.Value;
            StaticAltToggleKey = _altToggleKey.Value;

            // Listen for config changes
            _toggleKey.SettingChanged += (_, _) =>
            {
                StaticToggleKey = _toggleKey.Value;
                _listUI?.SetToggleKey(_toggleKey.Value, _requireCtrlModifier.Value);
            };
            _requireCtrlModifier.SettingChanged += (_, _) =>
            {
                StaticRequireCtrl = _requireCtrlModifier.Value;
                _listUI?.SetToggleKey(_toggleKey.Value, _requireCtrlModifier.Value);
            };
            _altToggleKey.SettingChanged += (_, _) =>
            {
                StaticAltToggleKey = _altToggleKey.Value;
                _listUI?.SetAltToggleKey(_altToggleKey.Value);
            };
            _enableHUD.SettingChanged += (_, _) => _listHUD?.SetEnabled(_enableHUD.Value);
            _hudPosition.SettingChanged += (_, _) => _listHUD?.SetPosition(_hudPosition.Value);
        }

        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null) return;

            _persistentRunner = new GameObject("TheList_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            _updateRunner = _persistentRunner.AddComponent<PersistentRunner>();
            Log.LogInfo("[PersistentRunner] Created hidden persistent runner");
        }

        private void CreateUIComponents()
        {
            var uiObject = new GameObject("TheList_UI");
            DontDestroyOnLoad(uiObject);
            // Don't use HideFlags.HideAndDontSave - it breaks OnGUI

            _listUI = uiObject.AddComponent<ListUI>();
            _listUI.Initialize(_listManager);
            _listUI.SetToggleKey(_toggleKey.Value, _requireCtrlModifier.Value);
            _listUI.SetAltToggleKey(_altToggleKey.Value);
            _staticListUI = _listUI;

            _listHUD = uiObject.AddComponent<ListHUD>();
            _listHUD.Initialize(_listManager);
            _listHUD.SetEnabled(_enableHUD.Value);
            _listHUD.SetPosition(_hudPosition.Value);
            _staticListHUD = _listHUD;

            Log.LogInfo("UI components created");
        }

        /// <summary>
        /// Ensure UI components exist (recreate if destroyed by game cleanup).
        /// </summary>
        public static void EnsureUIComponentsExist()
        {
            if (_staticListUI == null)
            {
                Log?.LogInfo("[EnsureUI] Recreating ListUI...");
                var uiObject = new GameObject("TheList_UI");
                UnityEngine.Object.DontDestroyOnLoad(uiObject);

                _staticListUI = uiObject.AddComponent<ListUI>();
                _staticListUI.Initialize(_staticListManager);
                _staticListUI.SetToggleKey(StaticToggleKey, StaticRequireCtrl);
                _staticListUI.SetAltToggleKey(StaticAltToggleKey);

                _staticListHUD = uiObject.AddComponent<ListHUD>();
                _staticListHUD.Initialize(_staticListManager);
            }
        }

        private void ApplyPatches()
        {
            try
            {
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

                // Patch Player.InitializeAsOwner for list loading
                var playerType = typeof(Wish.Player);
                var initMethod = AccessTools.Method(playerType, "InitializeAsOwner");
                if (initMethod != null)
                {
                    var patchMethod = AccessTools.Method(typeof(PlayerPatches), "OnPlayerInitialized");
                    _harmony.Patch(initMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.LogInfo("Patched Player.InitializeAsOwner");
                }
                else
                {
                    Log.LogWarning("Could not find Player.InitializeAsOwner");
                }

                // Patch GameSave.LoadCharacter for character name extraction
                var gameSaveType = typeof(Wish.GameSave);
                var loadCharMethod = AccessTools.Method(gameSaveType, "LoadCharacter", new[] { typeof(int) });
                if (loadCharMethod != null)
                {
                    var patchMethod = AccessTools.Method(typeof(GameSavePatches), "OnLoadCharacter");
                    _harmony.Patch(loadCharMethod, postfix: new HarmonyMethod(patchMethod));
                    Log.LogInfo("Patched GameSave.LoadCharacter");
                }

                Log.LogInfo("Harmony patches applied");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error applying patches: {ex.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"[SceneChange] Scene loaded: '{scene.name}' (mode: {mode})");

            // Check for menu scenes
            string sceneName = scene.name.ToLowerInvariant();
            if (sceneName.Contains("menu") || sceneName.Contains("title") || sceneName.Contains("mainmenu"))
            {
                Log.LogInfo($"Menu scene detected: {scene.name}");
                PlayerPatches.SaveAndReset();
            }
        }

        private void Update()
        {
            // Check auto-save
            if (PlayerPatches.IsListLoaded)
            {
                _saveSystem?.CheckAutoSave(_autoSaveInterval.Value);
            }

            // Scene polling backup
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != _lastScene)
            {
                Log.LogInfo($"[ScenePoll] Scene changed: '{_lastScene}' -> '{currentScene}'");
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
            Log.LogInfo("Application quitting, saving list...");
            _saveSystem?.ForceSave();
        }

        private void OnDisable()
        {
            Log.LogWarning("[CRITICAL] Plugin OnDisable called!");
            _saveSystem?.ForceSave();
        }

        private void OnDestroy()
        {
            Log.LogWarning("[CRITICAL] Plugin OnDestroy called!");
            _saveSystem?.ForceSave();
        }

        #region Public API

        public static ListManager GetListManager() => _staticListManager;
        public static ListUI GetListUI() => _staticListUI;
        public static ListHUD GetListHUD() => _staticListHUD;

        public static void SaveList()
        {
            _staticSaveSystem?.Save();
        }

        public static void LoadListForPlayer(string playerName)
        {
            _staticSaveSystem?.Load(playerName);
        }

        public static void ToggleUI()
        {
            _staticListUI?.Toggle();
        }

        #endregion
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.thelist";
        public const string PLUGIN_NAME = "The List";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    /// <summary>
    /// Persistent runner that survives game cleanup.
    /// Handles hotkey detection when main UI might be destroyed.
    /// </summary>
    public class PersistentRunner : MonoBehaviour
    {
        private string _lastScene = "";
        private int _heartbeatCounter = 0;
        private float _lastHeartbeat = 0;
        private const float HEARTBEAT_INTERVAL = 60f;

        private void Awake()
        {
            Plugin.Log?.LogInfo("[PersistentRunner] Awake");
        }

        private void Update()
        {
            // Hotkey detection
            CheckHotkeys();

            // Scene change detection
            CheckSceneChange();

            // Heartbeat logging
            if (Time.time - _lastHeartbeat >= HEARTBEAT_INTERVAL)
            {
                _heartbeatCounter++;
                _lastHeartbeat = Time.time;
                Plugin.Log?.LogInfo($"[PersistentRunner Heartbeat #{_heartbeatCounter}] Scene: {SceneManager.GetActiveScene().name}, ListLoaded: {PlayerPatches.IsListLoaded}, Character: {PlayerPatches.LoadedCharacterName ?? "none"}");
            }
        }

        private void CheckHotkeys()
        {
            // Check primary toggle key
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool togglePressed = Input.GetKeyDown(Plugin.StaticToggleKey);

            if (togglePressed && (ctrlPressed == Plugin.StaticRequireCtrl))
            {
                Plugin.ToggleUI();
                return;
            }

            // Check alt toggle key
            if (Input.GetKeyDown(Plugin.StaticAltToggleKey))
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
}
