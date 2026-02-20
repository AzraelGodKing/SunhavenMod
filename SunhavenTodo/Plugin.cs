using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SunhavenMods.Shared;
using SunhavenTodo.Data;
using SunhavenTodo.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SunhavenTodo
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Static references that survive plugin destruction
        private static TodoManager _staticTodoManager;
        private static TodoSaveSystem _staticSaveSystem;
        private static TodoUI _staticTodoUI;
        private static TodoHUD _staticTodoHUD;
        private static GameObject _persistentRunner;
        private static PersistentRunner _persistentRunnerComponent;
        private static KeyCode _staticToggleKey = KeyCode.T;
        private static bool _staticRequireCtrl = true;
        private static bool _staticAutoSave = true;
        private static float _staticAutoSaveInterval = 60f;
        private static bool _staticHUDEnabled = true;
        private static float _staticHUDPositionX = -1f;
        private static float _staticHUDPositionY = -1f;
        private static KeyCode _staticHUDToggleKey = KeyCode.H;

        // Configuration
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _requireCtrl;
        private ConfigEntry<bool> _autoSave;
        private ConfigEntry<float> _autoSaveInterval;
        private ConfigEntry<bool> _hudEnabled;
        private ConfigEntry<float> _hudPositionX;
        private ConfigEntry<float> _hudPositionY;
        private ConfigEntry<KeyCode> _hudToggleKey;
        private ConfigEntry<bool> _checkForUpdates;

        // Instance references
        private TodoManager _todoManager;
        private TodoSaveSystem _saveSystem;
        private TodoUI _todoUI;
        private TodoHUD _todoHUD;

        // State
        private Harmony _harmony;
        private float _lastAutoSaveTime;
        private bool _isDataLoaded;
        private string _loadedCharacterName;

        // Static access for patches and hotkeys
        public static KeyCode StaticToggleKey => _staticToggleKey;
        public static bool StaticRequireCtrl => _staticRequireCtrl;
        public static KeyCode StaticHUDToggleKey => _staticHUDToggleKey;
        public static bool StaticHUDEnabled => _staticHUDEnabled;

        /// <summary>Returns the display string for the open-list shortcut (e.g. "Ctrl+T" or "J").</summary>
        public static string GetOpenListShortcutDisplay()
        {
            var key = StaticToggleKey.ToString();
            return StaticRequireCtrl ? $"Ctrl+{key}" : key;
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            BindConfiguration();
            CreatePersistentRunner();
            InitializeManagers();
            ApplyPatches();

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Check for updates
            if (_checkForUpdates.Value)
            {
                VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log,
                    result => result.NotifyUpdateAvailable(Log));
            }

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
        }

        private void BindConfiguration()
        {
            _toggleKey = Config.Bind(
                "Hotkeys",
                "ToggleKey",
                KeyCode.T,
                "Key to toggle the Todo List window"
            );
            _staticToggleKey = _toggleKey.Value;
            _toggleKey.SettingChanged += (_, _) => _staticToggleKey = _toggleKey.Value;

            _requireCtrl = Config.Bind(
                "Hotkeys",
                "RequireCtrl",
                true,
                "Require Ctrl to be held when pressing the toggle key"
            );
            _staticRequireCtrl = _requireCtrl.Value;
            _requireCtrl.SettingChanged += (_, _) => _staticRequireCtrl = _requireCtrl.Value;

            _autoSave = Config.Bind(
                "Saving",
                "AutoSave",
                true,
                "Automatically save the todo list periodically"
            );
            _staticAutoSave = _autoSave.Value;
            _autoSave.SettingChanged += (_, _) => _staticAutoSave = _autoSave.Value;

            _autoSaveInterval = Config.Bind(
                "Saving",
                "AutoSaveInterval",
                60f,
                "Auto-save interval in seconds"
            );
            _staticAutoSaveInterval = _autoSaveInterval.Value;
            _autoSaveInterval.SettingChanged += (_, _) => _staticAutoSaveInterval = _autoSaveInterval.Value;

            // HUD Configuration
            _hudEnabled = Config.Bind(
                "HUD",
                "Enabled",
                true,
                "Show the movable HUD panel with top 5 urgent tasks"
            );
            _staticHUDEnabled = _hudEnabled.Value;
            _hudEnabled.SettingChanged += (_, _) =>
            {
                _staticHUDEnabled = _hudEnabled.Value;
                _staticTodoHUD?.SetEnabled(_staticHUDEnabled);
            };

            _hudPositionX = Config.Bind(
                "HUD",
                "PositionX",
                -1f,
                "HUD X position (-1 for default)"
            );
            _staticHUDPositionX = _hudPositionX.Value;

            _hudPositionY = Config.Bind(
                "HUD",
                "PositionY",
                -1f,
                "HUD Y position (-1 for default)"
            );
            _staticHUDPositionY = _hudPositionY.Value;

            _hudToggleKey = Config.Bind(
                "Hotkeys",
                "HUDToggleKey",
                KeyCode.H,
                "Key to toggle the HUD panel (with Ctrl if RequireCtrl is enabled)"
            );
            _staticHUDToggleKey = _hudToggleKey.Value;
            _hudToggleKey.SettingChanged += (_, _) => _staticHUDToggleKey = _hudToggleKey.Value;

            _checkForUpdates = Config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for mod updates on startup"
            );
        }

        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null && _persistentRunnerComponent != null) return;

            _persistentRunner = new GameObject("SunhavenTodo_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
            Log.LogInfo("[PersistentRunner] Created");
        }

        private void InitializeManagers()
        {
            _todoManager = new TodoManager();
            _staticTodoManager = _todoManager;

            _saveSystem = new TodoSaveSystem(_todoManager);
            _staticSaveSystem = _saveSystem;

            _todoManager.OnTodosChanged += OnTodosChanged;
        }

        private void ApplyPatches()
        {
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            try
            {
                // Patch player initialization to load data per character
                var playerType = AccessTools.TypeByName("Wish.Player");
                if (playerType != null)
                {
                    var initMethod = AccessTools.Method(playerType, "InitializeAsOwner");
                    if (initMethod != null)
                    {
                        var patchMethod = AccessTools.Method(typeof(PlayerPatches), nameof(PlayerPatches.OnPlayerInitialized));
                        _harmony.Patch(initMethod, postfix: new HarmonyMethod(patchMethod));
                        Log.LogInfo("Applied player initialization patch");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to apply patches: {ex.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureUIComponentsExist();
        }

        public static void EnsureUIComponentsExist()
        {
            try
            {
                // Recreate PersistentRunner if destroyed
                if (_persistentRunner == null || _persistentRunnerComponent == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("SunhavenTodo_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
                    Log?.LogInfo("[EnsureUI] PersistentRunner recreated");
                }

                // Recreate TodoUI if destroyed
                if (_staticTodoUI == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating TodoUI...");
                    var uiObject = new GameObject("SunhavenTodo_UI");
                    UnityEngine.Object.DontDestroyOnLoad(uiObject);
                    // NOTE: Do NOT use HideFlags.HideAndDontSave on TodoUI!
                    // That flag prevents Unity's OnGUI from being called, which breaks the UI rendering.
                    // Only PersistentRunner needs HideFlags (it only uses Update, not OnGUI).

                    _staticTodoUI = uiObject.AddComponent<TodoUI>();
                    _staticTodoUI.Initialize(_staticTodoManager);
                    Log?.LogInfo("[EnsureUI] TodoUI recreated");
                }

                // Recreate TodoHUD if destroyed
                if (_staticTodoHUD == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating TodoHUD...");
                    var hudObject = new GameObject("SunhavenTodo_HUD");
                    UnityEngine.Object.DontDestroyOnLoad(hudObject);

                    _staticTodoHUD = hudObject.AddComponent<TodoHUD>();
                    _staticTodoHUD.Initialize(_staticTodoManager);
                    _staticTodoHUD.SetEnabled(_staticHUDEnabled);

                    // Restore saved position if valid
                    if (_staticHUDPositionX >= 0 && _staticHUDPositionY >= 0)
                    {
                        _staticTodoHUD.SetPosition(_staticHUDPositionX, _staticHUDPositionY);
                    }

                    // Wire up position persistence
                    _staticTodoHUD.OnPositionChanged = (x, y) =>
                    {
                        _staticHUDPositionX = x;
                        _staticHUDPositionY = y;
                        Instance?._hudPositionX?.SetSerializedValue(x.ToString());
                        Instance?._hudPositionY?.SetSerializedValue(y.ToString());
                    };

                    Log?.LogInfo("[EnsureUI] TodoHUD recreated");
                }

                // Update instance reference if we have an instance
                if (Instance != null)
                {
                    Instance._todoUI = _staticTodoUI;
                    Instance._todoHUD = _staticTodoHUD;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error: {ex.Message}");
            }
        }

        private void Update()
        {
            // Auto-save logic
            if (_staticAutoSave && _staticTodoManager != null && _staticTodoManager.IsDirty)
            {
                if (Time.unscaledTime - _lastAutoSaveTime >= _staticAutoSaveInterval)
                {
                    SaveData();
                    _lastAutoSaveTime = Time.unscaledTime;
                }
            }
        }

        private void OnTodosChanged()
        {
            // Mark for auto-save in 5 seconds
            _lastAutoSaveTime = Time.unscaledTime - _staticAutoSaveInterval + 5f;
        }

        public void LoadDataForCharacter(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Log.LogWarning("Cannot load data: No character name");
                return;
            }

            // Save previous character's data if switching
            if (_isDataLoaded && _loadedCharacterName != characterName)
            {
                SaveData();
            }

            var data = _staticSaveSystem.Load(characterName);
            _staticTodoManager.LoadForCharacter(characterName, data);
            _isDataLoaded = true;
            _loadedCharacterName = characterName;
            Log.LogInfo($"Loaded todo list for character: {characterName}");
        }

        public static void SaveData()
        {
            _staticSaveSystem?.Save();
        }

        public static void ToggleUI()
        {
            EnsureUIComponentsExist();
            _staticTodoUI?.Toggle();
        }

        public static void ShowUI()
        {
            EnsureUIComponentsExist();
            _staticTodoUI?.Show();
        }

        public static void HideUI()
        {
            _staticTodoUI?.Hide();
        }

        public static TodoManager GetTodoManager() => _staticTodoManager;
        public static TodoUI GetTodoUI() => _staticTodoUI;
        public static TodoHUD GetTodoHUD() => _staticTodoHUD;

        public static void ToggleHUD()
        {
            EnsureUIComponentsExist();
            _staticTodoHUD?.Toggle();
        }

        private void OnDestroy()
        {
            Log.LogWarning("[CRITICAL] Plugin OnDestroy called!");
            SaveData();
            // NOTE: Do NOT call _harmony?.UnpatchSelf() here!
            // Patches must survive plugin destruction so OnPlayerInitialized
            // can trigger EnsureUIComponentsExist() on character reload.
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }
    }

    /// <summary>
    /// Persistent runner that survives game cleanup for hotkey detection.
    /// Uses HideFlags.HideAndDontSave to survive Unity cleanup.
    /// </summary>
    public class PersistentRunner : MonoBehaviour
    {
        private void Update()
        {
            CheckHotkeys();
        }

        private void CheckHotkeys()
        {
            // Don't process hotkeys if UI is visible (let the UI handle input)
            var todoUI = Plugin.GetTodoUI();
            if (todoUI != null && todoUI.IsVisible)
            {
                return;
            }

            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool togglePressed = Input.GetKeyDown(Plugin.StaticToggleKey);
            bool hudTogglePressed = Input.GetKeyDown(Plugin.StaticHUDToggleKey);

            if (togglePressed && (ctrlPressed == Plugin.StaticRequireCtrl))
            {
                Plugin.ToggleUI();
            }

            // HUD toggle (also uses Ctrl if required)
            if (hudTogglePressed && (ctrlPressed == Plugin.StaticRequireCtrl))
            {
                Plugin.ToggleHUD();
            }
        }

        private void OnDestroy()
        {
            Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy called - this should NOT happen!");
        }
    }

    /// <summary>
    /// Harmony patches for game integration
    /// </summary>
    public static class PlayerPatches
    {
        private static bool _isDataLoaded = false;
        private static string _loadedCharacterName = null;

        public static void OnPlayerInitialized(object __instance)
        {
            try
            {
                Plugin.EnsureUIComponentsExist();

                string characterName = GetCurrentCharacterName(__instance);

                if (_isDataLoaded && _loadedCharacterName != characterName)
                {
                    Plugin.SaveData();
                    ResetState();
                }

                if (!string.IsNullOrEmpty(characterName))
                {
                    Plugin.Instance?.LoadDataForCharacter(characterName);
                    _isDataLoaded = true;
                    _loadedCharacterName = characterName;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        private static string GetCurrentCharacterName(object player)
        {
            try
            {
                // Try to get character name from GameSave
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                if (gameSaveType != null)
                {
                    var currentProp = AccessTools.Property(gameSaveType, "Current");
                    if (currentProp != null)
                    {
                        var current = currentProp.GetValue(null);
                        if (current != null)
                        {
                            var characterNameProp = AccessTools.Property(current.GetType(), "characterName");
                            if (characterNameProp != null)
                            {
                                var name = characterNameProp.GetValue(current) as string;
                                if (!string.IsNullOrEmpty(name))
                                    return name;
                            }
                        }
                    }
                }

                // Fallback: try to get from player instance
                if (player != null)
                {
                    var nameProp = AccessTools.Property(player.GetType(), "playerName");
                    if (nameProp != null)
                    {
                        var name = nameProp.GetValue(player) as string;
                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to get character name: {ex.Message}");
            }

            return "DefaultCharacter";
        }

        private static void ResetState()
        {
            _isDataLoaded = false;
            _loadedCharacterName = null;
        }
    }
}
