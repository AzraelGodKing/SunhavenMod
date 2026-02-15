using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BirthdayReminder.Data;
using BirthdayReminder.Integration;
using BirthdayReminder.UI;
using HarmonyLib;
using SunhavenMods.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BirthdayReminder
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.azraelgodking.sunhaventodo", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        // Static references
        private static BirthdayManager _staticManager;
        private static BirthdayHUD _staticHUD;
        private static GameObject _hudObject;
        private static float _staticHUDPositionX = -1f;
        private static float _staticHUDPositionY = -1f;

        // Cross-mod integration
        private static TodoIntegration _todoIntegration;

        // Configuration
        private ConfigEntry<bool> _enabled;
        private ConfigEntry<float> _hudPositionX;
        private ConfigEntry<float> _hudPositionY;
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _showGiftHints;
        private ConfigEntry<bool> _useNativeNotifications;
        private ConfigEntry<bool> _debugMode;
        private ConfigEntry<bool> _checkForUpdates;

        // Character tracking for save switching
        private static string _currentCharacterName;
        private static bool _isCharacterLoaded;

        // Instance references
        private BirthdayManager _manager;
        private BirthdayHUD _hud;
        private Harmony _harmony;

        // Hook tracking - reset when returning to main menu
        private static bool _overnightHooked = false;
        private static UnityAction _overnightCallback;

        // Persistent runner for hotkey handling across scene transitions
        private static GameObject _persistentRunner;
        private static PersistentRunner _persistentRunnerComponent;

        // Static config values for PersistentRunner access
        private static KeyCode _staticToggleKey = KeyCode.B;
        private static bool _staticDebugMode = false;
        private static bool _staticUseNativeNotifications = true;

        // Cached reflection for native notifications
        private static Type _notificationStackType;
        private static PropertyInfo _notificationStackInstance;
        private static MethodInfo _sendNotificationMethod;
        private static bool _notificationSystemInitialized = false;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            BindConfiguration();
            CreatePersistentRunner();
            InitializeManager();
            ApplyPatches();
            InitializeIntegrations();

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
            _enabled = Config.Bind(
                "General",
                "Enabled",
                true,
                "Enable birthday reminders"
            );

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

            _toggleKey = Config.Bind(
                "Hotkeys",
                "ToggleKey",
                KeyCode.B,
                "Key to toggle the birthday HUD (with Ctrl)"
            );
            _staticToggleKey = _toggleKey.Value;
            _toggleKey.SettingChanged += (_, _) => _staticToggleKey = _toggleKey.Value;

            _showGiftHints = Config.Bind(
                "General",
                "ShowGiftHints",
                true,
                "Show gift preferences in the birthday reminder"
            );

            _useNativeNotifications = Config.Bind(
                "General",
                "UseNativeNotifications",
                true,
                "Show birthday notifications using the game's native notification system"
            );

            _debugMode = Config.Bind(
                "Debug",
                "DebugMode",
                false,
                "Enable debug mode for testing (Ctrl+Shift+B to add test birthday)"
            );
            _staticDebugMode = _debugMode.Value;
            _debugMode.SettingChanged += (_, _) => _staticDebugMode = _debugMode.Value;

            _staticUseNativeNotifications = _useNativeNotifications.Value;
            _useNativeNotifications.SettingChanged += (_, _) => _staticUseNativeNotifications = _useNativeNotifications.Value;

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

            _persistentRunner = new GameObject("BirthdayReminder_PersistentRunner");
            DontDestroyOnLoad(_persistentRunner);
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
            _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
            Log.LogInfo("[PersistentRunner] Created");
        }

        private void InitializeManager()
        {
            _manager = new BirthdayManager();
            _staticManager = _manager;
        }

        private void InitializeIntegrations()
        {
            try
            {
                // Check if SunhavenTodo is loaded
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.azraelgodking.sunhaventodo"))
                {
                    _todoIntegration = new TodoIntegration(_staticManager);
                }
                else
                {
                    Log.LogInfo("[Integrations] SunhavenTodo not found - birthday todos disabled");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[Integrations] Error initializing: {ex.Message}");
            }
        }

        private void ApplyPatches()
        {
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);

            try
            {
                // Patch player initialization
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

                // Patch gifting to track birthday gifts
                var npcAIType = AccessTools.TypeByName("Wish.NPCAI");
                if (npcAIType != null)
                {
                    // Specify parameter type to find the correct overload
                    var addFriendshipMethod = AccessTools.Method(npcAIType, "AddFriendship", new[] { typeof(int) });
                    if (addFriendshipMethod != null)
                    {
                        var patchMethod = AccessTools.Method(typeof(GiftPatches), nameof(GiftPatches.OnAddFriendship));
                        _harmony.Patch(addFriendshipMethod, postfix: new HarmonyMethod(patchMethod));
                        Log.LogInfo("Applied gift tracking patch");
                    }
                    else
                    {
                        Log.LogWarning("Could not find NPCAI.AddFriendship(int) method - gift tracking will not work");
                    }
                }
                else
                {
                    Log.LogWarning("Could not find NPCAI type - gift tracking will not work");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to apply patches: {ex.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Log.LogInfo($"[BirthdayReminder] Scene loaded: {scene.name}");

            // Check for specific main menu scene (Sun Haven uses "MainMenu")
            if (scene.name == "MainMenu" || scene.name == "Bootstrap")
            {
                Log.LogInfo("[BirthdayReminder] Main menu detected - hiding HUD and resetting state");
                _staticHUD?.Hide();
                _isCharacterLoaded = false;
                _currentCharacterName = null;
                return;
            }

            EnsureUIComponentsExist();
            TryHookOvernightEvent();
        }

        /// <summary>
        /// Reset the overnight hook so it can be re-established (call when returning from main menu)
        /// </summary>
        public static void ResetOvernightHook()
        {
            _overnightHooked = false;
            _overnightCallback = null;
            Log?.LogInfo("[BirthdayReminder] Overnight hook reset");
        }

        /// <summary>
        /// Try to hook into UIHandler.OnCompleteOvernight event and DayCycle.OnDayStart
        /// </summary>
        public static void TryHookOvernightEvent()
        {
            if (_overnightHooked) return;

            try
            {
                // Try to hook DayCycle.OnDayStart (most reliable)
                var dayCycleType = AccessTools.TypeByName("Wish.DayCycle");
                if (dayCycleType != null)
                {
                    var onDayStartField = AccessTools.Field(dayCycleType, "OnDayStart");
                    if (onDayStartField != null)
                    {
                        var currentAction = onDayStartField.GetValue(null) as UnityAction;
                        _overnightCallback = OnOvernightComplete;

                        if (currentAction != null)
                        {
                            currentAction += _overnightCallback;
                            onDayStartField.SetValue(null, currentAction);
                        }
                        else
                        {
                            onDayStartField.SetValue(null, _overnightCallback);
                        }

                        _overnightHooked = true;
                        Log?.LogInfo("Hooked into DayCycle.OnDayStart event");
                        return;
                    }
                }

                // Fallback: Try UIHandler.OnCompleteOvernight
                var uiHandlerType = AccessTools.TypeByName("Wish.UIHandler");
                if (uiHandlerType == null) return;

                var uiHandler = GetSingletonInstance(uiHandlerType);

                if (uiHandler == null) return;

                // Get the OnCompleteOvernight event
                var overnightField = AccessTools.Field(uiHandlerType, "OnCompleteOvernight");
                if (overnightField != null)
                {
                    var currentAction = overnightField.GetValue(uiHandler) as UnityAction;

                    _overnightCallback = OnOvernightComplete;

                    // Combine with existing action
                    if (currentAction != null)
                    {
                        currentAction += _overnightCallback;
                        overnightField.SetValue(uiHandler, currentAction);
                    }
                    else
                    {
                        overnightField.SetValue(uiHandler, _overnightCallback);
                    }

                    _overnightHooked = true;
                    Log?.LogInfo("Hooked into OnCompleteOvernight event");
                }
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"Failed to hook overnight event: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the overnight transition completes (wake up)
        /// </summary>
        private static void OnOvernightComplete()
        {
            Log?.LogInfo("[BirthdayReminder] Day started - checking for birthdays");

            // Check for birthdays
            _staticManager?.CheckTodaysBirthdays();

            // Show HUD if there are birthdays
            if (_staticManager != null && _staticManager.HasBirthdays)
            {
                EnsureUIComponentsExist();
                _staticHUD?.Show();

                // Send native game notifications for each birthday
                SendAllBirthdayNotifications();
            }
        }

        public static void EnsureUIComponentsExist()
        {
            try
            {
                // Recreate PersistentRunner if destroyed
                if (_persistentRunner == null || _persistentRunnerComponent == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("BirthdayReminder_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    _persistentRunnerComponent = _persistentRunner.AddComponent<PersistentRunner>();
                    Log?.LogInfo("[EnsureUI] PersistentRunner recreated");
                }

                if (_staticHUD == null)
                {
                    Log?.LogInfo("[EnsureUI] Creating BirthdayHUD...");
                    _hudObject = new GameObject("BirthdayReminder_HUD");
                    UnityEngine.Object.DontDestroyOnLoad(_hudObject);

                    _staticHUD = _hudObject.AddComponent<BirthdayHUD>();
                    _staticHUD.Initialize(_staticManager);

                    // Restore saved position if valid
                    if (_staticHUDPositionX >= 0 && _staticHUDPositionY >= 0)
                    {
                        _staticHUD.SetPosition(_staticHUDPositionX, _staticHUDPositionY);
                    }

                    // Wire up position persistence
                    _staticHUD.OnPositionChanged = (x, y) =>
                    {
                        _staticHUDPositionX = x;
                        _staticHUDPositionY = y;
                        Instance?._hudPositionX?.SetSerializedValue(x.ToString());
                        Instance?._hudPositionY?.SetSerializedValue(y.ToString());
                    };

                    Log?.LogInfo("[EnsureUI] BirthdayHUD created");
                }

                if (Instance != null)
                {
                    Instance._hud = _staticHUD;
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error: {ex.Message}");
            }
        }

        private void Update()
        {
            // Hotkey handling
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            // Toggle HUD: Ctrl+B
            if (ctrlPressed && !shiftPressed && !altPressed && Input.GetKeyDown(_toggleKey.Value))
            {
                Log.LogInfo("[Hotkey] Ctrl+B pressed - toggling HUD");
                EnsureUIComponentsExist();
                _staticHUD?.Toggle();
                Log.LogInfo($"[Hotkey] HUD visible: {_staticHUD?.IsVisible ?? false}, Birthdays: {_staticManager?.TodaysBirthdays?.Count ?? 0}");
            }

            // Test Mode: Ctrl+Alt+B - Add test birthday (works without debug mode for easy testing)
            if (ctrlPressed && altPressed && !shiftPressed && Input.GetKeyDown(_toggleKey.Value))
            {
                Log.LogInfo("[TEST] Ctrl+Alt+B pressed - adding test birthday");
                DebugAddTestBirthday();
            }

            // Test Mode: Ctrl+Alt+A - Load ALL birthdays (works without debug mode)
            if (ctrlPressed && altPressed && !shiftPressed && Input.GetKeyDown(KeyCode.A))
            {
                Log.LogInfo("[TEST] Ctrl+Alt+A pressed - loading all birthdays");
                DebugLoadAllBirthdays();
            }

            // Debug: Ctrl+Shift+B - Add test birthday and show HUD
            if (_debugMode.Value && ctrlPressed && shiftPressed && Input.GetKeyDown(_toggleKey.Value))
            {
                DebugAddTestBirthday();
            }

            // Debug: Ctrl+Shift+R - Refresh/recheck birthdays
            if (_debugMode.Value && ctrlPressed && shiftPressed && Input.GetKeyDown(KeyCode.R))
            {
                DebugRefreshBirthdays();
            }

            // Debug: Ctrl+Shift+C - Clear all birthdays
            if (_debugMode.Value && ctrlPressed && shiftPressed && Input.GetKeyDown(KeyCode.C))
            {
                DebugClearBirthdays();
            }

            // Debug: Ctrl+Shift+A - Load ALL birthdays (show all NPCs with birthdays)
            if (_debugMode.Value && ctrlPressed && shiftPressed && Input.GetKeyDown(KeyCode.A))
            {
                DebugLoadAllBirthdays();
            }

            // Debug: Ctrl+Shift+L - Dump Lynn's NPC info
            if (_debugMode.Value && ctrlPressed && shiftPressed && Input.GetKeyDown(KeyCode.L))
            {
                Log.LogInfo("[DEBUG] Ctrl+Shift+L pressed - dumping Lynn's NPC info...");
                _staticManager?.DebugDumpNPCInfo("Lynn");
            }
        }

        public static BirthdayManager GetManager() => _staticManager;
        public static BirthdayHUD GetHUD() => _staticHUD;
        public static TodoIntegration GetTodoIntegration() => _todoIntegration;
        public static KeyCode StaticToggleKey => _staticToggleKey;
        public static bool StaticDebugMode => _staticDebugMode;
        public static bool StaticUseNativeNotifications => _staticUseNativeNotifications;

        /// <summary>
        /// Initialize the native notification system via reflection
        /// </summary>
        private static void InitializeNotificationSystem()
        {
            if (_notificationSystemInitialized) return;
            _notificationSystemInitialized = true;

            try
            {
                // Get NotificationStack singleton
                _notificationStackType = AccessTools.TypeByName("Wish.NotificationStack");
                if (_notificationStackType == null)
                {
                    Log?.LogDebug("[Notifications] NotificationStack type not found");
                    return;
                }

                // Get SingletonBehaviour<NotificationStack>.Instance
                var singletonType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonType != null)
                {
                    var genericType = singletonType.MakeGenericType(_notificationStackType);
                    _notificationStackInstance = genericType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                }

                // Get SendNotification method - signature: SendNotification(string, int, int, bool, bool)
                _sendNotificationMethod = AccessTools.Method(_notificationStackType, "SendNotification",
                    new[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) });

                if (_sendNotificationMethod != null)
                {
                    Log?.LogInfo("[Notifications] Native notification system initialized");
                }
                else
                {
                    Log?.LogDebug("[Notifications] SendNotification method not found");
                }
            }
            catch (Exception ex)
            {
                Log?.LogDebug($"[Notifications] Error initializing: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a native game notification for a birthday
        /// </summary>
        /// <param name="npcName">Name of the NPC</param>
        /// <param name="itemId">Optional item ID to show (0 for default)</param>
        public static void SendBirthdayNotification(string npcName, int itemId = 0)
        {
            if (!_staticUseNativeNotifications) return;

            InitializeNotificationSystem();

            if (_sendNotificationMethod == null || _notificationStackInstance == null)
            {
                Log?.LogDebug("[Notifications] Cannot send - system not available");
                return;
            }

            try
            {
                var instance = _notificationStackInstance.GetValue(null);
                if (instance == null)
                {
                    Log?.LogDebug("[Notifications] NotificationStack instance is null");
                    return;
                }

                // Send notification: (title, itemId, amount, showInChat, playSound)
                string message = $"It's {npcName}'s birthday today!";
                _sendNotificationMethod.Invoke(instance, new object[] { message, itemId, 1, false, true });
                Log?.LogInfo($"[Notifications] Sent birthday notification for {npcName}");
            }
            catch (Exception ex)
            {
                Log?.LogDebug($"[Notifications] Error sending notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Send birthday notifications for all NPCs with birthdays today
        /// </summary>
        public static void SendAllBirthdayNotifications()
        {
            if (!_staticUseNativeNotifications) return;
            if (_staticManager == null || !_staticManager.HasBirthdays) return;

            foreach (var birthday in _staticManager.TodaysBirthdays)
            {
                if (!birthday.HasBeenGifted)
                {
                    SendBirthdayNotification(birthday.NPCName);
                }
            }
        }

        /// <summary>
        /// Get singleton instance using reflection
        /// </summary>
        private static object GetSingletonInstance(Type targetType)
        {
            try
            {
                // Try to find SingletonBehaviour<T> base class
                var singletonBaseType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonBaseType != null)
                {
                    var genericType = singletonBaseType.MakeGenericType(targetType);
                    var instanceProp = AccessTools.Property(genericType, "Instance");
                    return instanceProp?.GetValue(null);
                }

                // Alternative: look for static Instance property directly
                var directInstanceProp = AccessTools.Property(targetType, "Instance");
                return directInstanceProp?.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Manually trigger birthday check (for testing)
        /// </summary>
        public static void CheckBirthdays()
        {
            _staticManager?.CheckTodaysBirthdays();
        }

        /// <summary>
        /// Called when character changes (save switch)
        /// </summary>
        public static void OnCharacterChanged(string newCharacterName)
        {
            if (_currentCharacterName != newCharacterName)
            {
                Log?.LogInfo($"[BirthdayReminder] Character changed: {_currentCharacterName ?? "None"} -> {newCharacterName}");
                _currentCharacterName = newCharacterName;

                // Reset birthday tracking for new character
                _staticManager?.ResetForNewCharacter(newCharacterName);

                // Reset cross-mod integration tracking
                _todoIntegration?.Reset();

                // Hide HUD until we check birthdays
                _staticHUD?.Hide();

                _isCharacterLoaded = true;
            }
        }

        public static string CurrentCharacter => _currentCharacterName;
        public static bool IsCharacterLoaded => _isCharacterLoaded;

        #region Debug Methods

        /// <summary>
        /// Debug: Add a test birthday to verify HUD works
        /// </summary>
        private void DebugAddTestBirthday()
        {
            Log.LogInfo("[DEBUG] Adding test birthday...");

            EnsureUIComponentsExist();
            _staticManager?.DebugAddTestBirthday("Test NPC", "Loves: Diamonds, Gold");

            if (_staticManager != null && _staticManager.HasBirthdays)
            {
                _staticHUD?.Show();
                Log.LogInfo("[DEBUG] Test birthday added and HUD shown");
            }
        }

        /// <summary>
        /// Debug: Refresh birthday check
        /// </summary>
        private void DebugRefreshBirthdays()
        {
            Log.LogInfo("[DEBUG] Ctrl+Shift+R pressed - Manual refresh triggered");
            EnsureUIComponentsExist();

            // Use ManualRefresh which sets a status message
            _staticManager?.ManualRefresh();

            Log.LogInfo($"[DEBUG] Found {_staticManager?.TodaysBirthdays.Count ?? 0} birthdays");

            // Always show the HUD on manual refresh
            _staticHUD?.Show();
        }

        /// <summary>
        /// Debug: Clear all birthdays
        /// </summary>
        private void DebugClearBirthdays()
        {
            Log.LogInfo("[DEBUG] Clearing all birthdays...");
            _staticManager?.DebugClearBirthdays();
            _staticHUD?.Hide();
            Log.LogInfo("[DEBUG] Birthdays cleared and HUD hidden");
        }

        /// <summary>
        /// Debug: Load ALL NPC birthdays
        /// </summary>
        private void DebugLoadAllBirthdays()
        {
            Log.LogInfo("[DEBUG] Loading all NPC birthdays...");
            EnsureUIComponentsExist();
            _staticManager?.DebugLoadAllBirthdays();

            if (_staticManager != null && _staticManager.HasBirthdays)
            {
                _staticHUD?.Show();
                Log.LogInfo($"[DEBUG] Loaded {_staticManager.TodaysBirthdays.Count} birthdays");
            }
        }

        #endregion

        private void OnDestroy()
        {
            Log.LogWarning("[CRITICAL] Plugin OnDestroy called!");
        }
    }

    /// <summary>
    /// Player patches for initialization
    /// </summary>
    public static class PlayerPatches
    {
        private static string _lastCharacterName;

        public static void OnPlayerInitialized(object __instance)
        {
            try
            {
                Plugin.Log?.LogInfo("[PlayerPatches] Player initialized - checking character...");

                // Get character name
                string characterName = GetCharacterName(__instance);

                // Detect character change (save switch) or returning from main menu
                if (_lastCharacterName != characterName)
                {
                    Plugin.Log?.LogInfo($"[PlayerPatches] Character: {characterName}");
                    Plugin.OnCharacterChanged(characterName);
                    _lastCharacterName = characterName;
                }

                // Ensure UI components exist (recreates if destroyed)
                Plugin.EnsureUIComponentsExist();

                // Reset and re-hook the overnight event (it gets lost when returning from main menu)
                Plugin.ResetOvernightHook();
                Plugin.TryHookOvernightEvent();

                // Check birthdays when player loads in
                Plugin.GetManager()?.CheckTodaysBirthdays();

                // Show HUD if birthdays found
                var manager = Plugin.GetManager();
                if (manager != null && manager.HasBirthdays)
                {
                    Plugin.Log?.LogInfo($"[PlayerPatches] Found {manager.TodaysBirthdays.Count} birthdays - showing HUD");
                    Plugin.GetHUD()?.Show();
                }
                else
                {
                    Plugin.Log?.LogInfo("[PlayerPatches] No birthdays found today - HUD will not show automatically");
                    Plugin.Log?.LogInfo("[PlayerPatches] Use Ctrl+Alt+B to add a test birthday for testing");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        private static string GetCharacterName(object player)
        {
            try
            {
                // Try to get from GameSave
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                if (gameSaveType != null)
                {
                    var singletonBaseType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                    if (singletonBaseType != null)
                    {
                        var genericType = singletonBaseType.MakeGenericType(gameSaveType);
                        var instanceProp = AccessTools.Property(genericType, "Instance");
                        var gameSave = instanceProp?.GetValue(null);

                        if (gameSave != null)
                        {
                            var currentSaveProp = AccessTools.Property(gameSaveType, "CurrentSave");
                            var currentSave = currentSaveProp?.GetValue(gameSave);

                            if (currentSave != null)
                            {
                                var charDataProp = AccessTools.Property(currentSave.GetType(), "characterData");
                                var charData = charDataProp?.GetValue(currentSave);

                                if (charData != null)
                                {
                                    var nameProp = AccessTools.Property(charData.GetType(), "characterName");
                                    var name = nameProp?.GetValue(charData) as string;
                                    if (!string.IsNullOrEmpty(name))
                                        return name;
                                }
                            }
                        }
                    }
                }

                // Fallback: try player name
                if (player != null)
                {
                    var nameProp = AccessTools.Property(player.GetType(), "playerName");
                    var name = nameProp?.GetValue(player) as string;
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Failed to get character name: {ex.Message}");
            }

            return "Unknown";
        }
    }

    /// <summary>
    /// Gift tracking patches
    /// </summary>
    public static class GiftPatches
    {
        /// <summary>
        /// Track when friendship is added (gift given)
        /// </summary>
        public static void OnAddFriendship(object __instance, int __0)
        {
            try
            {
                // __instance is the NPCAI
                // __0 is the friendship amount

                // Only track if it's a significant gift (birthday bonus would be higher)
                if (__0 <= 0) return;

                // Get NPC name
                var npcType = __instance.GetType();
                var nameProp = AccessTools.Property(npcType, "NPCName")
                               ?? AccessTools.Property(npcType, "npcName")
                               ?? AccessTools.Property(npcType, "Name");

                string npcName = nameProp?.GetValue(__instance)?.ToString();

                if (!string.IsNullOrEmpty(npcName))
                {
                    var manager = Plugin.GetManager();
                    if (manager != null && manager.HasBirthdays)
                    {
                        // Check if this NPC has a birthday today
                        var birthdayInfo = manager.TodaysBirthdays.Find(b => b.NPCName == npcName);
                        if (birthdayInfo != null && !birthdayInfo.HasBeenGifted)
                        {
                            manager.MarkGifted(npcName);
                            Plugin.Log?.LogInfo($"[BirthdayReminder] Marked {npcName} as gifted on their birthday!");

                            // Directly notify the Todo integration to complete the birthday todo
                            Plugin.GetTodoIntegration()?.OnGiftGiven(npcName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"Error tracking gift: {ex.Message}");
            }
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
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            // Toggle HUD: Ctrl+B
            if (ctrlPressed && !shiftPressed && !altPressed && Input.GetKeyDown(Plugin.StaticToggleKey))
            {
                Plugin.Log?.LogInfo("[PersistentRunner] Ctrl+B pressed - toggling HUD");
                Plugin.EnsureUIComponentsExist();
                Plugin.GetHUD()?.Toggle();
            }

            // Test Mode: Ctrl+Alt+B - Add test birthday
            if (ctrlPressed && altPressed && !shiftPressed && Input.GetKeyDown(Plugin.StaticToggleKey))
            {
                Plugin.Log?.LogInfo("[PersistentRunner] Ctrl+Alt+B pressed - adding test birthday");
                Plugin.EnsureUIComponentsExist();
                Plugin.GetManager()?.DebugAddTestBirthday("Test NPC", "Loves: Diamonds, Gold");
                var hud = Plugin.GetHUD();
                if (Plugin.GetManager()?.HasBirthdays == true && hud != null)
                {
                    Plugin.Log?.LogInfo($"[PersistentRunner] Showing HUD - birthdays: {Plugin.GetManager()?.TodaysBirthdays?.Count}");
                    hud.Show();
                    Plugin.Log?.LogInfo($"[PersistentRunner] HUD visible: {hud.IsVisible}");
                }
                else
                {
                    Plugin.Log?.LogWarning($"[PersistentRunner] Cannot show HUD - manager: {Plugin.GetManager() != null}, hud: {hud != null}");
                }
            }

            // Debug: Ctrl+Shift+R - Refresh/recheck birthdays
            if (Plugin.StaticDebugMode && ctrlPressed && shiftPressed && Input.GetKeyDown(KeyCode.R))
            {
                Plugin.Log?.LogInfo("[PersistentRunner] Ctrl+Shift+R pressed - Manual refresh");
                Plugin.EnsureUIComponentsExist();
                Plugin.GetManager()?.ManualRefresh();
                Plugin.GetHUD()?.Show();
            }

            // Debug: Ctrl+Shift+C - Clear all birthdays
            if (Plugin.StaticDebugMode && ctrlPressed && shiftPressed && Input.GetKeyDown(KeyCode.C))
            {
                Plugin.Log?.LogInfo("[PersistentRunner] Ctrl+Shift+C pressed - Clearing birthdays");
                Plugin.GetManager()?.DebugClearBirthdays();
                Plugin.GetHUD()?.Hide();
            }
        }

        private void OnDestroy()
        {
            Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy called - this should NOT happen!");
        }
    }
}
