using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using SunhavenMods.Shared;
using TheVault.Vault;

namespace TheVault.Patches
{
    /// <summary>
    /// Patches for item interactions.
    /// Handles automatic deposit of currency items into vault when picked up,
    /// and item spawning when withdrawing from vault.
    /// </summary>
    public static class ItemPatches
    {
        /// <summary>
        /// Maps game item IDs to vault currency types.
        /// Format: gameItemId -> (currencyId, autoDeposit)
        /// </summary>
        private static Dictionary<int, (string currencyId, bool autoDeposit)> _itemToCurrency
            = new Dictionary<int, (string, bool)>();

        /// <summary>
        /// Maps vault currency types to game item IDs (for withdrawing)
        /// </summary>
        private static Dictionary<string, int> _currencyToItem
            = new Dictionary<string, int>();

        /// <summary>
        /// Whether auto-deposit is enabled globally
        /// </summary>
        public static bool AutoDepositEnabled { get; set; } = false;

        /// <summary>
        /// Global flag: we are withdrawing items to inventory (bypass auto-deposit).
        /// </summary>
        public static bool IsWithdrawing { get; set; } = false;

        /// <summary>
        /// Item IDs currently being withdrawn (bypass auto-deposit for these).
        /// </summary>
        private static readonly HashSet<int> _withdrawingItemIds = new HashSet<int>();

        /// <summary>
        /// Cached reflection for fast GetItemId (runs on every pickup - must be fast).
        /// </summary>
        private static bool _itemIdReflectionCached = false;
        private static FieldInfo _cachedItemIdField;
        private static PropertyInfo _cachedItemIdProperty;
        private static MethodInfo _cachedItemIdMethod;

        /// <summary>
        /// Check if auto-deposit is enabled for a currency.
        /// </summary>
        public static bool IsAutoDepositEnabled(string currencyId)
        {
            int itemId = GetItemForCurrency(currencyId);
            if (itemId < 0) return false;
            return AutoDepositEnabled && _itemToCurrency.TryGetValue(itemId, out var m) && m.autoDeposit;
        }

        /// <summary>
        /// Toggle auto-deposit for a currency.
        /// </summary>
        public static void ToggleAutoDeposit(string currencyId)
        {
            int itemId = GetItemForCurrency(currencyId);
            if (itemId < 0 || !_itemToCurrency.TryGetValue(itemId, out var m)) return;
            _itemToCurrency[itemId] = (m.currencyId, !m.autoDeposit);
        }

        /// <summary>
        /// Mark that we are withdrawing this item (bypass auto-deposit).
        /// </summary>
        public static void StartWithdrawing(int itemId)
        {
            _withdrawingItemIds.Add(itemId);
        }

        /// <summary>
        /// Clear withdrawing state for this item.
        /// </summary>
        public static void StopWithdrawing(int itemId)
        {
            _withdrawingItemIds.Remove(itemId);
        }

        /// <summary>
        /// Reset state when returning to menu.
        /// </summary>
        public static void ResetState()
        {
            IsWithdrawing = false;
            _withdrawingItemIds.Clear();
        }

        /// <summary>
        /// Register a mapping between a game item and vault currency.
        /// </summary>
        /// <param name="gameItemId">The item's ID in Sun Haven's item database</param>
        /// <param name="currencyId">The vault currency ID</param>
        /// <param name="autoDeposit">Whether to auto-deposit when picked up</param>
        public static void RegisterItemCurrencyMapping(int gameItemId, string currencyId, bool autoDeposit = false)
        {
            _itemToCurrency[gameItemId] = (currencyId, autoDeposit);
            _currencyToItem[currencyId] = gameItemId;
            Plugin.Log?.LogInfo($"Registered item-currency mapping: Item {gameItemId} <-> {currencyId}");
        }

        /// <summary>
        /// Get the vault currency ID for a game item
        /// </summary>
        public static string GetCurrencyForItem(int gameItemId)
        {
            return _itemToCurrency.TryGetValue(gameItemId, out var mapping) ? mapping.currencyId : null;
        }

        /// <summary>
        /// Get the game item ID for a vault currency
        /// </summary>
        public static int GetItemForCurrency(string currencyId)
        {
            return _currencyToItem.TryGetValue(currencyId, out int itemId) ? itemId : -1;
        }

        /// <summary>
        /// Check if an item should be auto-deposited
        /// </summary>
        public static bool ShouldAutoDeposit(int gameItemId)
        {
            if (!AutoDepositEnabled) return false;
            return _itemToCurrency.TryGetValue(gameItemId, out var mapping) && mapping.autoDeposit;
        }

        /// <summary>
        /// Deposit items from player inventory into vault.
        /// </summary>
        /// <param name="gameItemId">The game item ID to deposit</param>
        /// <param name="amount">Amount to deposit</param>
        /// <returns>True if successful</returns>
        public static bool DepositItemToVault(int gameItemId, int amount)
        {
            if (amount <= 0) return false;

            string currencyId = GetCurrencyForItem(gameItemId);
            if (string.IsNullOrEmpty(currencyId))
            {
                Plugin.Log?.LogWarning($"No currency mapping for item {gameItemId}");
                return false;
            }

            var vaultManager = Plugin.GetVaultManager();
            if (vaultManager == null) return false;

            // Remove from inventory
            if (!RemoveItemFromInventory(gameItemId, amount))
            {
                Plugin.Log?.LogWarning($"Failed to remove {amount} of item {gameItemId} from inventory");
                return false;
            }

            // Add to vault
            AddCurrencyToVault(vaultManager, currencyId, amount);
            Plugin.Log?.LogInfo($"Deposited {amount} of item {gameItemId} as {currencyId}");
            return true;
        }

        /// <summary>
        /// Withdraw currency from vault and spawn as items.
        /// </summary>
        /// <param name="currencyId">The vault currency ID</param>
        /// <param name="amount">Amount to withdraw</param>
        /// <returns>True if successful</returns>
        public static bool WithdrawCurrencyToInventory(string currencyId, int amount)
        {
            if (amount <= 0) return false;

            var vaultManager = Plugin.GetVaultManager();
            if (vaultManager == null) return false;

            // Check if player has enough
            if (!vaultManager.HasCurrency(currencyId, amount))
            {
                Plugin.Log?.LogWarning($"Not enough {currencyId} in vault");
                return false;
            }

            int gameItemId = GetItemForCurrency(currencyId);
            if (gameItemId < 0)
            {
                Plugin.Log?.LogWarning($"No item mapping for currency {currencyId}");
                return false;
            }

            // Remove from vault
            if (!RemoveCurrencyFromVault(vaultManager, currencyId, amount))
            {
                return false;
            }

            // Add to inventory
            if (!AddItemToInventory(gameItemId, amount))
            {
                // Rollback vault change
                AddCurrencyToVault(vaultManager, currencyId, amount);
                Plugin.Log?.LogWarning($"Failed to add item {gameItemId} to inventory, rolled back vault");
                return false;
            }

            Plugin.Log?.LogInfo($"Withdrew {amount} {currencyId} as item {gameItemId}");
            return true;
        }

        /// <summary>
        /// Postfix patch for item pickup.
        /// Auto-deposits currency items if enabled.
        /// </summary>
        public static void OnItemPickedUp(object __instance, int itemId, int amount)
        {
            try
            {
                if (!ShouldAutoDeposit(itemId)) return;

                string currencyId = GetCurrencyForItem(itemId);
                if (string.IsNullOrEmpty(currencyId)) return;

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null) return;

                // Auto-deposit: remove from inventory, add to vault
                if (RemoveItemFromInventory(itemId, amount))
                {
                    AddCurrencyToVault(vaultManager, currencyId, amount);
                    Plugin.Log?.LogInfo($"Auto-deposited {amount} of item {itemId} as {currencyId}");

                    // Show notification
                    ShowAutoDepositNotification(currencyId, amount);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnItemPickedUp: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix patch for item pickup (single parameter version).
        /// Assumes amount of 1.
        /// </summary>
        public static void OnItemPickedUpSingle(object __instance, int itemId)
        {
            OnItemPickedUp(__instance, itemId, 1);
        }

        /// <summary>
        /// PREFIX patch for Player.Pickup method.
        /// Signature: Pickup(int item, int amount = 1, bool rollForExtra = false)
        /// We always return true to let the original method run - the AddItem POSTFIX will handle auto-deposit.
        /// This ensures the native notification system triggers.
        /// </summary>
        public static bool OnPlayerPickupPrefix(int item, int amount, bool rollForExtra)
        {
            return true; // Let original run; AddItem prefix handles auto-deposit
        }

        /// <summary>
        /// Postfix patch for Inventory.AddItem(int, int).
        /// Catches items added to inventory for auto-deposit.
        /// The notification is already shown by the original method.
        /// </summary>
        public static void OnInventoryAddItem(object __instance, int itemId, int amount)
        {
            try
            {
                if (_isProcessingAutoDeposit || IsWithdrawing || _withdrawingItemIds.Contains(itemId)) return;
                if (!ShouldAutoDeposit(itemId))
                {
                    return;
                }

                string currencyId = GetCurrencyForItem(itemId);
                if (string.IsNullOrEmpty(currencyId))
                {
                    return;
                }

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null)
                {
                    return;
                }

                _isProcessingAutoDeposit = true;
                try
                {
                    // Remove from inventory - try inventory instance first, then Player fallback
                    bool removed = TryRemoveFromInventory(__instance, itemId, amount) || RemoveItemFromInventory(itemId, amount);
                    if (!removed)
                    {
                        Plugin.Log?.LogWarning($"Failed to remove {amount} of item {itemId} from inventory for auto-deposit");
                        return;
                    }

                    AddCurrencyToVault(vaultManager, currencyId, amount);
                }
                finally
                {
                    _isProcessingAutoDeposit = false;
                }
            }
            catch (Exception ex)
            {
                _isProcessingAutoDeposit = false;
                Plugin.Log?.LogError($"Error in OnInventoryAddItem: {ex.Message}");
            }
        }

        /// <summary>
        /// Postfix patch for Inventory.AddItem(int, int, bool).
        /// Catches items added to inventory for auto-deposit.
        /// The notification is already shown by the original method if notify=true.
        /// </summary>
        public static void OnInventoryAddItemWithNotify(object __instance, int itemId, int amount, bool notify)
        {
            try
            {
                if (_isProcessingAutoDeposit || IsWithdrawing || _withdrawingItemIds.Contains(itemId)) return;
                if (!ShouldAutoDeposit(itemId))
                {
                    return;
                }

                string currencyId = GetCurrencyForItem(itemId);
                if (string.IsNullOrEmpty(currencyId))
                {
                    return;
                }

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null)
                {
                    return;
                }

                _isProcessingAutoDeposit = true;
                try
                {
                    // Remove from inventory - try inventory instance first, then Player fallback
                    bool removed = TryRemoveFromInventory(__instance, itemId, amount) || RemoveItemFromInventory(itemId, amount);
                    if (!removed)
                    {
                        Plugin.Log?.LogWarning($"Failed to remove {amount} of item {itemId} from inventory for auto-deposit");
                        return;
                    }

                    AddCurrencyToVault(vaultManager, currencyId, amount);
                }
                finally
                {
                    _isProcessingAutoDeposit = false;
                }
            }
            catch (Exception ex)
            {
                _isProcessingAutoDeposit = false;
                Plugin.Log?.LogError($"Error in OnInventoryAddItemWithNotify: {ex.Message}");
            }
        }

        /// <summary>
        /// Flag to prevent recursive auto-deposit when we're adding items back to inventory during withdraw
        /// </summary>
        private static bool _isProcessingAutoDeposit = false;

        /// <summary>
        /// PREFIX patch for Inventory.AddItem(Item, int, int, bool, bool, bool).
        /// Called by Wish.Pickup.goToPlayer() when picking up items from the ground.
        /// Returns false to SKIP the original - item never enters inventory, goes straight to vault.
        /// </summary>
        public static bool OnInventoryAddItemObjectPrefix(object __instance, object item, int amount, int slot, bool sendNotification, bool specialItem, bool superSecretCheck)
        {
            try
            {
                if (_isProcessingAutoDeposit) return true;

                int itemId = GetItemId(item);
                if (itemId < 0) return true;

                if (IsWithdrawing || _withdrawingItemIds.Contains(itemId)) return true;

                if (!ShouldAutoDeposit(itemId)) return true;

                string currencyId = GetCurrencyForItem(itemId);
                if (string.IsNullOrEmpty(currencyId)) return true;

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null) return true;

                _isProcessingAutoDeposit = true;
                try
                {
                    AddCurrencyToVault(vaultManager, currencyId, amount);
                    if (sendNotification)
                        ShowAutoDepositNotification(currencyId, amount);
                    return false; // Skip original - item does NOT go to inventory
                }
                finally
                {
                    _isProcessingAutoDeposit = false;
                }
            }
            catch (Exception ex)
            {
                _isProcessingAutoDeposit = false;
                Plugin.Log?.LogError($"Error in OnInventoryAddItemObjectPrefix: {ex.Message}\n{ex.StackTrace}");
                return true; // Let original run on error
            }
        }

        /// <summary>
        /// POSTFIX patch for Inventory.AddItem(Item, int, int, bool, bool, bool).
        /// Backup path - only runs if PREFIX returned true (e.g. withdraw, or non-auto-deposit item).
        /// </summary>
        public static void OnInventoryAddItemObjectPostfix(object __instance, object item, int amount, int slot, bool sendNotification, bool specialItem, bool superSecretCheck)
        {
            try
            {
                // Prevent recursive calls when we're doing withdrawals or other operations
                if (_isProcessingAutoDeposit) return;

                // Get the item ID from the Item object
                int itemId = GetItemId(item);
                if (IsWithdrawing || _withdrawingItemIds.Contains(itemId)) return;
                if (!ShouldAutoDeposit(itemId))
                {
                    // Not registered for auto-deposit
                    return;
                }

                string currencyId = GetCurrencyForItem(itemId);
                if (string.IsNullOrEmpty(currencyId)) return;
                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null) return;

                _isProcessingAutoDeposit = true;
                try
                {
                    // The item is now in inventory (and notification was shown if sendNotification was true)
                    // Now remove it from inventory and add to vault

                    // Remove from inventory - try inventory instance first, then Player fallback
                    bool removed = TryRemoveFromInventory(__instance, itemId, amount) || RemoveItemFromInventory(itemId, amount);
                    if (!removed) return;
                    // Add to vault
                    AddCurrencyToVault(vaultManager, currencyId, amount);
                }
                finally
                {
                    _isProcessingAutoDeposit = false;
                }
            }
            catch (Exception ex)
            {
                _isProcessingAutoDeposit = false;
                Plugin.Log?.LogError($"Error in OnInventoryAddItemObjectPostfix: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Initialize cached reflection for GetItemId. Runs once per game session.
        /// </summary>
        private static void EnsureItemIdCache()
        {
            if (_itemIdReflectionCached) return;
            lock (typeof(ItemPatches))
            {
                if (_itemIdReflectionCached) return;
                try
                {
                    var itemType = ReflectionHelper.FindWishType("Item") ?? typeof(object);
                    _cachedItemIdField = AccessTools.Field(itemType, "id")
                        ?? AccessTools.Field(itemType, "_id");
                    if (_cachedItemIdField == null)
                    {
                        _cachedItemIdProperty = AccessTools.Property(itemType, "id")
                            ?? AccessTools.Property(itemType, "Id")
                            ?? AccessTools.Property(itemType, "ItemID");
                    }
                    if (_cachedItemIdField == null && _cachedItemIdProperty == null)
                        _cachedItemIdMethod = AccessTools.Method(itemType, "ID");
                    _itemIdReflectionCached = true;
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[ItemPatches] EnsureItemIdCache: {ex.Message}");
                    _itemIdReflectionCached = true;
                }
            }
        }

        /// <summary>
        /// Get item ID using cached reflection. Fast path for pickup hot path.
        /// </summary>
        private static int GetItemId(object item)
        {
            if (item == null) return -1;
            EnsureItemIdCache();
            try
            {
                if (_cachedItemIdField != null)
                {
                    var r = _cachedItemIdField.GetValue(item);
                    if (r is int id) return id;
                }
                if (_cachedItemIdProperty != null)
                {
                    var r = _cachedItemIdProperty.GetValue(item);
                    if (r is int id) return id;
                }
                if (_cachedItemIdMethod != null)
                {
                    var r = _cachedItemIdMethod.Invoke(item, null);
                    if (r is int id) return id;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[ItemPatches] GetItemId: {ex.Message}");
            }
            return -1;
        }

        #region Inventory Operations

        /// <summary>
        /// Try to remove an item from a specific inventory instance.
        /// Tries multiple RemoveItem signatures since Sun Haven's API may vary.
        /// </summary>
        private static bool TryRemoveFromInventory(object inventory, int itemId, int amount)
        {
            if (inventory == null) return false;
            var invType = inventory.GetType();

            // Try RemoveItem(int id, int amount, int slot)
            var m = AccessTools.Method(invType, "RemoveItem", new[] { typeof(int), typeof(int), typeof(int) });
            if (m != null)
            {
                try
                {
                    var result = m.Invoke(inventory, new object[] { itemId, amount, -1 });
                    if (result is bool b) return b;
                    return true; // void or other return - assume success
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[ItemPatches] RemoveItem(int,int,int) failed: {ex.Message}");
                }
            }

            // Try RemoveItem(int id, int amount)
            m = AccessTools.Method(invType, "RemoveItem", new[] { typeof(int), typeof(int) });
            if (m != null)
            {
                try
                {
                    var result = m.Invoke(inventory, new object[] { itemId, amount });
                    if (result is bool b) return b;
                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[ItemPatches] RemoveItem(int,int) failed: {ex.Message}");
                }
            }

            // Try RemoveItemAmount(int id, int amount)
            m = AccessTools.Method(invType, "RemoveItemAmount", new[] { typeof(int), typeof(int) });
            if (m != null)
            {
                try
                {
                    var result = m.Invoke(inventory, new object[] { itemId, amount });
                    if (result is bool b) return b;
                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogDebug($"[ItemPatches] RemoveItemAmount failed: {ex.Message}");
                }
            }

            return false;
        }

        /// <summary>
        /// Remove an item from player inventory.
        /// Needs to interact with Sun Haven's inventory system.
        /// </summary>
        private static bool RemoveItemFromInventory(int itemId, int amount)
        {
            try
            {
                // Try to find and use the player's inventory
                var playerType = typeof(Wish.Player);
                var localPlayerProperty = AccessTools.Property(playerType, "LocalPlayer");
                if (localPlayerProperty == null)
                {
                    Plugin.Log?.LogWarning("Could not find LocalPlayer property");
                    return false;
                }

                var player = localPlayerProperty.GetValue(null);
                if (player == null)
                {
                    Plugin.Log?.LogWarning("LocalPlayer is null");
                    return false;
                }

                // Try to find RemoveItem method on Player
                var removeMethod = AccessTools.Method(playerType, "RemoveItem", new[] { typeof(int), typeof(int) });
                if (removeMethod != null)
                {
                    var result = removeMethod.Invoke(player, new object[] { itemId, amount });
                    Plugin.Log?.LogInfo($"RemoveItem via Player returned: {result}");
                    return result is bool b && b;
                }

                // Try getting the PlayerInventory and calling RemoveItem on it
                var inventoryField = AccessTools.Field(playerType, "playerInventory");
                if (inventoryField == null)
                    inventoryField = AccessTools.Field(playerType, "_playerInventory");
                if (inventoryField == null)
                    inventoryField = AccessTools.Field(playerType, "inventory");
                if (inventoryField == null)
                    inventoryField = AccessTools.Field(playerType, "_inventory");

                var inventoryProperty = AccessTools.Property(playerType, "PlayerInventory");
                if (inventoryProperty == null)
                    inventoryProperty = AccessTools.Property(playerType, "Inventory");

                object inventory = null;
                if (inventoryField != null)
                    inventory = inventoryField.GetValue(player);
                else if (inventoryProperty != null)
                    inventory = inventoryProperty.GetValue(player);

                if (inventory != null)
                {
                    var invType = inventory.GetType();
                    Plugin.Log?.LogInfo($"Found inventory of type: {invType.FullName}");

                    // Try RemoveItem(int, int)
                    var invRemoveMethod = AccessTools.Method(invType, "RemoveItem", new[] { typeof(int), typeof(int) });
                    if (invRemoveMethod != null)
                    {
                        var result = invRemoveMethod.Invoke(inventory, new object[] { itemId, amount });
                        Plugin.Log?.LogInfo($"RemoveItem(int,int) returned: {result}");
                        return result is bool b ? b : true;
                    }

                    // Try RemoveItem(int, int, bool) - some versions have a sendNotification param
                    invRemoveMethod = AccessTools.Method(invType, "RemoveItem", new[] { typeof(int), typeof(int), typeof(bool) });
                    if (invRemoveMethod != null)
                    {
                        var result = invRemoveMethod.Invoke(inventory, new object[] { itemId, amount, false });
                        Plugin.Log?.LogInfo($"RemoveItem(int,int,bool) returned: {result}");
                        return result is bool b ? b : true;
                    }

                    // Try RemoveItemAmount
                    invRemoveMethod = AccessTools.Method(invType, "RemoveItemAmount", new[] { typeof(int), typeof(int) });
                    if (invRemoveMethod != null)
                    {
                        var result = invRemoveMethod.Invoke(inventory, new object[] { itemId, amount });
                        Plugin.Log?.LogInfo($"RemoveItemAmount returned: {result}");
                        return result is bool b ? b : true;
                    }

                    Plugin.Log?.LogWarning($"Could not find RemoveItem method on inventory type {invType.Name}");
                }
                else
                {
                    Plugin.Log?.LogWarning("Could not find player inventory");
                }

                Plugin.Log?.LogWarning("Could not find a method to remove items from inventory");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error removing item from inventory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add an item to player inventory.
        /// Needs to interact with Sun Haven's inventory system.
        /// </summary>
        private static bool AddItemToInventory(int itemId, int amount)
        {
            try
            {
                var playerType = typeof(Wish.Player);
                var localPlayerProperty = AccessTools.Property(playerType, "LocalPlayer");
                if (localPlayerProperty == null) return false;

                var player = localPlayerProperty.GetValue(null);
                if (player == null) return false;

                var inventoryProperty = AccessTools.Property(playerType, "Inventory");
                if (inventoryProperty != null)
                {
                    var inventory = inventoryProperty.GetValue(player);
                    if (inventory != null)
                    {
                        // Game has AddItem(int, int, bool) and AddItem(int, int, int, bool, bool)
                        var invAddMethod = AccessTools.Method(inventory.GetType(), "AddItem", new[] { typeof(int), typeof(int), typeof(bool) });
                        if (invAddMethod != null)
                        {
                            invAddMethod.Invoke(inventory, new object[] { itemId, amount, true });
                            return true;
                        }
                        var invAddMethod5 = AccessTools.Method(inventory.GetType(), "AddItem", new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(bool) });
                        if (invAddMethod5 != null)
                        {
                            invAddMethod5.Invoke(inventory, new object[] { itemId, amount, 0, true, true });
                            return true;
                        }
                    }
                }

                var addMethod = AccessTools.Method(playerType, "AddItemToInventory", new[] { typeof(int), typeof(int) });
                if (addMethod != null)
                {
                    var result = addMethod.Invoke(player, new object[] { itemId, amount });
                    return result is bool b && b;
                }

                Plugin.Log?.LogWarning("Could not find a method to add items to inventory");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error adding item to inventory: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Vault Operations

        private static void AddCurrencyToVault(VaultManager vaultManager, string currencyId, int amount)
        {
            if (currencyId.StartsWith("seasonal_"))
            {
                string typeName = currencyId.Substring("seasonal_".Length);
                if (Enum.TryParse<SeasonalTokenType>(typeName, out var tokenType))
                {
                    vaultManager.AddSeasonalTokens(tokenType, amount);
                }
            }
            else if (currencyId.StartsWith("community_"))
            {
                vaultManager.AddCommunityTokens(currencyId.Substring("community_".Length), amount);
            }
            else if (currencyId.StartsWith("special_"))
            {
                vaultManager.AddSpecial(currencyId.Substring("special_".Length), amount);
            }
            else if (currencyId.StartsWith("key_"))
            {
                vaultManager.AddKeys(currencyId.Substring("key_".Length), amount);
            }
            else if (currencyId.StartsWith("ticket_"))
            {
                vaultManager.AddTickets(currencyId.Substring("ticket_".Length), amount);
            }
            else if (currencyId.StartsWith("orb_"))
            {
                vaultManager.AddOrbs(currencyId.Substring("orb_".Length), amount);
            }
            else if (currencyId.StartsWith("custom_"))
            {
                vaultManager.AddCustomCurrency(currencyId.Substring("custom_".Length), amount);
            }
        }

        private static bool RemoveCurrencyFromVault(VaultManager vaultManager, string currencyId, int amount)
        {
            if (currencyId.StartsWith("seasonal_"))
            {
                string typeName = currencyId.Substring("seasonal_".Length);
                if (Enum.TryParse<SeasonalTokenType>(typeName, out var tokenType))
                {
                    return vaultManager.RemoveSeasonalTokens(tokenType, amount);
                }
            }
            else if (currencyId.StartsWith("community_"))
            {
                return vaultManager.RemoveCommunityTokens(currencyId.Substring("community_".Length), amount);
            }
            else if (currencyId.StartsWith("special_"))
            {
                return vaultManager.RemoveSpecial(currencyId.Substring("special_".Length), amount);
            }
            else if (currencyId.StartsWith("key_"))
            {
                return vaultManager.RemoveKeys(currencyId.Substring("key_".Length), amount);
            }
            else if (currencyId.StartsWith("ticket_"))
            {
                return vaultManager.RemoveTickets(currencyId.Substring("ticket_".Length), amount);
            }
            else if (currencyId.StartsWith("orb_"))
            {
                return vaultManager.RemoveOrbs(currencyId.Substring("orb_".Length), amount);
            }
            else if (currencyId.StartsWith("custom_"))
            {
                return vaultManager.RemoveCustomCurrency(currencyId.Substring("custom_".Length), amount);
            }

            return false;
        }

        #endregion

        #region Inventory Integration Patches (HasEnough, GetAmount, RemoveItem)

        /// <summary>
        /// Get the vault amount for a game item ID.
        /// Returns 0 if the item is not a registered currency.
        /// </summary>
        public static int GetVaultAmount(int itemId)
        {
            string currencyId = GetCurrencyForItem(itemId);
            if (string.IsNullOrEmpty(currencyId)) return 0;

            var vaultManager = Plugin.GetVaultManager();
            if (vaultManager == null) return 0;

            if (currencyId.StartsWith("seasonal_"))
            {
                string typeName = currencyId.Substring("seasonal_".Length);
                if (Enum.TryParse<SeasonalTokenType>(typeName, out var tokenType))
                {
                    return vaultManager.GetSeasonalTokens(tokenType);
                }
            }
            else if (currencyId.StartsWith("community_"))
            {
                return vaultManager.GetCommunityTokens(currencyId.Substring("community_".Length));
            }
            else if (currencyId.StartsWith("special_"))
            {
                return vaultManager.GetSpecial(currencyId.Substring("special_".Length));
            }
            else if (currencyId.StartsWith("key_"))
            {
                return vaultManager.GetKeys(currencyId.Substring("key_".Length));
            }
            else if (currencyId.StartsWith("ticket_"))
            {
                return vaultManager.GetTickets(currencyId.Substring("ticket_".Length));
            }
            else if (currencyId.StartsWith("orb_"))
            {
                return vaultManager.GetOrbs(currencyId.Substring("orb_".Length));
            }

            return 0;
        }

        /// <summary>
        /// Check if the item is a registered vault currency.
        /// </summary>
        public static bool IsVaultCurrency(int itemId)
        {
            return _itemToCurrency.ContainsKey(itemId);
        }

        /// <summary>
        /// Flag to temporarily skip vault addition in GetAmount (used by RemoveItem prefix)
        /// </summary>
        private static bool _skipVaultInGetAmount = false;

        /// <summary>
        /// POSTFIX for Inventory.GetAmount(int id) -> int
        /// Adds vault amount to inventory amount for registered currencies.
        /// </summary>
        public static void OnInventoryGetAmount(int id, ref int __result)
        {
            try
            {
                // Skip vault addition if we're getting raw inventory count
                if (_skipVaultInGetAmount) return;

                if (!IsVaultCurrency(id)) return;

                int vaultAmount = GetVaultAmount(id);
                if (vaultAmount > 0)
                {
                    int originalResult = __result;
                    __result += vaultAmount;
                    Plugin.Log?.LogInfo($"GetAmount for item {id}: inventory={originalResult}, vault={vaultAmount}, total={__result}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnInventoryGetAmount: {ex.Message}");
            }
        }

        /// <summary>
        /// POSTFIX for Inventory.HasEnough(int id, int amount) -> bool
        /// Returns true if inventory + vault has enough for registered currencies.
        /// </summary>
        public static void OnInventoryHasEnough(int id, int amount, ref bool __result)
        {
            try
            {
                // If already has enough in inventory, no need to check vault
                if (__result) return;

                // Only check vault for registered currencies
                if (!IsVaultCurrency(id)) return;

                int vaultAmount = GetVaultAmount(id);
                if (vaultAmount >= amount)
                {
                    __result = true;
                    Plugin.Log?.LogInfo($"HasEnough for item {id}: vault has {vaultAmount}, need {amount} - returning TRUE");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnInventoryHasEnough: {ex.Message}");
            }
        }

        /// <summary>
        /// PREFIX for Inventory.RemoveItem(int id, int amount, int slot) -> List
        /// Stores the inventory amount before removal so postfix knows how much to take from vault.
        /// </summary>
        public static void OnInventoryRemoveItemPrefix(object __instance, int id, int amount, int slot, ref int __state)
        {
            __state = 0;
            try
            {
                if (!IsVaultCurrency(id)) return;

                // Get current inventory count before removal (RAW, without vault)
                var invType = __instance.GetType();
                var getAmountMethod = AccessTools.Method(invType, "GetAmount", new[] { typeof(int) });
                if (getAmountMethod != null)
                {
                    // Temporarily disable vault addition so we get raw inventory count
                    _skipVaultInGetAmount = true;
                    try
                    {
                        __state = (int)getAmountMethod.Invoke(__instance, new object[] { id });
                    }
                    finally
                    {
                        _skipVaultInGetAmount = false;
                    }
                    Plugin.Log?.LogInfo($"RemoveItem prefix: item {id}, rawInventory={__state}, requestedAmount={amount}");
                }
            }
            catch (Exception ex)
            {
                _skipVaultInGetAmount = false; // Ensure flag is reset on error
                Plugin.Log?.LogError($"Error in OnInventoryRemoveItemPrefix: {ex.Message}");
            }
        }

        /// <summary>
        /// POSTFIX for Inventory.RemoveItem(int id, int amount, int slot) -> List
        /// If inventory didn't have enough, deduct the remainder from vault.
        /// </summary>
        public static void OnInventoryRemoveItemPostfix(object __instance, int id, int amount, int slot, int __state)
        {
            try
            {
                if (!IsVaultCurrency(id)) return;

                int inventoryHadBefore = __state;

                // If inventory had enough, nothing to do with vault
                if (inventoryHadBefore >= amount)
                {
                    Plugin.Log?.LogInfo($"RemoveItem postfix: item {id}, inventory had enough ({inventoryHadBefore} >= {amount})");
                    return;
                }

                // Calculate how much should come from vault
                int fromVault = amount - inventoryHadBefore;

                string currencyId = GetCurrencyForItem(id);
                if (string.IsNullOrEmpty(currencyId)) return;

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null) return;

                int vaultAmount = GetVaultAmount(id);
                if (fromVault > 0 && vaultAmount >= fromVault)
                {
                    // Deduct from vault
                    bool removed = RemoveCurrencyFromVault(vaultManager, currencyId, fromVault);
                    Plugin.Log?.LogInfo($"RemoveItem postfix: deducted {fromVault} of item {id} from vault (success={removed})");
                }
                else if (fromVault > 0)
                {
                    Plugin.Log?.LogWarning($"RemoveItem postfix: vault has {vaultAmount} but need {fromVault} more");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnInventoryRemoveItemPostfix: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Shows the native game pickup notification for auto-deposited items.
        /// This triggers the same popup players see when picking up any item.
        /// </summary>
        private static object _cachedNotifyInstance;
        private static MethodInfo _cachedNotifyMethod;

        private static void ShowAutoDepositNotification(string currencyId, int amount)
        {
            try
            {
                int itemId = GetItemForCurrency(currencyId);
                if (itemId < 0) return;

                if (_cachedNotifyInstance != null && _cachedNotifyMethod != null)
                {
                    _cachedNotifyMethod.Invoke(_cachedNotifyInstance, new object[] { itemId, amount });
                    return;
                }

                TryCacheAndSendNotification(itemId, amount);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[ItemPatches] ShowAutoDepositNotification: {ex.Message}");
            }
        }

        private static void TryCacheAndSendNotification(int itemId, int amount)
        {
            try
            {
                var stackType = ReflectionHelper.FindWishType("NotificationStack");
                if (stackType != null)
                {
                    var inst = ReflectionHelper.GetSingletonInstance(stackType);
                    var sendMethod = AccessTools.Method(stackType, "SendNotification", new[] { typeof(int), typeof(int) });
                    if (inst != null && sendMethod != null)
                    {
                        _cachedNotifyInstance = inst;
                        _cachedNotifyMethod = sendMethod;
                        sendMethod.Invoke(inst, new object[] { itemId, amount });
                        return;
                    }
                }
                var notifType = ReflectionHelper.FindWishType("Notifications");
                if (notifType != null)
                {
                    var inst = ReflectionHelper.GetSingletonInstance(notifType);
                    var sendMethod = AccessTools.Method(notifType, "SendNotification", new[] { typeof(int), typeof(int) });
                    if (inst != null && sendMethod != null)
                    {
                        _cachedNotifyInstance = inst;
                        _cachedNotifyMethod = sendMethod;
                        sendMethod.Invoke(inst, new object[] { itemId, amount });
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[ItemPatches] TryCacheAndSendNotification: {ex.Message}");
            }
        }

    }
}
