using BepInEx;
using BepInEx.Logging;
using SenpaisChest.Config;
using SenpaisChest.Data;
using SenpaisChest.Integration;
using SenpaisChest.UI;
using SunhavenMods.Shared;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wish;

namespace SenpaisChest
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.azraelgodking.sunhavenmuseumutilitytracker", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.sunhaventodo", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Static references that survive Plugin destruction
        private static SmartChestManager _staticManager;
        private static SmartChestSaveSystem _staticSaveSystem;
        private static SmartChestUI _staticUI;
        private static SmartChestConfig _staticConfig;

        // Cross-mod integration
        private static MuseumTodoIntegration _museumTodoIntegration;

        // Track the chest the player is currently interacting with
        internal static Chest CurrentInteractingChest;

        private Harmony _harmony;
        private SmartChestManager _manager;
        private SmartChestSaveSystem _saveSystem;
        private SmartChestUI _ui;
        private SmartChestConfig _config;

        // PersistentRunner
        private static GameObject _persistentRunner;
        private static SmartChestPersistentRunner _updateRunner;

        // Scene tracking
        private string _lastKnownScene = "";
        private bool _wasInMenuScene = true;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            CreatePersistentRunner();

            try
            {
                // Initialize config
                _config = new SmartChestConfig();
                _config.Initialize(Config);
                _staticConfig = _config;

                // Initialize manager and save system
                _manager = new SmartChestManager();
                _saveSystem = new SmartChestSaveSystem(_manager);
                _staticManager = _manager;
                _staticSaveSystem = _saveSystem;

                // Create UI (separate GameObject, no HideFlags — needs OnGUI)
                var uiObject = new GameObject("SenpaisChest_UI");
                DontDestroyOnLoad(uiObject);
                _ui = uiObject.AddComponent<SmartChestUI>();
                _ui.Initialize(_manager);
                _staticUI = _ui;

                // Apply Harmony patches
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                ApplyPatches();

                // Initialize cross-mod integrations
                InitializeIntegrations();

                // Subscribe to scene loading
                SceneManager.sceneLoaded += OnSceneLoaded;

                // Check for updates
                if (_config.CheckForUpdates.Value)
                {
                    VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log,
                        result => result.NotifyUpdateAvailable(Log));
                }

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
                Log.LogInfo($"Press {(_config.RequireCtrlModifier.Value ? "Ctrl+" : "")}{_config.ToggleKey.Value} to configure a chest while interacting with it");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to load {PluginInfo.PLUGIN_NAME}: {ex}");
            }
        }

        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null)
            {
                Log.LogInfo("PersistentRunner already exists");
                return;
            }

            _persistentRunner = new GameObject("SenpaisChest_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            _updateRunner = _persistentRunner.AddComponent<SmartChestPersistentRunner>();

            Log.LogInfo("Created hidden PersistentRunner");
        }

        public static void EnsureUIComponentsExist()
        {
            try
            {
                if (_persistentRunner == null || _updateRunner == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("SenpaisChest_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    _updateRunner = _persistentRunner.AddComponent<SmartChestPersistentRunner>();
                }

                if (_staticUI == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating SmartChestUI...");
                    var uiObject = new GameObject("SenpaisChest_UI");
                    UnityEngine.Object.DontDestroyOnLoad(uiObject);
                    _staticUI = uiObject.AddComponent<SmartChestUI>();
                    _staticUI.Initialize(_staticManager);
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error recreating UI: {ex.Message}");
            }
        }

        private void InitializeIntegrations()
        {
            try
            {
                var pluginInfos = BepInEx.Bootstrap.Chainloader.PluginInfos;
                bool hasSmut = pluginInfos.ContainsKey("com.azraelgodking.sunhavenmuseumutilitytracker");
                bool hasTodo = pluginInfos.ContainsKey("com.azraelgodking.sunhaventodo");

                if (hasSmut && hasTodo)
                {
                    _museumTodoIntegration = new MuseumTodoIntegration();
                }
                else
                {
                    if (!hasSmut) Log.LogInfo("[Integrations] S.M.U.T. not found");
                    if (!hasTodo) Log.LogInfo("[Integrations] SunhavenTodo not found");
                    Log.LogInfo("[Integrations] Museum todo integration disabled (requires both S.M.U.T. and Todo)");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[Integrations] Error initializing: {ex.Message}");
            }
        }

        #region Harmony Patches

        private void ApplyPatches()
        {
            try
            {
                // Player.InitializeAsOwner → load character data
                PatchMethod(typeof(Player), "InitializeAsOwner",
                    typeof(Plugin), nameof(OnPlayerInitialized));

                // Chest.Interact → track current chest
                PatchMethod(typeof(Chest), "Interact",
                    typeof(Plugin), nameof(OnChestInteract),
                    new[] { typeof(int) });

                // Chest.EndInteract → clear tracked chest
                PatchMethod(typeof(Chest), "EndInteract",
                    typeof(Plugin), nameof(OnChestEndInteract),
                    new[] { typeof(int) });

                Log.LogInfo("Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply patches: {ex}");
            }
        }

        private void PatchMethod(Type targetType, string methodName,
            Type patchType, string patchMethodName, Type[] parameters = null)
        {
            var original = parameters != null
                ? AccessTools.Method(targetType, methodName, parameters)
                : AccessTools.Method(targetType, methodName);

            if (original == null)
            {
                Log.LogWarning($"Could not find method {targetType.Name}.{methodName}");
                return;
            }

            var postfix = AccessTools.Method(patchType, patchMethodName);
            _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.LogInfo($"Patched {targetType.Name}.{methodName}");
        }

        private static void OnPlayerInitialized(Player __instance)
        {
            try
            {
                if (__instance != Player.Instance)
                    return;

                EnsureUIComponentsExist();

                // Use GameSave.CurrentCharacter (static property, no SingletonBehaviour needed)
                string characterName = null;
                var currentChar = GameSave.CurrentCharacter;
                if (currentChar != null)
                {
                    characterName = currentChar.characterName;
                }

                if (string.IsNullOrEmpty(characterName))
                {
                    Log?.LogWarning("Player initialized but no character name found");
                    return;
                }

                Log?.LogInfo($"Player initialized: {characterName}");
                _staticManager?.SetCharacterName(characterName);
                _museumTodoIntegration?.Reset();

                var data = _staticSaveSystem?.Load(characterName);
                _staticManager?.LoadData(data);
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error in OnPlayerInitialized: {ex}");
            }
        }

        private static void OnChestInteract(Chest __instance, int interactType)
        {
            if (interactType != 0)
                return;

            CurrentInteractingChest = __instance;
        }

        private static void OnChestEndInteract(Chest __instance, int interactType)
        {
            if (CurrentInteractingChest == __instance)
            {
                CurrentInteractingChest = null;
                _staticUI?.Hide();
            }
        }

        #endregion

        #region Scene Management

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool isMenuScene = scene.name == "MainMenu" || scene.name == "Menu";

            if (isMenuScene && !_wasInMenuScene)
            {
                Log.LogInfo("Returned to menu, saving data...");
                _staticSaveSystem?.Save();
            }

            _wasInMenuScene = isMenuScene;
            _lastKnownScene = scene.name;
        }

        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            // Do NOT unpatch Harmony — patches survive and enable reloads
            Log?.LogInfo("Plugin OnDestroy called — static references preserved");
            _staticSaveSystem?.Save();
        }

        private void OnApplicationQuit()
        {
            Log?.LogInfo("Application quitting — saving data");
            _staticSaveSystem?.Save();
        }

        #endregion

        #region Static Accessors (for PersistentRunner)

        internal static SmartChestManager GetManager() => _staticManager;
        internal static SmartChestSaveSystem GetSaveSystem() => _staticSaveSystem;
        internal static SmartChestUI GetUI() => _staticUI;
        internal static SmartChestConfig GetConfig() => _staticConfig;
        internal static MuseumTodoIntegration GetMuseumTodoIntegration() => _museumTodoIntegration;

        #endregion
    }

    /// <summary>
    /// Hidden MonoBehaviour that survives game cleanup. Handles scan timer, hotkeys, and auto-save.
    /// </summary>
    public class SmartChestPersistentRunner : MonoBehaviour
    {
        private float _scanTimer;
        private float _autoSaveTimer;
        private const float AUTO_SAVE_INTERVAL = 300f; // 5 minutes

        private void Update()
        {
            var config = Plugin.GetConfig();
            var manager = Plugin.GetManager();

            if (config == null || manager == null)
                return;

            float dt = Time.unscaledDeltaTime;

            // Scan timer
            _scanTimer += dt;
            if (_scanTimer >= config.GetScanInterval())
            {
                _scanTimer = 0f;
                try
                {
                    manager.ExecuteScan(
                        config.MaxItemsPerScan.Value,
                        config.EnableNotifications.Value);

                    // After scan, check for museum items in chests
                    Plugin.GetMuseumTodoIntegration()?.OnScanComplete();
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"Error during scan: {ex}");
                }
            }

            // Auto-save timer
            _autoSaveTimer += dt;
            if (_autoSaveTimer >= AUTO_SAVE_INTERVAL)
            {
                _autoSaveTimer = 0f;
                if (manager.IsDirty)
                {
                    Plugin.GetSaveSystem()?.Save();
                }
            }

            // Hotkey detection
            DetectHotkey(config);
        }

        private void DetectHotkey(SmartChestConfig config)
        {
            var toggleKey = SmartChestConfig.StaticToggleKey;
            var requireCtrl = SmartChestConfig.StaticRequireCtrl;

            if (Input.GetKeyDown(toggleKey))
            {
                if (requireCtrl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
                    return;

                var ui = Plugin.GetUI();
                if (ui == null)
                    return;

                // Only toggle if player is interacting with a chest
                if (Plugin.CurrentInteractingChest != null)
                {
                    ui.ToggleForChest(Plugin.CurrentInteractingChest);
                }
            }
        }

        private void OnDestroy()
        {
            Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy called — this should NOT happen!");
        }
    }
}
