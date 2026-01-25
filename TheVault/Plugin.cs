using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using TheVault.DebugTools;
using TheVault.Patches;
using TheVault.UI;
using TheVault.Vault;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheVault
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static ConfigFile ConfigFile { get; private set; }

        // Static references that survive Plugin destruction
        // (Unity's null-conditional returns null for destroyed MonoBehaviours)
        private static VaultManager _staticVaultManager;
        private static VaultSaveSystem _staticSaveSystem;
        private static VaultUI _staticVaultUI;
        private static VaultHUD _staticVaultHUD;

        // Static config values for PersistentRunner to use for hotkey detection
        internal static KeyCode StaticToggleKey = KeyCode.V;
        internal static bool StaticRequireCtrl = true;
        internal static KeyCode StaticAltToggleKey = KeyCode.F8;
        internal static KeyCode StaticHUDToggleKey = KeyCode.F7;

        private Harmony _harmony;
        private VaultManager _vaultManager;
        private VaultSaveSystem _saveSystem;
        private VaultUI _vaultUI;
        private VaultHUD _vaultHUD;
        private DebugMode _debugMode;

        // Configuration
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _requireCtrlModifier;
        private ConfigEntry<KeyCode> _altToggleKey;
        private ConfigEntry<bool> _enableHUD;
        private ConfigEntry<string> _hudPosition;
        private ConfigEntry<KeyCode> _hudToggleKey;
        private ConfigEntry<bool> _enableAutoSave;
        private ConfigEntry<float> _autoSaveInterval;

        // Backup menu detection via polling (in case SceneManager.sceneLoaded stops working)
        private string _lastKnownScene = "";
        private bool _wasInMenuScene = true; // Start as true since game starts at menu
        private float _sceneCheckTimer = 0f;
        private const float SCENE_CHECK_INTERVAL = 0.5f; // Check every 0.5 seconds

        // Heartbeat for debugging - proves plugin is still running
        private float _heartbeatTimer = 0f;
        private const float HEARTBEAT_INTERVAL = 30f; // Log every 30 seconds to prove plugin is alive
        private int _heartbeatCount = 0;

        // Separate persistent object that survives game's UIHandler.UnloadGame cleanup
        private static GameObject _persistentRunner;
        private static PersistentUpdateRunner _updateRunner;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ConfigFile = Config;

            // NOTE: DontDestroyOnLoad on this gameObject doesn't help because
            // the game's UIHandler.UnloadGame explicitly destroys UI objects.
            // We use a separate hidden persistent runner instead.

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            // Create a hidden persistent runner that survives the game's cleanup
            CreatePersistentRunner();

            try
            {
                // Initialize configuration
                InitializeConfig();

                // Initialize vault system
                // Store in both instance and static fields so they survive Plugin destruction
                _vaultManager = new VaultManager();
                _saveSystem = new VaultSaveSystem(_vaultManager);
                _staticVaultManager = _vaultManager;
                _staticSaveSystem = _saveSystem;

                // Create UI GameObject
                var uiObject = new GameObject("TheVault_UI");
                DontDestroyOnLoad(uiObject);
                _vaultUI = uiObject.AddComponent<VaultUI>();
                _vaultUI.Initialize(_vaultManager);
                _vaultUI.SetToggleKey(_toggleKey.Value, _requireCtrlModifier.Value);
                _vaultUI.SetAltToggleKey(_altToggleKey.Value);
                _staticVaultUI = _vaultUI;

                // Store config values for PersistentRunner hotkey detection
                StaticToggleKey = _toggleKey.Value;
                StaticRequireCtrl = _requireCtrlModifier.Value;
                StaticAltToggleKey = _altToggleKey.Value;
                StaticHUDToggleKey = _hudToggleKey.Value;

                // Create HUD for persistent display
                _vaultHUD = uiObject.AddComponent<VaultHUD>();
                _vaultHUD.Initialize(_vaultManager);
                _vaultHUD.SetEnabled(_enableHUD.Value);
                _vaultHUD.SetPosition(ParseHUDPosition(_hudPosition.Value));
                _staticVaultHUD = _vaultHUD;

                // Initialize icon cache for UI icons
                IconCache.Initialize();

                // Create Debug Mode (only activates for authorized users)
                Log.LogInfo("Adding DebugMode component...");
                _debugMode = uiObject.AddComponent<DebugMode>();
                Log.LogInfo($"DebugMode component added: {_debugMode != null}");

                // Register item-to-currency mappings for deposit/withdraw
                RegisterItemMappings();

                // Apply Harmony patches
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                ApplyPatches();

                // Patch GameSave class for character loading detection
                PatchGameSave();

                // Subscribe to scene loading as a backup trigger for vault loading
                // This is more reliable than patching game-specific methods that may not exist
                SceneManager.sceneLoaded += OnSceneLoaded;
                Log.LogInfo("Subscribed to SceneManager.sceneLoaded for vault loading");

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
                Log.LogInfo($"Press {(_requireCtrlModifier.Value ? "Ctrl+" : "")}{_toggleKey.Value} or {_altToggleKey.Value} to open the vault");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to load {PluginInfo.PLUGIN_NAME}: {ex}");
            }
        }

        /// <summary>
        /// Creates a hidden GameObject that persists across scene loads AND survives
        /// the game's UIHandler.UnloadGame cleanup that destroys regular GameObjects.
        /// </summary>
        private void CreatePersistentRunner()
        {
            if (_persistentRunner != null)
            {
                Log.LogInfo("PersistentRunner already exists");
                return;
            }

            // Create a new hidden GameObject
            _persistentRunner = new GameObject("TheVault_PersistentRunner");

            // Mark it to survive scene changes
            DontDestroyOnLoad(_persistentRunner);

            // Hide it from the game's cleanup routines and hierarchy
            _persistentRunner.hideFlags = HideFlags.HideAndDontSave;

            // Add the update runner component
            _updateRunner = _persistentRunner.AddComponent<PersistentUpdateRunner>();

            Log.LogInfo("Created hidden PersistentRunner that survives game cleanup");
        }

        /// <summary>
        /// Ensures UI components exist and recreates them if they were destroyed by the game's cleanup.
        /// Called from PlayerPatches when a character loads.
        /// </summary>
        public static void EnsureUIComponentsExist()
        {
            try
            {
                // Check if PersistentRunner was destroyed and recreate it
                if (_persistentRunner == null || _updateRunner == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating PersistentRunner...");
                    _persistentRunner = new GameObject("TheVault_PersistentRunner");
                    UnityEngine.Object.DontDestroyOnLoad(_persistentRunner);
                    _persistentRunner.hideFlags = HideFlags.HideAndDontSave;
                    _updateRunner = _persistentRunner.AddComponent<PersistentUpdateRunner>();
                    Log?.LogInfo("[EnsureUI] PersistentRunner recreated");
                }

                // Check if VaultUI was destroyed and recreate it
                if (_staticVaultUI == null)
                {
                    Log?.LogInfo("[EnsureUI] Recreating VaultUI...");
                    var uiObject = new GameObject("TheVault_UI");
                    UnityEngine.Object.DontDestroyOnLoad(uiObject);
                    // NOTE: Do NOT use HideFlags.HideAndDontSave on VaultUI!
                    // That flag prevents Unity's OnGUI from being called, which breaks the UI rendering.
                    // Only PersistentRunner needs HideFlags (it only uses Update, not OnGUI).

                    _staticVaultUI = uiObject.AddComponent<VaultUI>();
                    _staticVaultUI.Initialize(_staticVaultManager);
                    _staticVaultUI.SetToggleKey(StaticToggleKey, StaticRequireCtrl);
                    _staticVaultUI.SetAltToggleKey(StaticAltToggleKey);

                    _staticVaultHUD = uiObject.AddComponent<VaultHUD>();
                    _staticVaultHUD.Initialize(_staticVaultManager);

                    Log?.LogInfo("[EnsureUI] VaultUI and VaultHUD recreated");
                }
            }
            catch (Exception ex)
            {
                Log?.LogError($"[EnsureUI] Error recreating UI: {ex.Message}");
            }
        }

        private void InitializeConfig()
        {
            _toggleKey = Config.Bind(
                "UI",
                "ToggleKey",
                KeyCode.V,
                "Key to toggle the vault UI"
            );

            _requireCtrlModifier = Config.Bind(
                "UI",
                "RequireCtrlModifier",
                true,
                "Require Ctrl key to be held when pressing toggle key"
            );

            _altToggleKey = Config.Bind(
                "UI",
                "AltToggleKey",
                KeyCode.F8,
                "Alternative key to toggle vault UI (no modifier required). Useful for Steam Deck."
            );

            _enableHUD = Config.Bind(
                "HUD",
                "EnableHUD",
                true,
                "Show a persistent HUD bar displaying vault currency totals"
            );

            _hudPosition = Config.Bind(
                "HUD",
                "Position",
                "TopLeft",
                "HUD position: TopLeft, TopCenter, TopRight, BottomLeft, BottomCenter, BottomRight"
            );

            _hudToggleKey = Config.Bind(
                "HUD",
                "ToggleKey",
                KeyCode.F7,
                "Key to toggle the HUD display on/off"
            );

            _enableAutoSave = Config.Bind(
                "Saving",
                "EnableAutoSave",
                true,
                "Automatically save vault data periodically"
            );

            _autoSaveInterval = Config.Bind(
                "Saving",
                "AutoSaveInterval",
                300f,
                "Auto-save interval in seconds (default: 5 minutes)"
            );
        }

        /// <summary>
        /// Register mappings between Sun Haven item IDs and vault currency IDs.
        /// Auto-deposit is enabled so items are automatically converted to vault currency when picked up.
        /// </summary>
        private void RegisterItemMappings()
        {
            // Enable auto-deposit globally
            ItemPatches.AutoDepositEnabled = true;

            // Seasonal Tokens - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(18020, "seasonal_Spring", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(18021, "seasonal_Summer", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(18022, "seasonal_Winter", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(18023, "seasonal_Fall", autoDeposit: true);

            // Keys - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(1251, "key_copper", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1252, "key_iron", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1253, "key_adamant", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1254, "key_mithril", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1255, "key_sunite", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1256, "key_glorite", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1257, "key_kingslostmine", autoDeposit: true);

            // Special currencies - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(18013, "special_communitytoken", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(60014, "special_doubloon", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(60013, "special_blackbottlecap", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(18012, "special_redcarnivalticket", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(18016, "special_candycornpieces", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(18015, "special_manashard", autoDeposit: true);

            Log.LogInfo("Registered item-to-currency mappings with auto-deposit enabled");
        }

        private void ApplyPatches()
        {
            try
            {
                // Patch player initialization for loading vault data
                var playerType = typeof(Wish.Player);

                PatchMethod(playerType, "InitializeAsOwner",
                    typeof(PlayerPatches), "OnPlayerInitialized");

                // Patch shop purchase methods for vault currency checks
                var shopMenuType = AccessTools.TypeByName("Wish.ShopMenu");
                if (shopMenuType != null)
                {
                    PatchMethodPrefix(shopMenuType, "BuyItem",
                        typeof(ShopPatches), "OnBeforeBuyItem");
                }
                else
                {
                    Log.LogWarning("Could not find ShopMenu type - shop vault integration unavailable");
                }

                // Patch save/load for vault persistence
                var saveLoadType = AccessTools.TypeByName("Wish.SaveLoadManager");
                if (saveLoadType != null)
                {
                    PatchMethod(saveLoadType, "Save",
                        typeof(SaveLoadPatches), "OnGameSaved");

                    PatchMethod(saveLoadType, "Load",
                        typeof(SaveLoadPatches), "OnGameLoaded");
                }
                else
                {
                    Log.LogWarning("Could not find SaveLoadManager - using fallback save triggers");
                }

                // Patch return to menu for state reset (critical for character switching)
                var mainMenuType = AccessTools.TypeByName("Wish.MainMenuController");
                if (mainMenuType != null)
                {
                    // Try to patch ReturnToMainMenu or similar method
                    PatchMethod(mainMenuType, "ReturnToMainMenu",
                        typeof(SaveLoadPatches), "OnReturnToMenu");
                }

                // Also try patching the title screen load
                var titleType = AccessTools.TypeByName("Wish.TitleScreen");
                if (titleType != null)
                {
                    PatchMethod(titleType, "Start",
                        typeof(SaveLoadPatches), "OnReturnToMenu");
                }

                // Try GameManager's return to menu method
                var gameManagerType = AccessTools.TypeByName("Wish.GameManager");
                if (gameManagerType != null)
                {
                    PatchMethod(gameManagerType, "ReturnToMainMenu",
                        typeof(SaveLoadPatches), "OnReturnToMenu");
                    PatchMethod(gameManagerType, "QuitToMainMenu",
                        typeof(SaveLoadPatches), "OnReturnToMenu");
                }

                // Patch item pickup for auto-deposit
                PatchItemPickup(playerType);

                // Log results
                var patchedMethods = _harmony.GetPatchedMethods();
                int count = 0;
                foreach (var method in patchedMethods)
                {
                    Log.LogInfo($"Patched: {method.DeclaringType?.Name}.{method.Name}");
                    count++;
                }
                Log.LogInfo($"Total methods patched: {count}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Harmony patching failed: {ex}");
            }
        }

        private void PatchMethod(Type targetType, string methodName, Type patchType, string patchMethodName, Type[] parameters = null)
        {
            try
            {
                var original = parameters == null
                    ? AccessTools.Method(targetType, methodName)
                    : AccessTools.Method(targetType, methodName, parameters);

                if (original == null)
                {
                    Log.LogWarning($"Could not find method {targetType.Name}.{methodName}");
                    return;
                }

                var postfix = AccessTools.Method(patchType, patchMethodName);
                if (postfix == null)
                {
                    Log.LogWarning($"Could not find patch method {patchType.Name}.{patchMethodName}");
                    return;
                }

                _harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                Log.LogInfo($"Successfully patched {targetType.Name}.{methodName}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch {targetType.Name}.{methodName}: {ex.Message}");
            }
        }

        private void PatchMethodPrefix(Type targetType, string methodName, Type patchType, string patchMethodName, Type[] parameters = null)
        {
            try
            {
                var original = parameters == null
                    ? AccessTools.Method(targetType, methodName)
                    : AccessTools.Method(targetType, methodName, parameters);

                if (original == null)
                {
                    Log.LogWarning($"Could not find method {targetType.Name}.{methodName}");
                    return;
                }

                var prefix = AccessTools.Method(patchType, patchMethodName);
                if (prefix == null)
                {
                    Log.LogWarning($"Could not find patch method {patchType.Name}.{patchMethodName}");
                    return;
                }

                _harmony.Patch(original, prefix: new HarmonyMethod(prefix));
                Log.LogInfo($"Successfully patched {targetType.Name}.{methodName} (prefix)");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to patch {targetType.Name}.{methodName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Patch GameSave class to detect when characters are loaded.
        /// This is crucial for detecting character switches that don't trigger scene reloads.
        /// </summary>
        private void PatchGameSave()
        {
            try
            {
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                if (gameSaveType == null)
                {
                    Log.LogWarning("Could not find Wish.GameSave type");
                    return;
                }

                // Patch Load method
                var loadMethod = AccessTools.Method(gameSaveType, "Load");
                if (loadMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(GameSavePatches), "OnGameSaveLoad");
                    if (postfix != null)
                    {
                        _harmony.Patch(loadMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo("Patched GameSave.Load");
                    }
                }

                // Patch LoadCharacter method (critical for character switching)
                var loadCharMethod = AccessTools.Method(gameSaveType, "LoadCharacter");
                if (loadCharMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(GameSavePatches), "OnLoadCharacter");
                    if (postfix != null)
                    {
                        _harmony.Patch(loadCharMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo("Patched GameSave.LoadCharacter");
                    }
                }

                // Patch SetCurrentCharacter
                var setCharMethod = AccessTools.Method(gameSaveType, "SetCurrentCharacter");
                if (setCharMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(GameSavePatches), "OnSetCurrentCharacter");
                    if (postfix != null)
                    {
                        _harmony.Patch(setCharMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo("Patched GameSave.SetCurrentCharacter");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error patching GameSave: {ex.Message}");
            }
        }

        private void PatchItemPickup(Type playerType)
        {
            // Log all methods on Player that might be related to item pickup
            Log.LogInfo("Searching for item pickup methods on Player...");
            var allMethods = playerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var m in allMethods)
            {
                string nameLower = m.Name.ToLowerInvariant();
                if (nameLower.Contains("pickup") || nameLower.Contains("additem") || nameLower.Contains("collect") || nameLower.Contains("gain"))
                {
                    var parameters = m.GetParameters();
                    string paramStr = string.Join(", ", System.Linq.Enumerable.Select(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                    Log.LogInfo($"  Found: {m.Name}({paramStr}) in {m.DeclaringType.Name}");
                }
            }

            // Search for ItemPickup, DroppedItem, Collectible classes that might handle ground pickups
            string[] potentialClasses = new[]
            {
                "Wish.ItemPickup", "Wish.DroppedItem", "Wish.Collectible", "Wish.GroundItem",
                "Wish.ItemEntity", "Wish.PickupItem", "Wish.WorldItem", "Wish.ItemDrop",
                "ItemPickup", "DroppedItem", "Collectible", "GroundItem"
            };

            foreach (var className in potentialClasses)
            {
                var itemType = AccessTools.TypeByName(className);
                if (itemType != null)
                {
                    Log.LogInfo($"Found potential pickup class: {itemType.FullName}");
                    var methods = itemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var m in methods)
                    {
                        string nameLower = m.Name.ToLowerInvariant();
                        if (nameLower.Contains("pickup") || nameLower.Contains("collect") || nameLower.Contains("interact") || nameLower.Contains("onpick") || nameLower.Contains("trigger"))
                        {
                            var parameters = m.GetParameters();
                            string paramStr = string.Join(", ", System.Linq.Enumerable.Select(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                            Log.LogInfo($"  {itemType.Name}.{m.Name}({paramStr})");
                        }
                    }
                }
            }

            // The actual method in Sun Haven is Player.Pickup(int item, int amount = 1, bool rollForExtra = false)
            // Try to patch the Pickup method first
            var pickupMethod = AccessTools.Method(playerType, "Pickup");
            if (pickupMethod != null)
            {
                Log.LogInfo($"Found Pickup method: {pickupMethod.DeclaringType.FullName}.{pickupMethod.Name}");
                var parameters = pickupMethod.GetParameters();
                string paramStr = string.Join(", ", System.Linq.Enumerable.Select(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                Log.LogInfo($"  Parameters: ({paramStr})");

                // Use PREFIX to intercept BEFORE item is added to inventory
                var prefix = AccessTools.Method(typeof(ItemPatches), "OnPlayerPickupPrefix");
                var postfix = AccessTools.Method(typeof(ItemPatches), "OnPlayerPickup");
                if (prefix != null)
                {
                    _harmony.Patch(pickupMethod,
                        prefix: new HarmonyMethod(prefix),
                        postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                    Log.LogInfo($"Successfully patched {playerType.Name}.Pickup with PREFIX for auto-deposit");
                    // Don't return here - we also need to patch Inventory.AddItem below
                }
            }
            else
            {
                Log.LogWarning("Could not find Pickup method on Player");
            }

            // Patch Inventory.AddItem - this is the main method called by Wish.Pickup for ground pickups
            var inventoryType = AccessTools.TypeByName("Wish.Inventory");
            if (inventoryType == null)
                inventoryType = AccessTools.TypeByName("Wish.PlayerInventory");

            if (inventoryType != null)
            {
                Log.LogInfo($"Searching Inventory class: {inventoryType.FullName}");
                var invMethods = inventoryType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                // Log methods related to getting item counts (for shop/door integration)
                Log.LogInfo("=== Inventory methods for checking item amounts ===");
                foreach (var m in invMethods)
                {
                    string nameLower = m.Name.ToLowerInvariant();
                    if (nameLower.Contains("get") || nameLower.Contains("has") || nameLower.Contains("count") ||
                        nameLower.Contains("amount") || nameLower.Contains("total") || nameLower.Contains("contain"))
                    {
                        var parameters = m.GetParameters();
                        string paramStr = string.Join(", ", System.Linq.Enumerable.Select(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                        Log.LogInfo($"  {m.Name}({paramStr}) -> {m.ReturnType.Name}");
                    }
                }

                // Log AddItem methods
                Log.LogInfo("=== Inventory AddItem methods ===");
                foreach (var m in invMethods)
                {
                    string nameLower = m.Name.ToLowerInvariant();
                    if (nameLower.Contains("add") && nameLower.Contains("item"))
                    {
                        var parameters = m.GetParameters();
                        string paramStr = string.Join(", ", System.Linq.Enumerable.Select(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                        Log.LogInfo($"  {m.Name}({paramStr}) in {m.DeclaringType.Name}");
                    }
                }

                // Log RemoveItem methods
                Log.LogInfo("=== Inventory RemoveItem methods ===");
                foreach (var m in invMethods)
                {
                    string nameLower = m.Name.ToLowerInvariant();
                    if (nameLower.Contains("remove"))
                    {
                        var parameters = m.GetParameters();
                        string paramStr = string.Join(", ", System.Linq.Enumerable.Select(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                        Log.LogInfo($"  {m.Name}({paramStr}) -> {m.ReturnType.Name}");
                    }
                }

                // Search for shop/store/purchase related types in the assembly
                Log.LogInfo("=== Searching for Shop/Store/Purchase types ===");
                var assembly = inventoryType.Assembly;
                foreach (var type in assembly.GetTypes())
                {
                    string typeName = type.Name.ToLowerInvariant();
                    if (typeName.Contains("shop") || typeName.Contains("store") || typeName.Contains("purchase") ||
                        typeName.Contains("buy") || typeName.Contains("vendor") || typeName.Contains("merchant"))
                    {
                        Log.LogInfo($"  Found type: {type.FullName}");
                    }
                }

                // Search for door/chest/lock related types
                Log.LogInfo("=== Searching for Door/Chest/Lock types ===");
                foreach (var type in assembly.GetTypes())
                {
                    string typeName = type.Name.ToLowerInvariant();
                    if (typeName.Contains("door") || typeName.Contains("chest") || typeName.Contains("lock") ||
                        typeName.Contains("treasure") || typeName.Contains("gate"))
                    {
                        Log.LogInfo($"  Found type: {type.FullName}");
                    }
                }

                // Find the Item type for the signature
                var itemType = AccessTools.TypeByName("Wish.Item");
                if (itemType != null)
                {
                    Log.LogInfo($"Found Wish.Item type: {itemType.FullName}");

                    // Try AddItem(Item, int, int, bool, bool, bool) - the main pickup method
                    // We use POSTFIX so the notification happens first, then we move to vault
                    var addItemMethod = AccessTools.Method(inventoryType, "AddItem",
                        new[] { itemType, typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool) });

                    if (addItemMethod != null)
                    {
                        // Use PREFIX to intercept before item enters inventory - this is the main fix
                        var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPrefix");
                        var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPostfix");
                        if (prefix != null)
                        {
                            _harmony.Patch(addItemMethod,
                                prefix: new HarmonyMethod(prefix),
                                postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                            Log.LogInfo($"Successfully patched {inventoryType.Name}.AddItem(Item,int,int,bool,bool,bool) with PREFIX+POSTFIX for auto-deposit");
                        }
                        else if (postfix != null)
                        {
                            _harmony.Patch(addItemMethod, postfix: new HarmonyMethod(postfix));
                            Log.LogInfo($"Successfully patched {inventoryType.Name}.AddItem(Item,int,int,bool,bool,bool) with POSTFIX only for auto-deposit");
                        }
                    }
                    else
                    {
                        Log.LogWarning("Could not find AddItem(Item,int,int,bool,bool,bool) method");

                        // Try to find any AddItem method that takes Item as first parameter
                        foreach (var m in invMethods)
                        {
                            if (m.Name == "AddItem")
                            {
                                var parameters = m.GetParameters();
                                if (parameters.Length > 0 && parameters[0].ParameterType == itemType)
                                {
                                    string paramStr = string.Join(", ", System.Linq.Enumerable.Select(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                                    Log.LogInfo($"Found alternative AddItem: {m.Name}({paramStr})");

                                    var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPrefix");
                                    var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObjectPostfix");
                                    if (prefix != null)
                                    {
                                        _harmony.Patch(m,
                                            prefix: new HarmonyMethod(prefix),
                                            postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                                        Log.LogInfo($"Successfully patched {inventoryType.Name}.{m.Name} with PREFIX+POSTFIX for auto-deposit");
                                        break;
                                    }
                                    else if (postfix != null)
                                    {
                                        _harmony.Patch(m, postfix: new HarmonyMethod(postfix));
                                        Log.LogInfo($"Successfully patched {inventoryType.Name}.{m.Name} with POSTFIX for auto-deposit");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log.LogWarning("Could not find Wish.Item type");
                }

                // Also try to patch AddItem with int signatures as fallback
                var addItemIntMethod = AccessTools.Method(inventoryType, "AddItem", new[] { typeof(int), typeof(int) });
                if (addItemIntMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItem");
                    if (postfix != null)
                    {
                        _harmony.Patch(addItemIntMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo($"Successfully patched {inventoryType.Name}.AddItem(int,int) for auto-deposit");
                    }
                }

                // Patch GetAmount to include vault amounts - makes shops see vault currency
                var getAmountMethod = AccessTools.Method(inventoryType, "GetAmount", new[] { typeof(int) });
                if (getAmountMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryGetAmount");
                    if (postfix != null)
                    {
                        _harmony.Patch(getAmountMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo($"Successfully patched {inventoryType.Name}.GetAmount for vault integration");
                    }
                }
                else
                {
                    Log.LogWarning("Could not find Inventory.GetAmount method");
                }

                // Patch HasEnough to check vault - makes shops/doors allow purchases with vault currency
                var hasEnoughMethod = AccessTools.Method(inventoryType, "HasEnough", new[] { typeof(int), typeof(int) });
                if (hasEnoughMethod != null)
                {
                    var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryHasEnough");
                    if (postfix != null)
                    {
                        _harmony.Patch(hasEnoughMethod, postfix: new HarmonyMethod(postfix));
                        Log.LogInfo($"Successfully patched {inventoryType.Name}.HasEnough for vault integration");
                    }
                }
                else
                {
                    Log.LogWarning("Could not find Inventory.HasEnough method");
                }

                // Patch RemoveItem to deduct from vault when inventory is insufficient
                var removeItemMethod = AccessTools.Method(inventoryType, "RemoveItem", new[] { typeof(int), typeof(int), typeof(int) });
                if (removeItemMethod != null)
                {
                    var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryRemoveItemPrefix");
                    var postfix = AccessTools.Method(typeof(ItemPatches), "OnInventoryRemoveItemPostfix");
                    if (prefix != null && postfix != null)
                    {
                        _harmony.Patch(removeItemMethod,
                            prefix: new HarmonyMethod(prefix),
                            postfix: new HarmonyMethod(postfix));
                        Log.LogInfo($"Successfully patched {inventoryType.Name}.RemoveItem for vault integration");
                    }
                }
                else
                {
                    Log.LogWarning("Could not find Inventory.RemoveItem method");
                }
            }
            else
            {
                Log.LogWarning("Could not find Inventory type");
            }
        }

        private void Update()
        {
            // Check for auto-save
            if (_enableAutoSave.Value)
            {
                _saveSystem?.CheckAutoSave();
            }

            // Check for HUD toggle
            if (Input.GetKeyDown(_hudToggleKey.Value))
            {
                _vaultHUD?.Toggle();
            }

            // BACKUP: Poll for menu scene changes
            // This is a failsafe in case SceneManager.sceneLoaded stops firing
            _sceneCheckTimer += Time.deltaTime;
            if (_sceneCheckTimer >= SCENE_CHECK_INTERVAL)
            {
                _sceneCheckTimer = 0f;
                CheckForMenuSceneChange();
            }

            // Heartbeat - prove the plugin is still running
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _heartbeatTimer = 0f;
                _heartbeatCount++;
                Log.LogInfo($"[Heartbeat #{_heartbeatCount}] Plugin alive. Scene: {_lastKnownScene}, VaultLoaded: {PlayerPatches.IsVaultLoaded}, Character: {PlayerPatches.LoadedCharacterName ?? "none"}");
            }
        }

        /// <summary>
        /// Backup menu detection via polling.
        /// Checks the active scene name and triggers SaveAndReset when entering a menu scene.
        /// </summary>
        private void CheckForMenuSceneChange()
        {
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                string sceneName = activeScene.name;

                // Only log if scene actually changed
                if (sceneName != _lastKnownScene)
                {
                    Log.LogInfo($"[ScenePoll] Scene changed: '{_lastKnownScene}' -> '{sceneName}'");
                    _lastKnownScene = sceneName;

                    string sceneLower = sceneName.ToLowerInvariant();
                    bool isMenuScene = sceneLower.Contains("menu") || sceneLower.Contains("title");

                    // Detect transition INTO menu scene (was not in menu, now is)
                    if (isMenuScene && !_wasInMenuScene)
                    {
                        Log.LogInfo($"[ScenePoll] Menu scene detected via polling: {sceneName}");
                        PlayerPatches.SaveAndReset();
                    }

                    _wasInMenuScene = isMenuScene;
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error in CheckForMenuSceneChange: {ex.Message}");
            }
        }

        private static VaultHUD.HUDPosition ParseHUDPosition(string position)
        {
            return position?.ToLower() switch
            {
                "topleft" => VaultHUD.HUDPosition.TopLeft,
                "topcenter" => VaultHUD.HUDPosition.TopCenter,
                "topright" => VaultHUD.HUDPosition.TopRight,
                "bottomleft" => VaultHUD.HUDPosition.BottomLeft,
                "bottomcenter" => VaultHUD.HUDPosition.BottomCenter,
                "bottomright" => VaultHUD.HUDPosition.BottomRight,
                _ => VaultHUD.HUDPosition.TopLeft
            };
        }

        private void OnApplicationQuit()
        {
            // Save vault data on quit
            Log.LogInfo("Application quitting - saving vault data");
            _saveSystem?.ForceSave();
        }

        private void OnDisable()
        {
            Log.LogWarning("[CRITICAL] Plugin OnDisable called! Plugin is being disabled.");
            Log.LogWarning($"[CRITICAL] Last known scene: {_lastKnownScene}");
            Log.LogWarning($"[CRITICAL] Stack trace: {Environment.StackTrace}");
        }

        private void OnDestroy()
        {
            Log.LogWarning("[CRITICAL] Plugin OnDestroy called! Plugin is being destroyed.");
            Log.LogWarning($"[CRITICAL] Last known scene: {_lastKnownScene}");
            Log.LogWarning($"[CRITICAL] Stack trace: {Environment.StackTrace}");
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // IMPORTANT: Do NOT unpatch Harmony here!
            // Harmony patches are global and will continue working even after this MonoBehaviour is destroyed.
            // If we unpatch, the LoadCharacter and InitializeAsOwner hooks stop working,
            // which breaks character switching entirely.
            // Only unpatch in OnApplicationQuit when the game is actually closing.
            // _harmony?.UnpatchSelf(); // REMOVED - this was breaking character switching!

            _saveSystem?.ForceSave();
        }

        /// <summary>
        /// Called when a new scene is loaded.
        /// We only care about detecting menu scenes to reset vault state.
        /// Actual vault loading is handled by OnPlayerInitialized.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                // Log ALL scene changes for debugging
                Log.LogInfo($"[SceneChange] Scene loaded: '{scene.name}' (mode: {mode})");

                string sceneLower = scene.name.ToLowerInvariant();

                // Detect menu/title scenes to reset vault state
                if (sceneLower.Contains("menu") || sceneLower.Contains("title"))
                {
                    Log.LogInfo($"Menu scene detected: {scene.name}");
                    PlayerPatches.SaveAndReset();
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error in OnSceneLoaded: {ex.Message}");
            }
        }

        #region Public API

        /// <summary>
        /// Get the vault manager instance
        /// </summary>
        public static VaultManager GetVaultManager()
        {
            // Use static field which survives Plugin destruction
            return _staticVaultManager;
        }

        /// <summary>
        /// Get the save system instance
        /// </summary>
        public static VaultSaveSystem GetSaveSystem()
        {
            // Use static field which survives Plugin destruction
            return _staticSaveSystem;
        }

        /// <summary>
        /// Get the vault UI instance
        /// </summary>
        public static VaultUI GetVaultUI()
        {
            // Use static field which survives Plugin destruction
            return _staticVaultUI;
        }

        /// <summary>
        /// Open the vault UI
        /// </summary>
        public static void OpenVault()
        {
            // Use static field which survives Plugin destruction
            _staticVaultUI?.Show();
        }

        /// <summary>
        /// Close the vault UI
        /// </summary>
        public static void CloseVault()
        {
            // Use static field which survives Plugin destruction
            _staticVaultUI?.Hide();
        }

        /// <summary>
        /// Load vault data for a player
        /// </summary>
        public static void LoadVaultForPlayer(string playerName)
        {
            // Use static field which survives Plugin destruction
            _staticSaveSystem?.Load(playerName);
        }

        /// <summary>
        /// Force save vault data
        /// </summary>
        public static void SaveVault()
        {
            // Use static field which survives Plugin destruction
            _staticSaveSystem?.ForceSave();
        }

        /// <summary>
        /// Get the vault HUD instance
        /// </summary>
        public static VaultHUD GetVaultHUD()
        {
            // Use static field which survives Plugin destruction
            return _staticVaultHUD;
        }

        /// <summary>
        /// Toggle the vault HUD visibility
        /// </summary>
        public static void ToggleHUD()
        {
            // Use static field which survives Plugin destruction
            _staticVaultHUD?.Toggle();
        }

        #endregion
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.thevault";
        public const string PLUGIN_NAME = "The Vault";
        public const string PLUGIN_VERSION = "2.0.3";
    }

    /// <summary>
    /// A separate MonoBehaviour that runs on a hidden GameObject.
    /// This survives the game's UIHandler.UnloadGame cleanup because:
    /// 1. It's marked DontDestroyOnLoad
    /// 2. It's hidden from Unity's hierarchy (HideFlags)
    /// 3. It's not a child of any game object the cleanup knows about
    /// </summary>
    public class PersistentUpdateRunner : MonoBehaviour
    {
        private string _lastKnownScene = "";
        private bool _wasInMenuScene = true;
        private float _sceneCheckTimer = 0f;
        private float _heartbeatTimer = 0f;
        private int _heartbeatCount = 0;

        private const float SCENE_CHECK_INTERVAL = 0.5f;
        private const float HEARTBEAT_INTERVAL = 30f;

        private void Awake()
        {
            // Hide this object from the game's cleanup routines
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            Plugin.Log?.LogInfo("[PersistentRunner] Created hidden persistent runner");
        }

        private void Update()
        {
            // Poll for menu scene changes
            _sceneCheckTimer += Time.deltaTime;
            if (_sceneCheckTimer >= SCENE_CHECK_INTERVAL)
            {
                _sceneCheckTimer = 0f;
                CheckForMenuSceneChange();
            }

            // Heartbeat
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HEARTBEAT_INTERVAL)
            {
                _heartbeatTimer = 0f;
                _heartbeatCount++;
                Plugin.Log?.LogInfo($"[PersistentRunner Heartbeat #{_heartbeatCount}] Scene: {_lastKnownScene}, VaultLoaded: {PlayerPatches.IsVaultLoaded}, Character: {PlayerPatches.LoadedCharacterName ?? "none"}");
            }

            // Handle hotkey detection for Vault UI (since VaultUI might be destroyed)
            CheckHotkeys();
        }

        private void CheckHotkeys()
        {
            try
            {
                var vaultUI = Plugin.GetVaultUI();
                if (vaultUI == null) return;

                // Check for vault toggle key (with modifier)
                bool modifierHeld = !Plugin.StaticRequireCtrl ||
                    Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

                if (modifierHeld && Input.GetKeyDown(Plugin.StaticToggleKey))
                {
                    vaultUI.Toggle();
                }

                // Check for alternative toggle key (no modifier - for Steam Deck)
                if (Plugin.StaticAltToggleKey != KeyCode.None && Input.GetKeyDown(Plugin.StaticAltToggleKey))
                {
                    vaultUI.Toggle();
                }

                // Check for HUD toggle key
                if (Input.GetKeyDown(Plugin.StaticHUDToggleKey))
                {
                    var vaultHUD = Plugin.GetVaultHUD();
                    vaultHUD?.Toggle();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[PersistentRunner] Hotkey error: {ex.Message}");
            }
        }

        private void CheckForMenuSceneChange()
        {
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                string sceneName = activeScene.name;

                if (sceneName != _lastKnownScene)
                {
                    Plugin.Log?.LogInfo($"[PersistentRunner] Scene changed: '{_lastKnownScene}' -> '{sceneName}'");
                    _lastKnownScene = sceneName;

                    string sceneLower = sceneName.ToLowerInvariant();
                    bool isMenuScene = sceneLower.Contains("menu") || sceneLower.Contains("title");

                    if (isMenuScene && !_wasInMenuScene)
                    {
                        Plugin.Log?.LogInfo($"[PersistentRunner] Menu scene detected: {sceneName}");
                        PlayerPatches.SaveAndReset();
                    }

                    _wasInMenuScene = isMenuScene;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[PersistentRunner] Error: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            Plugin.Log?.LogWarning("[PersistentRunner] OnDestroy called - this should NOT happen!");
        }
    }
}
