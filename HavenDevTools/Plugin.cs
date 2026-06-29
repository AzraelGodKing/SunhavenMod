using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HavenDevTools.Config;
using HavenDevTools.Services;
using HavenDevTools.UI;
using SunhavenMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HavenDevTools
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.azraelgodking.thevault", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.sunhavenmuseumutilitytracker", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.havensbirthright", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.senpaischest", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.squirrelsbirthdayreminder", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.sunhaventodo", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.azraelgodking.trinketfortune", BepInDependency.DependencyFlags.SoftDependency)]
    // NOTE: Do NOT add BepInDependency on HavensAlmanac - it creates a cyclic dep (Almanac already depends on HavenDevTools).
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static ConfigFile ConfigFile { get; private set; }

        // Static references that survive Plugin destruction
        private static ItemInspector _staticItemInspector;
        private static CurrencyTracker _staticCurrencyTracker;
        private static BundleInspector _staticBundleInspector;
        private static RaceModifierTracker _staticRaceModifierTracker;
        private static DebugWindow _staticDebugWindow;
        private static DebugOverlay _staticDebugOverlay;
        private static CommandConsole _staticCommandConsole;
        private static LogViewerPanel _staticLogViewer;

        // Persistent runner
        private static GameObject _persistentRunner;
        private static PersistentRunner _persistentRunnerComponent;

        // Static config values for PersistentRunner hotkey detection
        internal static KeyCode StaticToggleKey = KeyCode.F11;
        internal static KeyCode StaticOverlayToggleKey = KeyCode.F6;

        // Convenience gate only (not security): anyone can patch or rebuild the DLL.
        // SHA-256 of Steam ID strings — when matched, dev UI/features stay enabled; otherwise some toggles are hidden.
        private static readonly HashSet<string> _authorizedSteamIdHashes = new HashSet<string>
        {
            "Tr4eyf86yKRu6fhNQjQOrO/ZnwasQaY/fgKV0PcnoGA=" // Steam ID: 76561198082062127
        };

        private static bool _isAuthorized;
        private static string _currentSteamId;
        private static string _currentPlayerName;

        // Detected mods
        public static bool HasTheVault { get; private set; }
        public static bool HasSMUT { get; private set; }
        public static bool HasHavensBirthright { get; private set; }
        public static bool HasSenpaisChest { get; private set; }
        public static bool HasBirthdayReminder { get; private set; }
        public static bool HasSunhavenTodo { get; private set; }
        public static bool HasHavensAlmanac { get; private set; }
        public static bool HasTrinketFortune { get; private set; }

        private Harmony _harmony;
        private bool _applicationQuitting;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ConfigFile = CreateNamedConfig();
            SunhavenMods.Shared.ConfigFileHelper.ReplacePluginConfig(this, ConfigFile, Log.LogWarning);

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            try
            {
                // Initialize configuration
                ModConfig.Initialize(ConfigFile);

                // Store static config values for PersistentRunner
                StaticToggleKey = ModConfig.ToggleKey.Value;
                StaticOverlayToggleKey = ModConfig.OverlayToggleKey.Value;

                // Create persistent runner first
                CreatePersistentRunner();

                // Detect installed mods
                DetectInstalledMods();
                ModConfig.SyncTheVaultFullVaultInspectorToPlugin();

                // Initialize services
                _staticItemInspector = new ItemInspector();
                _staticCurrencyTracker = new CurrencyTracker();
                _staticBundleInspector = new BundleInspector();
                _staticRaceModifierTracker = new RaceModifierTracker();
                _staticCommandConsole = new CommandConsole();
                _staticLogViewer = new LogViewerPanel();

                // Create UI components
                CreateUIComponents();

                // Apply Harmony patches for player detection
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                LocalizationBootstrap.BindForceEnglish(ConfigFile);
                LocalizationBootstrap.Init(PluginInfo.PLUGIN_GUID, _harmony, Log);
                PatchPlayerInit();

                // Subscribe to scene changes
                SceneManager.sceneLoaded += OnSceneLoaded;

                // Check for updates
                if (ModConfig.CheckForUpdates.Value)
                {
                    VersionChecker.CheckForUpdate(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_VERSION, Log,
                        result => result.NotifyUpdateAvailable(Log));
                }

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
                Log.LogInfo($"Press {ModConfig.ToggleKey.Value} to open the debug window (requires authorization)");
                Log.LogInfo($"Press {ModConfig.OverlayToggleKey.Value} to toggle the overlay");
                Log.LogInfo($"Detected mods - TheVault: {HasTheVault}, SMUT: {HasSMUT}, Birthright: {HasHavensBirthright}, SenpaisChest: {HasSenpaisChest}, Birthday: {HasBirthdayReminder}, Todo: {HasSunhavenTodo}, Almanac: {HasHavensAlmanac}, TrinketFortune: {HasTrinketFortune}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to load {PluginInfo.PLUGIN_NAME}: {ex}");
            }
        }

        private static ConfigFile CreateNamedConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "HavenDevTools.cfg");
            string legacyPath = Path.Combine(Paths.ConfigPath, $"{PluginInfo.PLUGIN_GUID}.cfg");
            try
            {
                if (!File.Exists(configPath) && File.Exists(legacyPath))
                    File.Copy(legacyPath, configPath);
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"[Config] Migration to HavenDevTools.cfg failed: {ex.Message}");
            }
            return new ConfigFile(configPath, true);
        }

        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null && _persistentRunnerComponent != null) return;

            _persistentRunner = new GameObject("HavenDevTools_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            SceneRootSurvivor.TryRegisterPersistentRunnerGameObject(_persistentRunner);
            _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
            Log.LogInfo("[PersistentRunner] Created hidden persistent runner");
        }

        private void CreateUIComponents()
        {
            var uiObject = new GameObject("HavenDevTools_UI");
            DontDestroyOnLoad(uiObject);
            // NOTE: Do NOT use HideFlags.HideAndDontSave on UI objects!
            // That flag prevents Unity's OnGUI from being called.

            _staticDebugWindow = uiObject.AddComponent<DebugWindow>();
            _staticDebugOverlay = uiObject.AddComponent<DebugOverlay>();

            Log.LogInfo("UI components created");
        }

        /// <summary>
        /// Ensure UI components exist (recreate if destroyed by game cleanup).
        /// Called from PersistentRunner or when player initializes.
        /// </summary>
        public static void EnsureUIComponentsExist()
        {
            try
            {
                // Check if PersistentRunner was destroyed and recreate it
                if (_persistentRunner == null || _persistentRunnerComponent == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("HavenDevTools_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    SceneRootSurvivor.TryRegisterPersistentRunnerGameObject(_persistentRunner);
                    _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
                    Log?.LogInfo("[EnsureUI] PersistentRunner recreated");
                }

                // Check if UI was destroyed and recreate it
                if (_staticDebugWindow == null || _staticDebugOverlay == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating UI components...");
                    var uiObject = new GameObject("HavenDevTools_UI");
                    UnityEngine.Object.DontDestroyOnLoad(uiObject);

                    _staticDebugWindow = uiObject.AddComponent<DebugWindow>();
                    _staticDebugOverlay = uiObject.AddComponent<DebugOverlay>();

                    Log?.LogInfo("[EnsureUI] UI components recreated");
                }

                // Ensure services exist
                if (_staticItemInspector == null) _staticItemInspector = new ItemInspector();
                if (_staticCurrencyTracker == null) _staticCurrencyTracker = new CurrencyTracker();
                if (_staticBundleInspector == null) _staticBundleInspector = new BundleInspector();
                if (_staticRaceModifierTracker == null) _staticRaceModifierTracker = new RaceModifierTracker();
                if (_staticCommandConsole == null) _staticCommandConsole = new CommandConsole();
                if (_staticLogViewer == null) _staticLogViewer = new LogViewerPanel();
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error recreating components: {ex.Message}");
            }
        }

        private void DetectInstalledMods()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name == "TheVault") HasTheVault = true;
                if (name == "SunHavenMuseumUtilityTracker") HasSMUT = true;
                if (name == "HavensBirthright") HasHavensBirthright = true;
                if (name == "SenpaisChest") HasSenpaisChest = true;
                if (name == "BirthdayReminder") HasBirthdayReminder = true;
                if (name == "SunhavenTodo") HasSunhavenTodo = true;
                if (name == "HavensAlmanac") HasHavensAlmanac = true;
                if (name == "TrinketFortune") HasTrinketFortune = true;
            }

            Log.LogInfo($"Mod detection complete - TheVault: {HasTheVault}, SMUT: {HasSMUT}, Birthright: {HasHavensBirthright}, SenpaisChest: {HasSenpaisChest}, Birthday: {HasBirthdayReminder}, Todo: {HasSunhavenTodo}, Almanac: {HasHavensAlmanac}, TrinketFortune: {HasTrinketFortune}");
        }

        private void PatchPlayerInit()
        {
            try
            {
                var playerType = typeof(Wish.Player);
                var initMethod = AccessTools.Method(playerType, "InitializeAsOwner");

                if (initMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(Plugin), nameof(OnPlayerInitialized));
                    _harmony.Patch(initMethod, postfix: new HarmonyMethod(postfix));
                    Log.LogInfo("Patched Player.InitializeAsOwner");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch player init: {ex.Message}");
            }
        }

        private static void OnPlayerInitialized()
        {
            Log?.LogInfo("[HavenDevTools] Player initialized, ensuring UI exists and checking authorization...");
            EnsureUIComponentsExist();
            CheckAuthorization();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogDebug($"[SceneChange] Scene loaded: '{scene.name}'");

            string sceneLower = scene.name.ToLowerInvariant();
            if (sceneLower.Contains("menu") || sceneLower.Contains("title"))
            {
                Log.LogDebug($"Menu scene detected: {scene.name}");
                // Reset authorization state on return to menu
                _isAuthorized = false;
                _currentPlayerName = null;
            }
        }

        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            string sceneLower = sceneName.ToLowerInvariant();
            bool expectedTeardown = _applicationQuitting || !Application.isPlaying || sceneLower.Contains("menu") || sceneLower.Contains("title");
            if (expectedTeardown)
                Log?.LogInfo($"[Lifecycle] Plugin OnDestroy during expected teardown (scene: {sceneName})");
            else
                Log?.LogWarning($"[Lifecycle] Plugin OnDestroy outside expected teardown (scene: {sceneName})");

            // NOTE: Do NOT call _harmony?.UnpatchSelf() here!
            // Patches must survive plugin destruction so OnPlayerInitialized
            // can trigger EnsureUIComponentsExist() on character reload.
        }

        #region Authorization

        private static void CheckAuthorization()
        {
            try
            {
                // Get player name
                try
                {
                    if (Wish.Player.Instance != null)
                    {
                        var playerNameProp = Wish.Player.Instance.GetType().GetProperty("PlayerName",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (playerNameProp != null)
                        {
                            _currentPlayerName = playerNameProp.GetValue(Wish.Player.Instance) as string;
                        }

                        if (string.IsNullOrEmpty(_currentPlayerName))
                        {
                            _currentPlayerName = Wish.Player.Instance.name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogDebug($"[HavenDevTools] Player name fallback: {ex.Message}");
                    _currentPlayerName = "Unknown";
                }

                // Steam ID authorization
                _currentSteamId = TryGetSteamId();

                if (!string.IsNullOrEmpty(_currentSteamId))
                {
                    string steamHash = ComputeHash(_currentSteamId);
                    if (_authorizedSteamIdHashes.Contains(steamHash))
                    {
                        Log?.LogInfo("[HavenDevTools] Authorized via Steam ID");
                        _isAuthorized = true;
                        return;
                    }
                    Log?.LogInfo($"[HavenDevTools] Steam ID hash not authorized: {steamHash}");
                }
                else
                {
                    Log?.LogInfo("[HavenDevTools] Could not retrieve Steam ID");
                }

                Log?.LogInfo("[HavenDevTools] Not authorized - Steam ID required");
            }
            catch (Exception ex)
            {
                Log?.LogError($"[HavenDevTools] Authorization error: {ex.Message}");
            }
        }

        private static string TryGetSteamId()
        {
            try
            {
                Assembly steamAssembly = null;

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string name = assembly.GetName().Name;
                    if (name.Contains("rlabrecque") || name == "Steamworks.NET")
                    {
                        steamAssembly = assembly;
                        break;
                    }
                }

                if (steamAssembly == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var steamUserType = assembly.GetType("Steamworks.SteamUser");
                            if (steamUserType != null)
                            {
                                steamAssembly = assembly;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log?.LogDebug($"[HavenDevTools] TryGetSteamId assembly search: {ex.Message}");
                        }
                    }
                }

                if (steamAssembly == null) return null;

                var steamUserType2 = steamAssembly.GetType("Steamworks.SteamUser");
                if (steamUserType2 == null) return null;

                var getSteamIdMethod = steamUserType2.GetMethod("GetSteamID", BindingFlags.Public | BindingFlags.Static);
                if (getSteamIdMethod == null) return null;

                var steamId = getSteamIdMethod.Invoke(null, null);
                if (steamId == null) return null;

                string steamIdStr = steamId.ToString();
                if (string.IsNullOrEmpty(steamIdStr) || steamIdStr == "0" || steamIdStr.Length < 10)
                    return null;

                return steamIdStr;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[HavenDevTools] TryGetSteamId: {ex.Message}");
                return null;
            }
        }

        private static string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }

        public static string GenerateHash(string input) => ComputeHash(input);

        #endregion

        #region Public API

        public static bool IsAuthorized => _isAuthorized;
        public static string CurrentPlayerName => _currentPlayerName;

        public static ItemInspector GetItemInspector() => _staticItemInspector;
        public static CurrencyTracker GetCurrencyTracker() => _staticCurrencyTracker;
        public static BundleInspector GetBundleInspector() => _staticBundleInspector;
        public static RaceModifierTracker GetRaceModifierTracker() => _staticRaceModifierTracker;

        public static DebugWindow GetDebugWindow() => _staticDebugWindow;
        public static DebugOverlay GetDebugOverlay() => _staticDebugOverlay;
        public static CommandConsole GetCommandConsole() => _staticCommandConsole;
        public static LogViewerPanel GetLogViewer() => _staticLogViewer;

        public static void ToggleDebugWindow()
        {
            if (_isAuthorized)
            {
                _staticDebugWindow?.Toggle();
            }
            else
            {
                CheckAuthorization();
                if (_isAuthorized)
                {
                    _staticDebugWindow?.Toggle();
                }
            }
        }

        public static void ToggleDebugOverlay()
        {
            if (_isAuthorized)
            {
                _staticDebugOverlay?.Toggle();
            }
        }

        #endregion
    }

    /// <summary>
    /// Persistent runner that survives game cleanup.
    /// Uses HideFlags.HideAndDontSave to avoid being destroyed by game's UIHandler.UnloadGame.
    /// </summary>
    public class PersistentRunner : MonoBehaviour
    {
        private float _heartbeatTimer = 0f;
        private int _heartbeatCount = 0;
        private const float HEARTBEAT_INTERVAL = 30f;
        private bool _syncedTheVaultInspectorOnce;

        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            Plugin.Log?.LogInfo("[PersistentRunner] Awake - hidden persistent runner active");
        }

        private void Update()
        {
            // HavenDevTools may load before TheVault; push inspector flag once all assemblies are in.
            if (!_syncedTheVaultInspectorOnce)
            {
                _syncedTheVaultInspectorOnce = true;
                ModConfig.SyncTheVaultFullVaultInspectorToPlugin();
            }

            CheckHotkeys();

            // Heartbeat - prove the runner is still alive
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _heartbeatTimer = 0f;
                _heartbeatCount++;
                Plugin.Log?.LogInfo($"[PersistentRunner Heartbeat #{_heartbeatCount}] Authorized: {Plugin.IsAuthorized}, Player: {Plugin.CurrentPlayerName ?? "none"}");
            }
        }

        private void CheckHotkeys()
        {
            try
            {
                if (TextInputFocusGuard.ShouldDeferModHotkeys(Plugin.Log))
                    return;

                // Check for debug window toggle key
                if (Input.GetKeyDown(Plugin.StaticToggleKey))
                {
                    Plugin.ToggleDebugWindow();
                }

                // Check for overlay toggle key
                if (Input.GetKeyDown(Plugin.StaticOverlayToggleKey))
                {
                    Plugin.ToggleDebugOverlay();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[PersistentRunner] Hotkey error: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
            string sceneLower = sceneName.ToLowerInvariant();
            bool expectedTeardown = !Application.isPlaying || sceneLower.Contains("menu") || sceneLower.Contains("title");
            if (expectedTeardown)
            {
                Plugin.Log?.LogInfo("[PersistentRunner] OnDestroy during app quit/menu unload (expected).");
            }
            else
            {
                Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy outside quit/menu (unexpected).");
            }
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.havendevtools";
        public const string PLUGIN_NAME = "Haven Dev Tools";
        public const string PLUGIN_VERSION = "2.0.1";
    }
}
