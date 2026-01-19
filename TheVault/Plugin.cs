using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using TheVault.Patches;
using TheVault.UI;
using TheVault.Vault;
using HarmonyLib;
using System;
using UnityEngine;

namespace TheVault
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        public static ConfigFile ConfigFile { get; private set; }

        private Harmony _harmony;
        private VaultManager _vaultManager;
        private VaultSaveSystem _saveSystem;
        private VaultUI _vaultUI;

        // Configuration
        private ConfigEntry<KeyCode> _toggleKey;
        private ConfigEntry<bool> _requireCtrlModifier;
        private ConfigEntry<bool> _enableAutoSave;
        private ConfigEntry<float> _autoSaveInterval;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            ConfigFile = Config;

            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");

            try
            {
                // Initialize configuration
                InitializeConfig();

                // Initialize vault system
                _vaultManager = new VaultManager();
                _saveSystem = new VaultSaveSystem(_vaultManager);

                // Create UI GameObject
                var uiObject = new GameObject("TheVault_UI");
                DontDestroyOnLoad(uiObject);
                _vaultUI = uiObject.AddComponent<VaultUI>();
                _vaultUI.Initialize(_vaultManager);
                _vaultUI.SetToggleKey(_toggleKey.Value, _requireCtrlModifier.Value);

                // Register item-to-currency mappings for deposit/withdraw
                RegisterItemMappings();

                // Apply Harmony patches
                _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
                ApplyPatches();

                Log.LogInfo($"{PluginInfo.PLUGIN_NAME} loaded successfully!");
                Log.LogInfo($"Press {(_requireCtrlModifier.Value ? "Ctrl+" : "")}{_toggleKey.Value} to open the vault");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to load {PluginInfo.PLUGIN_NAME}: {ex}");
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

            // Community Token - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(18013, "community_community", autoDeposit: true);

            // Keys - auto-deposit enabled
            ItemPatches.RegisterItemCurrencyMapping(1251, "key_copper", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1252, "key_iron", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1253, "key_adamant", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1254, "key_mithril", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1255, "key_sunite", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1256, "key_glorite", autoDeposit: true);
            ItemPatches.RegisterItemCurrencyMapping(1257, "key_kingslostmine", autoDeposit: true);

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
                    var addItemMethod = AccessTools.Method(inventoryType, "AddItem",
                        new[] { itemType, typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(bool) });

                    if (addItemMethod != null)
                    {
                        var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObject");
                        if (prefix != null)
                        {
                            _harmony.Patch(addItemMethod, prefix: new HarmonyMethod(prefix));
                            Log.LogInfo($"Successfully patched {inventoryType.Name}.AddItem(Item,int,int,bool,bool,bool) with PREFIX for auto-deposit");
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

                                    var prefix = AccessTools.Method(typeof(ItemPatches), "OnInventoryAddItemObject");
                                    if (prefix != null)
                                    {
                                        _harmony.Patch(m, prefix: new HarmonyMethod(prefix));
                                        Log.LogInfo($"Successfully patched {inventoryType.Name}.{m.Name} with PREFIX for auto-deposit");
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
        }

        private void OnApplicationQuit()
        {
            // Save vault data on quit
            Log.LogInfo("Application quitting - saving vault data");
            _saveSystem?.ForceSave();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _saveSystem?.ForceSave();
        }

        #region Public API

        /// <summary>
        /// Get the vault manager instance
        /// </summary>
        public static VaultManager GetVaultManager()
        {
            return Instance?._vaultManager;
        }

        /// <summary>
        /// Get the save system instance
        /// </summary>
        public static VaultSaveSystem GetSaveSystem()
        {
            return Instance?._saveSystem;
        }

        /// <summary>
        /// Get the vault UI instance
        /// </summary>
        public static VaultUI GetVaultUI()
        {
            return Instance?._vaultUI;
        }

        /// <summary>
        /// Open the vault UI
        /// </summary>
        public static void OpenVault()
        {
            Instance?._vaultUI?.Show();
        }

        /// <summary>
        /// Close the vault UI
        /// </summary>
        public static void CloseVault()
        {
            Instance?._vaultUI?.Hide();
        }

        /// <summary>
        /// Load vault data for a player
        /// </summary>
        public static void LoadVaultForPlayer(string playerName)
        {
            Instance?._saveSystem?.Load(playerName);
        }

        /// <summary>
        /// Force save vault data
        /// </summary>
        public static void SaveVault()
        {
            Instance?._saveSystem?.ForceSave();
        }

        #endregion
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.azraelgodking.thevault";
        public const string PLUGIN_NAME = "The Vault";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
