using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /// Format: gameItemId -> currencyId
        /// </summary>
        private static Dictionary<int, string> _itemToCurrency
            = new Dictionary<int, string>();

        /// <summary>
        /// Maps vault currency types to game item IDs (for withdrawing)
        /// </summary>
        private static Dictionary<string, int> _currencyToItem
            = new Dictionary<string, int>();

        /// <summary>
        /// Per-currency auto-deposit settings (can be toggled by user)
        /// Format: currencyId -> autoDepositEnabled
        /// </summary>
        private static Dictionary<string, bool> _currencyAutoDeposit
            = new Dictionary<string, bool>();

        /// <summary>
        /// Whether auto-deposit is enabled globally
        /// </summary>
        public static bool AutoDepositEnabled { get; set; } = false;

        /// <summary>
        /// When true, bypasses ALL auto-deposit logic. Used during withdrawals.
        /// This is separate from AutoDepositEnabled to ensure withdrawals work even if
        /// there are timing issues with the main flag.
        /// </summary>
        public static bool IsWithdrawing { get; set; } = false;

        /// <summary>
        /// Tracks recently withdrawn items with timestamps. Items in this dictionary will not be auto-deposited
        /// until the withdrawal window expires. This handles async inventory operations where the postfix
        /// might fire after the withdrawal method returns.
        /// Key: itemId, Value: DateTime when withdrawal started
        /// </summary>
        private static Dictionary<int, DateTime> _withdrawingItems = new Dictionary<int, DateTime>();

        /// <summary>
        /// Time window (in milliseconds) to block auto-deposit after a withdrawal starts.
        /// This should be long enough to cover any async delays in inventory operations.
        /// </summary>
        private const int WITHDRAWAL_BLOCK_WINDOW_MS = 2000;

        /// <summary>
        /// Mark an item as being withdrawn (prevents auto-deposit for WITHDRAWAL_BLOCK_WINDOW_MS)
        /// </summary>
        public static void StartWithdrawing(int itemId)
        {
            _withdrawingItems[itemId] = DateTime.Now;
            Plugin.Log?.LogInfo($"Started withdrawing item {itemId} - auto-deposit blocked for {WITHDRAWAL_BLOCK_WINDOW_MS}ms");
        }

        /// <summary>
        /// Explicitly stop withdrawing an item (optional - withdrawal will auto-expire)
        /// This is kept for backwards compatibility but the time-based expiry is the primary mechanism.
        /// </summary>
        public static void StopWithdrawing(int itemId)
        {
            // Don't actually remove - let the time window handle it
            // This prevents race conditions where StopWithdrawing is called before postfixes run
            Plugin.Log?.LogInfo($"StopWithdrawing called for item {itemId} - will auto-expire");
        }

        /// <summary>
        /// Check if an item was recently withdrawn (within WITHDRAWAL_BLOCK_WINDOW_MS)
        /// </summary>
        public static bool IsItemBeingWithdrawn(int itemId)
        {
            if (_withdrawingItems.TryGetValue(itemId, out DateTime withdrawStart))
            {
                double elapsed = (DateTime.Now - withdrawStart).TotalMilliseconds;
                if (elapsed < WITHDRAWAL_BLOCK_WINDOW_MS)
                {
                    Plugin.Log?.LogInfo($"Item {itemId} was withdrawn {elapsed:F0}ms ago - blocking auto-deposit");
                    return true;
                }
                // Expired - clean up
                _withdrawingItems.Remove(itemId);
            }
            return false;
        }

        /// <summary>
        /// Register a mapping between a game item and vault currency.
        /// </summary>
        /// <param name="gameItemId">The item's ID in Sun Haven's item database</param>
        /// <param name="currencyId">The vault currency ID</param>
        /// <param name="autoDeposit">Whether to auto-deposit when picked up (default setting)</param>
        public static void RegisterItemCurrencyMapping(int gameItemId, string currencyId, bool autoDeposit = false)
        {
            _itemToCurrency[gameItemId] = currencyId;
            _currencyToItem[currencyId] = gameItemId;
            _currencyAutoDeposit[currencyId] = autoDeposit;
            Plugin.Log?.LogInfo($"Registered item-currency mapping: Item {gameItemId} <-> {currencyId}");
        }

        /// <summary>
        /// Get the vault currency ID for a game item
        /// </summary>
        public static string GetCurrencyForItem(int gameItemId)
        {
            return _itemToCurrency.TryGetValue(gameItemId, out var currencyId) ? currencyId : null;
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
            // Never auto-deposit during withdrawals (global flag or item-specific)
            if (IsWithdrawing) return false;
            if (IsItemBeingWithdrawn(gameItemId)) return false;
            if (!AutoDepositEnabled) return false;

            // Check if item is registered and has auto-deposit enabled
            if (!_itemToCurrency.TryGetValue(gameItemId, out var currencyId))
                return false;

            return _currencyAutoDeposit.TryGetValue(currencyId, out var enabled) && enabled;
        }

        /// <summary>
        /// Check if auto-deposit is enabled for a specific currency
        /// </summary>
        public static bool IsAutoDepositEnabled(string currencyId)
        {
            return _currencyAutoDeposit.TryGetValue(currencyId, out var enabled) && enabled;
        }

        /// <summary>
        /// Toggle auto-deposit for a specific currency
        /// </summary>
        public static void SetAutoDeposit(string currencyId, bool enabled)
        {
            if (_currencyAutoDeposit.ContainsKey(currencyId))
            {
                _currencyAutoDeposit[currencyId] = enabled;
                Plugin.Log?.LogInfo($"Auto-deposit for {currencyId}: {enabled}");
            }
        }

        /// <summary>
        /// Toggle auto-deposit for a specific currency
        /// </summary>
        public static bool ToggleAutoDeposit(string currencyId)
        {
            if (_currencyAutoDeposit.ContainsKey(currencyId))
            {
                _currencyAutoDeposit[currencyId] = !_currencyAutoDeposit[currencyId];
                Plugin.Log?.LogInfo($"Auto-deposit for {currencyId} toggled to: {_currencyAutoDeposit[currencyId]}");
                return _currencyAutoDeposit[currencyId];
            }
            return false;
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
            // Just log for debugging - let the original method run
            // The AddItem POSTFIX will handle moving the item to vault after notification is shown
            if (ShouldAutoDeposit(item))
            {
                Plugin.Log?.LogInfo($"OnPlayerPickupPrefix: item={item}, amount={amount} - will be auto-deposited via AddItem POSTFIX");
            }
            return true;
        }

        /// <summary>
        /// POSTFIX patch for Player.Pickup method (backup for debugging).
        /// Signature: Pickup(int item, int amount = 1, bool rollForExtra = false)
        /// </summary>
        public static void OnPlayerPickup(int item, int amount, bool rollForExtra)
        {
            // This postfix only runs if the prefix returned true (item was not auto-deposited)
            Plugin.Log?.LogInfo($"OnPlayerPickup POSTFIX called: item={item}, amount={amount} (item added to inventory normally)");
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
                if (IsProcessingAutoDeposit(itemId)) return;
                if (WasRecentlyDeposited(itemId)) return;

                Plugin.Log?.LogInfo($"OnInventoryAddItem called: itemId={itemId}, amount={amount}");

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

                StartProcessingAutoDeposit(itemId);
                try
                {
                    Plugin.Log?.LogInfo($"Auto-depositing {amount} of item {itemId} as {currencyId} (via Inventory.AddItem)");

                    // Remove from inventory
                    var invType = __instance.GetType();

                    // Get inventory count BEFORE removal for logging (raw, without vault additions)
                    int countBefore = -1;
                    var getAmountMethod = AccessTools.Method(invType, "GetAmount", new[] { typeof(int) });
                    if (getAmountMethod != null)
                    {
                        _skipVaultInGetAmount = true;
                        try
                        {
                            countBefore = (int)getAmountMethod.Invoke(__instance, new object[] { itemId });
                            Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItem - Inventory count BEFORE removal: {countBefore} of item {itemId} (raw, no vault)");
                        }
                        finally
                        {
                            _skipVaultInGetAmount = false;
                        }
                    }

                    var removeMethod = AccessTools.Method(invType, "RemoveItem", new[] { typeof(int), typeof(int), typeof(int) });
                    if (removeMethod != null)
                    {
                        Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItem - Calling RemoveItem({itemId}, {amount}, -1)...");
                        removeMethod.Invoke(__instance, new object[] { itemId, amount, -1 });
                        Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItem - RemoveItem completed");
                    }

                    // Get inventory count AFTER removal to verify it worked (raw, without vault additions)
                    int countAfter = -1;
                    if (getAmountMethod != null)
                    {
                        _skipVaultInGetAmount = true;
                        try
                        {
                            countAfter = (int)getAmountMethod.Invoke(__instance, new object[] { itemId });
                            Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItem - Inventory count AFTER removal: {countAfter} of item {itemId} (raw, no vault)");
                        }
                        finally
                        {
                            _skipVaultInGetAmount = false;
                        }

                        if (countBefore >= 0 && countAfter >= 0)
                        {
                            int removed = countBefore - countAfter;
                            Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItem - Items actually removed: {removed} (expected: {amount})");

                            if (removed == 0)
                            {
                                Plugin.Log?.LogWarning($"[DEBUG] OnInventoryAddItem - WARNING: No items were removed from inventory!");
                            }
                        }
                    }

                    AddCurrencyToVault(vaultManager, currencyId, amount);
                    MarkAsDeposited(itemId);
                    Plugin.Log?.LogInfo($"Auto-deposited {amount} of item {itemId} as {currencyId}");
                }
                finally
                {
                    StopProcessingAutoDeposit(itemId);
                }
            }
            catch (Exception ex)
            {
                StopProcessingAutoDeposit(itemId);
                Plugin.Log?.LogError($"Error in OnInventoryAddItem: {ex.Message}\n{ex.StackTrace}");
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
                if (IsProcessingAutoDeposit(itemId)) return;
                if (WasRecentlyDeposited(itemId)) return;

                Plugin.Log?.LogInfo($"OnInventoryAddItemWithNotify called: itemId={itemId}, amount={amount}, notify={notify}");

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

                StartProcessingAutoDeposit(itemId);
                try
                {
                    Plugin.Log?.LogInfo($"Auto-depositing {amount} of item {itemId} as {currencyId} (via Inventory.AddItem with notify)");

                    // Remove from inventory
                    var invType = __instance.GetType();

                    // Get inventory count BEFORE removal for logging (raw, without vault additions)
                    int countBefore = -1;
                    var getAmountMethod = AccessTools.Method(invType, "GetAmount", new[] { typeof(int) });
                    if (getAmountMethod != null)
                    {
                        _skipVaultInGetAmount = true;
                        try
                        {
                            countBefore = (int)getAmountMethod.Invoke(__instance, new object[] { itemId });
                            Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItemWithNotify - Inventory count BEFORE removal: {countBefore} of item {itemId} (raw, no vault)");
                        }
                        finally
                        {
                            _skipVaultInGetAmount = false;
                        }
                    }

                    var removeMethod = AccessTools.Method(invType, "RemoveItem", new[] { typeof(int), typeof(int), typeof(int) });
                    if (removeMethod != null)
                    {
                        Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItemWithNotify - Calling RemoveItem({itemId}, {amount}, -1)...");
                        removeMethod.Invoke(__instance, new object[] { itemId, amount, -1 });
                        Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItemWithNotify - RemoveItem completed");
                    }

                    // Get inventory count AFTER removal to verify it worked (raw, without vault additions)
                    int countAfter = -1;
                    if (getAmountMethod != null)
                    {
                        _skipVaultInGetAmount = true;
                        try
                        {
                            countAfter = (int)getAmountMethod.Invoke(__instance, new object[] { itemId });
                            Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItemWithNotify - Inventory count AFTER removal: {countAfter} of item {itemId} (raw, no vault)");
                        }
                        finally
                        {
                            _skipVaultInGetAmount = false;
                        }

                        if (countBefore >= 0 && countAfter >= 0)
                        {
                            int removed = countBefore - countAfter;
                            Plugin.Log?.LogInfo($"[DEBUG] OnInventoryAddItemWithNotify - Items actually removed: {removed} (expected: {amount})");

                            if (removed == 0)
                            {
                                Plugin.Log?.LogWarning($"[DEBUG] OnInventoryAddItemWithNotify - WARNING: No items were removed from inventory!");
                            }
                        }
                    }

                    AddCurrencyToVault(vaultManager, currencyId, amount);
                    MarkAsDeposited(itemId);
                    Plugin.Log?.LogInfo($"Auto-deposited {amount} of item {itemId} as {currencyId}");
                }
                finally
                {
                    StopProcessingAutoDeposit(itemId);
                }
            }
            catch (Exception ex)
            {
                StopProcessingAutoDeposit(itemId);
                Plugin.Log?.LogError($"Error in OnInventoryAddItemWithNotify: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Set of item IDs currently being processed for auto-deposit.
        /// Using a set instead of a boolean flag allows concurrent processing of different item types.
        /// </summary>
        private static HashSet<int> _processingAutoDepositItems = new HashSet<int>();

        /// <summary>
        /// Queue of pending deposits. Items are added here and processed after a short delay
        /// to allow multiple stacks picked up simultaneously to be batched together.
        /// Key: itemId, Value: (totalAmount, lastAddedTime)
        /// </summary>
        private static Dictionary<int, (int amount, DateTime addedTime)> _pendingDeposits = new Dictionary<int, (int, DateTime)>();

        /// <summary>
        /// Lock object for thread-safe access to pending deposits
        /// </summary>
        private static readonly object _pendingDepositsLock = new object();

        /// <summary>
        /// Delay (in milliseconds) before processing pending deposits.
        /// This allows multiple stacks picked up at the same time to be batched.
        /// </summary>
        private const int DEPOSIT_BATCH_DELAY_MS = 100;

        /// <summary>
        /// Tracks items currently being processed to prevent re-entry
        /// </summary>
        private static HashSet<int> _currentlyProcessingDeposits = new HashSet<int>();

        /// <summary>
        /// Tracks recently deposited items with timestamps to prevent duplicate deposits
        /// from multiple patched methods firing for the same pickup event.
        /// Key: itemId, Value: DateTime of last deposit
        /// </summary>
        private static Dictionary<int, DateTime> _recentlyDepositedItems = new Dictionary<int, DateTime>();

        /// <summary>
        /// Time window (in milliseconds) to consider a deposit as "recent" and skip duplicate processing.
        /// This should be long enough to cover multiple postfix methods firing for the same pickup event.
        /// </summary>
        private const int RECENT_DEPOSIT_WINDOW_MS = 500;

        /// <summary>
        /// Check if a specific item is currently being processed for auto-deposit
        /// </summary>
        private static bool IsProcessingAutoDeposit(int itemId)
        {
            return _processingAutoDepositItems.Contains(itemId);
        }

        /// <summary>
        /// Check if an item was recently deposited (within RECENT_DEPOSIT_WINDOW_MS)
        /// </summary>
        private static bool WasRecentlyDeposited(int itemId)
        {
            if (_recentlyDepositedItems.TryGetValue(itemId, out DateTime lastDeposit))
            {
                double elapsed = (DateTime.Now - lastDeposit).TotalMilliseconds;
                Plugin.Log?.LogInfo($"[DEBUG] WasRecentlyDeposited check for item {itemId}: elapsed={elapsed:F0}ms, window={RECENT_DEPOSIT_WINDOW_MS}ms");
                if (elapsed < RECENT_DEPOSIT_WINDOW_MS)
                {
                    Plugin.Log?.LogInfo($"Item {itemId} was recently deposited {elapsed:F0}ms ago, skipping duplicate");
                    return true;
                }
            }
            else
            {
                Plugin.Log?.LogInfo($"[DEBUG] WasRecentlyDeposited check for item {itemId}: NOT in recently deposited list");
            }
            return false;
        }

        /// <summary>
        /// Mark an item as recently deposited
        /// </summary>
        private static void MarkAsDeposited(int itemId)
        {
            _recentlyDepositedItems[itemId] = DateTime.Now;
        }

        /// <summary>
        /// Mark an item as being processed for auto-deposit
        /// </summary>
        private static void StartProcessingAutoDeposit(int itemId)
        {
            _processingAutoDepositItems.Add(itemId);
        }

        /// <summary>
        /// Mark an item as no longer being processed for auto-deposit
        /// </summary>
        private static void StopProcessingAutoDeposit(int itemId)
        {
            _processingAutoDepositItems.Remove(itemId);
        }

        /// <summary>
        /// PREFIX patch for Inventory.AddItem(Item, int, int, bool, bool, bool).
        /// This is the actual method called by Wish.Pickup when items are collected from the ground.
        /// Signature: AddItem(Item item, int amount, int slot, bool sendNotification, bool specialItem, bool superSecretCheck)
        /// We use PREFIX to intercept BEFORE the item enters inventory, deposit directly to vault, and skip the original method.
        /// </summary>
        /// <returns>False to skip original method (item goes directly to vault), True to let original run normally</returns>
        public static bool OnInventoryAddItemObjectPrefix(object __instance, object item, int amount, int slot, bool sendNotification, bool specialItem, bool superSecretCheck)
        {
            int itemId = GetItemId(item);

            try
            {
                // Skip if we're currently processing a withdrawal for this item
                if (IsWithdrawing) return true;
                if (IsItemBeingWithdrawn(itemId)) return true;

                // Skip if not registered for auto-deposit
                if (!ShouldAutoDeposit(itemId)) return true;

                string currencyId = GetCurrencyForItem(itemId);
                if (string.IsNullOrEmpty(currencyId)) return true;

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null) return true;

                Plugin.Log?.LogInfo($"[PREFIX] Intercepting {amount} of item {itemId} - depositing directly to vault as {currencyId}");

                // Mark as deposited IMMEDIATELY to prevent POSTFIX from also depositing
                MarkAsDeposited(itemId);

                // Add directly to vault - item never enters inventory
                AddCurrencyToVault(vaultManager, currencyId, amount);
                Plugin.Log?.LogInfo($"[PREFIX] Deposited {amount} of item {itemId} to vault");

                // Show notification using the game's native notification system
                // We pass the item object directly to trigger the same notification the player would see
                if (sendNotification)
                {
                    TriggerPickupNotification(item, amount);
                }

                // Return FALSE to skip the original AddItem method - item goes directly to vault
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnInventoryAddItemObjectPrefix: {ex.Message}");
                // On error, let the original method run
                return true;
            }
        }

        /// <summary>
        /// Cached reference to NotificationStack instance
        /// </summary>
        private static object _notificationStackInstance;
        private static MethodInfo _addNotificationMethod;
        private static bool _notificationSystemInitialized;

        /// <summary>
        /// Trigger the game's native pickup notification for an item.
        /// Uses NotificationStack.SendNotification(string text, int id, int amount, bool unique, bool error)
        /// </summary>
        private static void TriggerPickupNotification(object item, int amount)
        {
            try
            {
                int itemId = GetItemId(item);

                // Initialize notification system on first use
                if (!_notificationSystemInitialized)
                {
                    InitializeNotificationSystem();
                    _notificationSystemInitialized = true;
                }

                // Try to get instance at runtime if we don't have it yet
                if (_notificationStackInstance == null && _addNotificationMethod != null)
                {
                    TryGetNotificationStackInstance();
                }

                // Use SendNotification(string text, int id, int amount, bool unique, bool error)
                if (_addNotificationMethod != null && _notificationStackInstance != null)
                {
                    try
                    {
                        string itemName = GetItemDisplayName(itemId);
                        // Parameters: text (item name), id (item ID), amount, unique (false = can stack), error (false = not an error)
                        _addNotificationMethod.Invoke(_notificationStackInstance, new object[] { itemName, itemId, amount, false, false });
                        Plugin.Log?.LogInfo($"[NOTIFY] Sent notification: {amount}x {itemName} (id={itemId})");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogWarning($"[NOTIFY] SendNotification failed: {ex.Message}");
                    }
                }

                // Fallback: log only
                string fallbackName = GetItemDisplayName(itemId);
                Plugin.Log?.LogInfo($"[NOTIFY] Auto-deposited {amount}x {fallbackName} to vault (notification unavailable)");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[NOTIFY] Error triggering notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to get the NotificationStack instance at runtime
        /// </summary>
        private static void TryGetNotificationStackInstance()
        {
            try
            {
                var notificationStackType = AccessTools.TypeByName("Wish.NotificationStack");
                if (notificationStackType == null) return;

                // Try SingletonBehaviour<NotificationStack>.Instance
                var singletonType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonType != null)
                {
                    var genericSingleton = singletonType.MakeGenericType(notificationStackType);
                    var instanceProp = AccessTools.Property(genericSingleton, "Instance");
                    if (instanceProp != null)
                    {
                        _notificationStackInstance = instanceProp.GetValue(null);
                        if (_notificationStackInstance != null)
                        {
                            Plugin.Log?.LogInfo("[NOTIFY] Got NotificationStack instance at runtime via SingletonBehaviour");
                            return;
                        }
                    }
                }

                // Try FindObjectOfType as fallback
                var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", Type.EmptyTypes);
                if (findMethod != null)
                {
                    var genericFind = findMethod.MakeGenericMethod(notificationStackType);
                    _notificationStackInstance = genericFind.Invoke(null, null);
                    if (_notificationStackInstance != null)
                    {
                        Plugin.Log?.LogInfo("[NOTIFY] Got NotificationStack instance at runtime via FindObjectOfType");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[NOTIFY] Failed to get NotificationStack instance: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize the notification system by finding the correct types and methods.
        /// Sun Haven uses: NotificationStack.SendNotification(string text, int id, int amount, bool unique, bool error)
        /// Access via SingletonBehaviour&lt;NotificationStack&gt;.Instance
        /// </summary>
        private static void InitializeNotificationSystem()
        {
            try
            {
                Plugin.Log?.LogInfo("[NOTIFY] Initializing notification system...");

                // NotificationStack is accessed via SingletonBehaviour<NotificationStack>.Instance
                // The method signature is: SendNotification(string text, int id, int amount, bool unique, bool error)
                var notificationStackType = AccessTools.TypeByName("Wish.NotificationStack");
                if (notificationStackType == null)
                {
                    Plugin.Log?.LogWarning("[NOTIFY] NotificationStack type not found");
                    return;
                }

                Plugin.Log?.LogInfo($"[NOTIFY] Found NotificationStack type: {notificationStackType.FullName}");

                // Try to get instance via SingletonBehaviour<NotificationStack>.Instance
                var singletonType = AccessTools.TypeByName("Wish.SingletonBehaviour`1");
                if (singletonType != null)
                {
                    var genericSingleton = singletonType.MakeGenericType(notificationStackType);
                    var instanceProp = AccessTools.Property(genericSingleton, "Instance");
                    if (instanceProp != null)
                    {
                        _notificationStackInstance = instanceProp.GetValue(null);
                        Plugin.Log?.LogInfo($"[NOTIFY] Got NotificationStack via SingletonBehaviour: {(_notificationStackInstance != null ? "success" : "null")}");
                    }
                }

                // Fallback: try direct Instance property
                if (_notificationStackInstance == null)
                {
                    var directInstanceProp = AccessTools.Property(notificationStackType, "Instance");
                    if (directInstanceProp != null)
                    {
                        _notificationStackInstance = directInstanceProp.GetValue(null);
                        Plugin.Log?.LogInfo($"[NOTIFY] Got NotificationStack via direct Instance: {(_notificationStackInstance != null ? "success" : "null")}");
                    }
                }

                // Fallback: try to find it via UnityEngine.Object.FindObjectOfType
                if (_notificationStackInstance == null)
                {
                    var findMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", new Type[0]);
                    if (findMethod != null)
                    {
                        var genericFind = findMethod.MakeGenericMethod(notificationStackType);
                        _notificationStackInstance = genericFind.Invoke(null, null);
                        Plugin.Log?.LogInfo($"[NOTIFY] Got NotificationStack via FindObjectOfType: {(_notificationStackInstance != null ? "success" : "null")}");
                    }
                }

                if (_notificationStackInstance == null)
                {
                    Plugin.Log?.LogWarning("[NOTIFY] Could not get NotificationStack instance - will try at runtime");
                }

                // Find SendNotification method: SendNotification(string text, int id, int amount, bool unique, bool error)
                _addNotificationMethod = AccessTools.Method(notificationStackType, "SendNotification",
                    new[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) });

                if (_addNotificationMethod != null)
                {
                    Plugin.Log?.LogInfo("[NOTIFY] Found SendNotification(string, int, int, bool, bool) method");
                }
                else
                {
                    Plugin.Log?.LogWarning("[NOTIFY] Could not find SendNotification method");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[NOTIFY] Error initializing notification system: {ex.Message}");
            }
        }

        /// <summary>
        /// POSTFIX patch for Inventory.AddItem(Item, int, int, bool, bool, bool).
        /// This is kept as a backup/fallback in case the PREFIX doesn't fire or returns true.
        /// NOTE: If PREFIX handled the deposit (returned false), this POSTFIX should NOT run because
        /// the original method was skipped. However, we keep the WasRecentlyDeposited check as a safety measure.
        /// Signature: AddItem(Item item, int amount, int slot, bool sendNotification, bool specialItem, bool superSecretCheck)
        /// </summary>
        public static void OnInventoryAddItemObjectPostfix(object __instance, object item, int amount, int slot, bool sendNotification, bool specialItem, bool superSecretCheck)
        {
            // Get the item ID from the Item object first so we can use item-specific tracking
            int itemId = GetItemId(item);

            try
            {
                // IMPORTANT: If PREFIX already handled this item and returned false, this POSTFIX should not run.
                // But if it somehow does run (e.g., PREFIX returned true), check if it was recently deposited.
                if (WasRecentlyDeposited(itemId))
                {
                    Plugin.Log?.LogInfo($"[POSTFIX] Item {itemId} was recently deposited by PREFIX, skipping");
                    return;
                }

                // Prevent recursive calls when we're doing withdrawals or other operations for THIS specific item
                if (IsProcessingAutoDeposit(itemId)) return;

                Plugin.Log?.LogInfo($"OnInventoryAddItemObjectPostfix called: itemId={itemId}, amount={amount}, sendNotification={sendNotification}");

                if (!ShouldAutoDeposit(itemId))
                {
                    // Not registered for auto-deposit
                    return;
                }

                string currencyId = GetCurrencyForItem(itemId);
                if (string.IsNullOrEmpty(currencyId))
                {
                    Plugin.Log?.LogWarning($"No currency ID found for item {itemId}");
                    return;
                }

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null)
                {
                    Plugin.Log?.LogWarning("VaultManager is null");
                    return;
                }

                Plugin.Log?.LogInfo($"Auto-depositing {amount} of item {itemId} as {currencyId} (via Inventory.AddItem POSTFIX)");

                // Mark as deposited IMMEDIATELY before any processing to prevent duplicate calls
                // that happen on the same frame/call stack
                MarkAsDeposited(itemId);

                StartProcessingAutoDeposit(itemId);
                try
                {
                    // The item is now in inventory (and notification was shown if sendNotification was true)
                    // Now remove it from inventory and add to vault

                    // IMPORTANT: We must use the PLAYER's inventory, not __instance which might be
                    // a different inventory (chest, shop, etc.). The postfix fires on any Inventory.AddItem call.
                    object playerInventory = GetPlayerInventory();
                    if (playerInventory == null)
                    {
                        Plugin.Log?.LogWarning("[DEBUG] Could not get player inventory - using __instance as fallback");
                        playerInventory = __instance;
                    }
                    else
                    {
                        Plugin.Log?.LogInfo($"[DEBUG] Using player inventory (type: {playerInventory.GetType().Name}) instead of __instance (type: {__instance.GetType().Name})");
                    }

                    var invType = playerInventory.GetType();

                    // Get inventory count BEFORE removal for logging
                    // Use _skipVaultInGetAmount to get raw inventory count without vault additions
                    int countBefore = -1;
                    var getAmountMethod = AccessTools.Method(invType, "GetAmount", new[] { typeof(int) });
                    if (getAmountMethod != null)
                    {
                        _skipVaultInGetAmount = true;
                        try
                        {
                            countBefore = (int)getAmountMethod.Invoke(playerInventory, new object[] { itemId });
                            Plugin.Log?.LogInfo($"[DEBUG] Player inventory count BEFORE removal: {countBefore} of item {itemId} (raw, no vault)");
                        }
                        finally
                        {
                            _skipVaultInGetAmount = false;
                        }
                    }

                    // Remove from player's inventory using RemoveAll which is simpler and avoids parameter issues
                    // Try RemoveAll(int id) first - this removes all of a specific item type
                    var removeAllMethod = AccessTools.Method(invType, "RemoveAll", new[] { typeof(int) });
                    if (removeAllMethod != null)
                    {
                        Plugin.Log?.LogInfo($"[DEBUG] Calling RemoveAll({itemId}) on player inventory...");
                        removeAllMethod.Invoke(playerInventory, new object[] { itemId });
                        Plugin.Log?.LogInfo($"[DEBUG] RemoveAll({itemId}) completed on player inventory");
                    }
                    else
                    {
                        // Fallback to RemoveItem(int, int, int)
                        var removeMethod = AccessTools.Method(invType, "RemoveItem", new[] { typeof(int), typeof(int), typeof(int) });
                        if (removeMethod != null)
                        {
                            try
                            {
                                Plugin.Log?.LogInfo($"[DEBUG] Calling RemoveItem({itemId}, {amount}, -1) on player inventory...");
                                removeMethod.Invoke(playerInventory, new object[] { itemId, amount, -1 });
                                Plugin.Log?.LogInfo($"[DEBUG] RemoveItem completed on player inventory");
                            }
                            catch (Exception removeEx)
                            {
                                // Get inner exception for better error message
                                var innerEx = removeEx.InnerException ?? removeEx;
                                Plugin.Log?.LogError($"RemoveItem failed: {innerEx.Message}");
                                return;
                            }
                        }
                        else
                        {
                            Plugin.Log?.LogWarning("Could not find RemoveItem or RemoveAll method on inventory");
                            return;
                        }
                    }

                    // Get inventory count AFTER removal to verify it worked
                    int countAfter = -1;
                    if (getAmountMethod != null)
                    {
                        _skipVaultInGetAmount = true;
                        try
                        {
                            countAfter = (int)getAmountMethod.Invoke(playerInventory, new object[] { itemId });
                            Plugin.Log?.LogInfo($"[DEBUG] Player inventory count AFTER removal: {countAfter} of item {itemId} (raw, no vault)");
                        }
                        finally
                        {
                            _skipVaultInGetAmount = false;
                        }

                        // Check if removal actually happened
                        if (countBefore >= 0 && countAfter >= 0)
                        {
                            int removed = countBefore - countAfter;
                            Plugin.Log?.LogInfo($"[DEBUG] Items actually removed from player inventory: {removed} (expected: {amount})");

                            if (removed == 0)
                            {
                                Plugin.Log?.LogWarning($"[DEBUG] WARNING: No items were removed from player inventory! RemoveAll/RemoveItem may not have worked.");
                            }
                            else if (removed != amount && removed != countBefore)
                            {
                                Plugin.Log?.LogWarning($"[DEBUG] WARNING: Removed {removed} items but expected {amount}");
                            }
                        }
                    }

                    // Add to vault (already marked as deposited at the start of processing)
                    AddCurrencyToVault(vaultManager, currencyId, amount);
                    Plugin.Log?.LogInfo($"Auto-deposited {amount} of item {itemId} as {currencyId} to vault");
                }
                finally
                {
                    StopProcessingAutoDeposit(itemId);
                }
            }
            catch (Exception ex)
            {
                StopProcessingAutoDeposit(itemId);
                // Get inner exception for better error message
                var innerEx = ex.InnerException ?? ex;
                Plugin.Log?.LogError($"Error in OnInventoryAddItemObjectPostfix: {innerEx.Message}\n{innerEx.StackTrace}");
            }
        }

        /// <summary>
        /// Get the player's inventory object. This ensures we always operate on the player's
        /// actual inventory rather than some other inventory instance (chest, shop, etc.)
        /// </summary>
        private static object GetPlayerInventory()
        {
            try
            {
                // Get LocalPlayer via reflection
                var playerType = typeof(Wish.Player);
                var localPlayerProperty = AccessTools.Property(playerType, "LocalPlayer");
                if (localPlayerProperty == null)
                {
                    // Try Instance as fallback
                    localPlayerProperty = AccessTools.Property(playerType, "Instance");
                }
                if (localPlayerProperty == null)
                {
                    Plugin.Log?.LogWarning("[DEBUG] GetPlayerInventory: Could not find LocalPlayer or Instance property");
                    return null;
                }

                var player = localPlayerProperty.GetValue(null);
                if (player == null)
                {
                    Plugin.Log?.LogWarning("[DEBUG] GetPlayerInventory: LocalPlayer is null");
                    return null;
                }

                // Try various property/field names for the inventory
                var inventoryProperty = AccessTools.Property(playerType, "Inventory");
                if (inventoryProperty != null)
                {
                    var inv = inventoryProperty.GetValue(player);
                    if (inv != null)
                    {
                        Plugin.Log?.LogInfo($"[DEBUG] GetPlayerInventory: Found via Inventory property");
                        return inv;
                    }
                }

                var playerInventoryProperty = AccessTools.Property(playerType, "PlayerInventory");
                if (playerInventoryProperty != null)
                {
                    var inv = playerInventoryProperty.GetValue(player);
                    if (inv != null)
                    {
                        Plugin.Log?.LogInfo($"[DEBUG] GetPlayerInventory: Found via PlayerInventory property");
                        return inv;
                    }
                }

                // Try fields
                var inventoryField = AccessTools.Field(playerType, "inventory");
                if (inventoryField == null)
                    inventoryField = AccessTools.Field(playerType, "_inventory");
                if (inventoryField == null)
                    inventoryField = AccessTools.Field(playerType, "playerInventory");
                if (inventoryField == null)
                    inventoryField = AccessTools.Field(playerType, "_playerInventory");

                if (inventoryField != null)
                {
                    var inv = inventoryField.GetValue(player);
                    if (inv != null)
                    {
                        Plugin.Log?.LogInfo($"[DEBUG] GetPlayerInventory: Found via {inventoryField.Name} field");
                        return inv;
                    }
                }

                Plugin.Log?.LogWarning("[DEBUG] GetPlayerInventory: Could not find inventory on player");
                return null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[DEBUG] GetPlayerInventory error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the item ID from a Wish.Item object using reflection.
        /// </summary>
        private static int GetItemId(object item)
        {
            if (item == null)
            {
                Plugin.Log?.LogWarning("GetItemId: item is null");
                return -1;
            }

            try
            {
                var itemType = item.GetType();
                Plugin.Log?.LogInfo($"GetItemId: item type is {itemType.FullName}");

                // Log all members to help debug
                var fields = itemType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var f in fields)
                {
                    if (f.Name.ToLowerInvariant().Contains("id"))
                    {
                        try
                        {
                            var val = f.GetValue(item);
                            Plugin.Log?.LogInfo($"  Field: {f.Name} ({f.FieldType.Name}) = {val}");
                        }
                        catch { }
                    }
                }

                var props = itemType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var p in props)
                {
                    if (p.Name.ToLowerInvariant().Contains("id"))
                    {
                        try
                        {
                            var val = p.GetValue(item);
                            Plugin.Log?.LogInfo($"  Property: {p.Name} ({p.PropertyType.Name}) = {val}");
                        }
                        catch { }
                    }
                }

                // Try id field (lowercase) - common in Unity
                var idField = AccessTools.Field(itemType, "id");
                if (idField != null)
                {
                    var result = idField.GetValue(item);
                    Plugin.Log?.LogInfo($"Found id field, value: {result}");
                    if (result is int id) return id;
                }

                // Try ID() method
                var idMethod = AccessTools.Method(itemType, "ID");
                if (idMethod != null)
                {
                    var result = idMethod.Invoke(item, null);
                    Plugin.Log?.LogInfo($"Found ID() method, value: {result}");
                    if (result is int id) return id;
                }

                // Try id property (lowercase)
                var idProperty = AccessTools.Property(itemType, "id");
                if (idProperty != null)
                {
                    var result = idProperty.GetValue(item);
                    Plugin.Log?.LogInfo($"Found id property, value: {result}");
                    if (result is int id) return id;
                }

                // Try Id property (PascalCase)
                idProperty = AccessTools.Property(itemType, "Id");
                if (idProperty != null)
                {
                    var result = idProperty.GetValue(item);
                    Plugin.Log?.LogInfo($"Found Id property, value: {result}");
                    if (result is int id) return id;
                }

                // Try _id field
                idField = AccessTools.Field(itemType, "_id");
                if (idField != null)
                {
                    var result = idField.GetValue(item);
                    Plugin.Log?.LogInfo($"Found _id field, value: {result}");
                    if (result is int id) return id;
                }

                // Try ItemID property
                idProperty = AccessTools.Property(itemType, "ItemID");
                if (idProperty != null)
                {
                    var result = idProperty.GetValue(item);
                    Plugin.Log?.LogInfo($"Found ItemID property, value: {result}");
                    if (result is int id) return id;
                }

                Plugin.Log?.LogWarning($"Could not find ID on item type {itemType.FullName}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting item ID: {ex.Message}\n{ex.StackTrace}");
            }

            return -1;
        }

        #region Inventory Operations

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

                // Try to find AddItem method
                var addMethod = AccessTools.Method(playerType, "AddItem", new[] { typeof(int), typeof(int) });
                if (addMethod != null)
                {
                    var result = addMethod.Invoke(player, new object[] { itemId, amount });
                    return result is bool b && b;
                }

                // Try alternate method
                addMethod = AccessTools.Method(playerType, "AddItemToInventory", new[] { typeof(int), typeof(int) });
                if (addMethod != null)
                {
                    var result = addMethod.Invoke(player, new object[] { itemId, amount });
                    return result is bool b && b;
                }

                // Try getting inventory
                var inventoryProperty = AccessTools.Property(playerType, "Inventory");
                if (inventoryProperty != null)
                {
                    var inventory = inventoryProperty.GetValue(player);
                    if (inventory != null)
                    {
                        var invAddMethod = AccessTools.Method(inventory.GetType(), "AddItem", new[] { typeof(int), typeof(int) });
                        if (invAddMethod != null)
                        {
                            var result = invAddMethod.Invoke(inventory, new object[] { itemId, amount });
                            return result is bool b && b;
                        }
                    }
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
            else if (currencyId.StartsWith("key_"))
            {
                vaultManager.AddKeys(currencyId.Substring("key_".Length), amount);
            }
            else if (currencyId.StartsWith("ticket_"))
            {
                vaultManager.AddTickets(currencyId.Substring("ticket_".Length), amount);
            }
            else if (currencyId.StartsWith("pirate_"))
            {
                vaultManager.AddTickets(currencyId.Substring("pirate_".Length), amount);
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
            else if (currencyId.StartsWith("key_"))
            {
                return vaultManager.RemoveKeys(currencyId.Substring("key_".Length), amount);
            }            
            else if (currencyId.StartsWith("pirate_"))
            {
                return vaultManager.RemoveTickets(currencyId.Substring("pirate_".Length), amount);
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
            else if (currencyId.StartsWith("key_"))
            {
                return vaultManager.GetKeys(currencyId.Substring("key_".Length));
            }
            else if (currencyId.StartsWith("ticket_"))
            {
                return vaultManager.GetTickets(currencyId.Substring("ticket_".Length));
            }
            else if (currencyId.StartsWith("pirate_"))
            {
                return vaultManager.GetTickets(currencyId.Substring("pirate_".Length));
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
        /// Log all registered item-currency mappings (for debugging)
        /// </summary>
        public static void LogAllMappings()
        {
            Plugin.Log?.LogInfo("[DEBUG] === Item-Currency Mappings ===");
            foreach (var kvp in _itemToCurrency)
            {
                bool autoDeposit = _currencyAutoDeposit.TryGetValue(kvp.Value, out var enabled) && enabled;
                Plugin.Log?.LogInfo($"  Item {kvp.Key} -> {kvp.Value} (auto-deposit: {autoDeposit})");
            }
            Plugin.Log?.LogInfo($"[DEBUG] Total mappings: {_itemToCurrency.Count}");
            Plugin.Log?.LogInfo($"[DEBUG] Auto-deposit enabled: {AutoDepositEnabled}");
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
                // Skip vault logic when we're doing auto-deposit removal for this specific item
                if (IsProcessingAutoDeposit(id)) return;

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
                // Skip vault logic when we're doing auto-deposit removal for this specific item
                if (IsProcessingAutoDeposit(id)) return;

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
        private static void ShowAutoDepositNotification(string currencyId, int amount)
        {
            try
            {
                int itemId = GetItemForCurrency(currencyId);
                if (itemId < 0)
                {
                    Plugin.Log?.LogWarning($"ShowAutoDepositNotification: No item ID for currency {currencyId}");
                    return;
                }

                Plugin.Log?.LogInfo($"ShowAutoDepositNotification: Attempting notification for item {itemId} x{amount}");

                // Method 1: Try SingletonBehaviour<Notifications>.Instance.SendNotification (most common in Sun Haven)
                if (TrySendViaNotifications(itemId, amount)) return;

                // Method 2: Try NotificationStack
                if (TrySendViaNotificationStack(itemId, amount)) return;

                // Method 3: Try UIHandler or similar UI manager
                if (TrySendViaUIHandler(itemId, amount)) return;

                // Method 4: Try Player notification methods
                if (TrySendViaPlayer(itemId, amount)) return;

                // Method 5: Search all loaded types for notification-related singletons
                if (TrySendViaReflectionSearch(itemId, amount)) return;

                Plugin.Log?.LogWarning("Could not find a notification method - item deposited silently");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error showing auto-deposit notification: {ex.Message}");
            }
        }

        private static bool TrySendViaNotifications(int itemId, int amount)
        {
            try
            {
                var notificationsType = AccessTools.TypeByName("Wish.Notifications");
                if (notificationsType == null) return false;

                var instanceProp = AccessTools.Property(notificationsType, "Instance");
                if (instanceProp == null) return false;

                var instance = instanceProp.GetValue(null);
                if (instance == null) return false;

                // Log available methods
                var methods = notificationsType.GetMethods();
                foreach (var m in methods)
                {
                    if (m.Name.ToLower().Contains("notif") || m.Name.ToLower().Contains("send") || m.Name.ToLower().Contains("item"))
                    {
                        var parms = m.GetParameters();
                        string parmStr = string.Join(", ", System.Linq.Enumerable.Select(parms, p => $"{p.ParameterType.Name}"));
                        Plugin.Log?.LogInfo($"  Notifications method: {m.Name}({parmStr})");
                    }
                }

                // Try SendNotification with Item object
                var itemType = AccessTools.TypeByName("Wish.Item");
                if (itemType != null)
                {
                    var sendItemObjMethod = AccessTools.Method(notificationsType, "SendNotification", new[] { itemType, typeof(int) });
                    if (sendItemObjMethod != null)
                    {
                        var itemObj = GetItemObject(itemId);
                        if (itemObj != null)
                        {
                            sendItemObjMethod.Invoke(instance, new object[] { itemObj, amount });
                            Plugin.Log?.LogInfo($"Sent Notifications.SendNotification(Item, int) for {amount}x item {itemId}");
                            return true;
                        }
                    }
                }

                // Try SendNotification(int, int)
                var sendMethod = AccessTools.Method(notificationsType, "SendNotification", new[] { typeof(int), typeof(int) });
                if (sendMethod != null)
                {
                    sendMethod.Invoke(instance, new object[] { itemId, amount });
                    Plugin.Log?.LogInfo($"Sent Notifications.SendNotification(int, int) for {amount}x item {itemId}");
                    return true;
                }

                // Try SendItemNotification
                var sendItemMethod = AccessTools.Method(notificationsType, "SendItemNotification", new[] { typeof(int), typeof(int) });
                if (sendItemMethod != null)
                {
                    sendItemMethod.Invoke(instance, new object[] { itemId, amount });
                    Plugin.Log?.LogInfo($"Sent Notifications.SendItemNotification for {amount}x item {itemId}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"TrySendViaNotifications error: {ex.Message}");
            }
            return false;
        }

        private static bool TrySendViaNotificationStack(int itemId, int amount)
        {
            try
            {
                var stackType = AccessTools.TypeByName("Wish.NotificationStack");
                if (stackType == null) return false;

                var instanceProp = AccessTools.Property(stackType, "Instance");
                if (instanceProp == null) return false;

                var instance = instanceProp.GetValue(null);
                if (instance == null) return false;

                // Try with Item object first
                var itemType = AccessTools.TypeByName("Wish.Item");
                if (itemType != null)
                {
                    var sendItemMethod = AccessTools.Method(stackType, "SendNotification", new[] { itemType, typeof(int) });
                    if (sendItemMethod != null)
                    {
                        var itemObj = GetItemObject(itemId);
                        if (itemObj != null)
                        {
                            sendItemMethod.Invoke(instance, new object[] { itemObj, amount });
                            Plugin.Log?.LogInfo($"Sent NotificationStack.SendNotification(Item, int) for {amount}x item {itemId}");
                            return true;
                        }
                    }
                }

                // Try int, int
                var sendIntMethod = AccessTools.Method(stackType, "SendNotification", new[] { typeof(int), typeof(int) });
                if (sendIntMethod != null)
                {
                    sendIntMethod.Invoke(instance, new object[] { itemId, amount });
                    Plugin.Log?.LogInfo($"Sent NotificationStack.SendNotification(int, int) for {amount}x item {itemId}");
                    return true;
                }

                // Fallback to string
                var sendStringMethod = AccessTools.Method(stackType, "SendNotification", new[] { typeof(string) });
                if (sendStringMethod != null)
                {
                    string itemName = GetItemDisplayName(itemId);
                    sendStringMethod.Invoke(instance, new object[] { $"+{amount} {itemName}" });
                    Plugin.Log?.LogInfo($"Sent NotificationStack string notification: +{amount} {itemName}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"TrySendViaNotificationStack error: {ex.Message}");
            }
            return false;
        }

        private static bool TrySendViaUIHandler(int itemId, int amount)
        {
            try
            {
                // Try various UI handler types
                string[] uiTypes = { "Wish.UIHandler", "Wish.GameUI", "Wish.HUD", "Wish.PickupUI", "Wish.ItemPickupUI" };
                foreach (var typeName in uiTypes)
                {
                    var uiType = AccessTools.TypeByName(typeName);
                    if (uiType == null) continue;

                    var instanceProp = AccessTools.Property(uiType, "Instance");
                    if (instanceProp == null) continue;

                    var instance = instanceProp.GetValue(null);
                    if (instance == null) continue;

                    // Look for pickup/notification methods
                    var methods = uiType.GetMethods();
                    foreach (var method in methods)
                    {
                        if (method.Name.ToLower().Contains("pickup") || method.Name.ToLower().Contains("item") && method.Name.ToLower().Contains("notif"))
                        {
                            var parms = method.GetParameters();
                            if (parms.Length >= 2 && parms[0].ParameterType == typeof(int) && parms[1].ParameterType == typeof(int))
                            {
                                method.Invoke(instance, new object[] { itemId, amount });
                                Plugin.Log?.LogInfo($"Sent {typeName}.{method.Name} for {amount}x item {itemId}");
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"TrySendViaUIHandler error: {ex.Message}");
            }
            return false;
        }

        private static bool TrySendViaPlayer(int itemId, int amount)
        {
            try
            {
                if (Wish.Player.Instance == null) return false;

                var playerType = typeof(Wish.Player);

                // Log player methods related to notifications/pickups
                var methods = playerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var m in methods)
                {
                    if (m.Name.ToLower().Contains("notif") || m.Name.ToLower().Contains("pickup") && !m.Name.ToLower().Contains("can"))
                    {
                        var parms = m.GetParameters();
                        string parmStr = string.Join(", ", System.Linq.Enumerable.Select(parms, p => $"{p.ParameterType.Name}"));
                        Plugin.Log?.LogInfo($"  Player method: {m.Name}({parmStr})");
                    }
                }

                // Try various method names
                string[] methodNames = { "SendPickupNotification", "ShowPickupNotification", "ItemPickedUp", "OnItemPickedUp", "PickupNotification" };
                foreach (var methodName in methodNames)
                {
                    var method = AccessTools.Method(playerType, methodName, new[] { typeof(int), typeof(int) });
                    if (method != null)
                    {
                        method.Invoke(Wish.Player.Instance, new object[] { itemId, amount });
                        Plugin.Log?.LogInfo($"Sent Player.{methodName} for {amount}x item {itemId}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"TrySendViaPlayer error: {ex.Message}");
            }
            return false;
        }

        private static bool TrySendViaReflectionSearch(int itemId, int amount)
        {
            try
            {
                // Search for any singleton with notification methods in Wish namespace
                var assembly = typeof(Wish.Player).Assembly;
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.Namespace?.StartsWith("Wish") == true) continue;
                    if (!type.Name.ToLower().Contains("notif") && !type.Name.ToLower().Contains("pickup")) continue;

                    var instanceProp = AccessTools.Property(type, "Instance");
                    if (instanceProp == null) continue;

                    var instance = instanceProp.GetValue(null);
                    if (instance == null) continue;

                    Plugin.Log?.LogInfo($"Found potential notification type: {type.FullName}");

                    // Try to find a send method
                    var methods = type.GetMethods();
                    foreach (var method in methods)
                    {
                        if (!method.Name.ToLower().Contains("send") && !method.Name.ToLower().Contains("show") && !method.Name.ToLower().Contains("add")) continue;

                        var parms = method.GetParameters();
                        if (parms.Length >= 2 && parms[0].ParameterType == typeof(int) && parms[1].ParameterType == typeof(int))
                        {
                            method.Invoke(instance, new object[] { itemId, amount });
                            Plugin.Log?.LogInfo($"Sent {type.Name}.{method.Name} for {amount}x item {itemId}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"TrySendViaReflectionSearch error: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Get an Item object from the game's database by ID.
        /// </summary>
        private static object GetItemObject(int itemId)
        {
            try
            {
                // Try Database.GetData<Item>(itemId)
                var databaseType = AccessTools.TypeByName("Wish.Database");
                if (databaseType != null)
                {
                    var itemType = AccessTools.TypeByName("Wish.Item");
                    if (itemType != null)
                    {
                        // Try GetData<T> generic method
                        var getDataMethod = databaseType.GetMethod("GetData", new[] { typeof(int) });
                        if (getDataMethod != null && getDataMethod.IsGenericMethod)
                        {
                            var genericMethod = getDataMethod.MakeGenericMethod(itemType);
                            var item = genericMethod.Invoke(null, new object[] { itemId });
                            if (item != null)
                            {
                                Plugin.Log?.LogInfo($"Got Item object via Database.GetData<Item>({itemId})");
                                return item;
                            }
                        }

                        // Try GetItem static method
                        var getItemMethod = AccessTools.Method(databaseType, "GetItem", new[] { typeof(int) });
                        if (getItemMethod != null)
                        {
                            var item = getItemMethod.Invoke(null, new object[] { itemId });
                            if (item != null)
                            {
                                Plugin.Log?.LogInfo($"Got Item object via Database.GetItem({itemId})");
                                return item;
                            }
                        }
                    }
                }

                // Try ItemDatabase.Instance.GetItem
                var itemDbType = AccessTools.TypeByName("Wish.ItemDatabase");
                if (itemDbType != null)
                {
                    var instanceProp = AccessTools.Property(itemDbType, "Instance");
                    if (instanceProp != null)
                    {
                        var instance = instanceProp.GetValue(null);
                        if (instance != null)
                        {
                            var getItemMethod = AccessTools.Method(itemDbType, "GetItem", new[] { typeof(int) });
                            if (getItemMethod != null)
                            {
                                var item = getItemMethod.Invoke(instance, new object[] { itemId });
                                if (item != null)
                                {
                                    Plugin.Log?.LogInfo($"Got Item object via ItemDatabase.GetItem({itemId})");
                                    return item;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"GetItemObject error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Get display name for an item ID using the game's item database.
        /// </summary>
        private static string GetItemDisplayName(int itemId)
        {
            try
            {
                // Try to get item name from database
                var databaseType = AccessTools.TypeByName("Wish.Database");
                if (databaseType != null)
                {
                    var getItemMethod = AccessTools.Method(databaseType, "GetItem", new[] { typeof(int) });
                    if (getItemMethod != null)
                    {
                        var item = getItemMethod.Invoke(null, new object[] { itemId });
                        if (item != null)
                        {
                            var nameProperty = AccessTools.Property(item.GetType(), "name");
                            if (nameProperty == null)
                                nameProperty = AccessTools.Property(item.GetType(), "Name");
                            if (nameProperty == null)
                                nameProperty = AccessTools.Property(item.GetType(), "itemName");
                            if (nameProperty == null)
                                nameProperty = AccessTools.Property(item.GetType(), "ItemName");

                            if (nameProperty != null)
                            {
                                var name = nameProperty.GetValue(item) as string;
                                if (!string.IsNullOrEmpty(name))
                                    return name;
                            }
                        }
                    }
                }

                // Try ItemDatabase
                var itemDbType = AccessTools.TypeByName("Wish.ItemDatabase");
                if (itemDbType != null)
                {
                    var instanceProp = AccessTools.Property(itemDbType, "Instance");
                    if (instanceProp != null)
                    {
                        var instance = instanceProp.GetValue(null);
                        if (instance != null)
                        {
                            var getItemMethod = AccessTools.Method(itemDbType, "GetItem", new[] { typeof(int) });
                            if (getItemMethod != null)
                            {
                                var item = getItemMethod.Invoke(instance, new object[] { itemId });
                                if (item != null)
                                {
                                    var nameProp = AccessTools.Property(item.GetType(), "name") ??
                                                   AccessTools.Property(item.GetType(), "Name") ??
                                                   AccessTools.Property(item.GetType(), "itemName");
                                    if (nameProp != null)
                                    {
                                        var name = nameProp.GetValue(item) as string;
                                        if (!string.IsNullOrEmpty(name))
                                            return name;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting item name: {ex.Message}");
            }

            // Fallback: format from currency ID
            string currencyId = GetCurrencyForItem(itemId);
            if (!string.IsNullOrEmpty(currencyId) && currencyId.Contains("_"))
            {
                string[] parts = currencyId.Split('_');
                if (parts.Length >= 2)
                {
                    string name = parts[1];
                    // Capitalize first letter
                    return char.ToUpper(name[0]) + name.Substring(1);
                }
            }

            return $"Item {itemId}";
        }
    }
}
