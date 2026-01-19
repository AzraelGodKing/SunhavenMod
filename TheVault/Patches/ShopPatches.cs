using HarmonyLib;
using System;
using System.Collections.Generic;
using TheVault.Vault;

namespace TheVault.Patches
{
    /// <summary>
    /// Patches for shop interactions.
    /// Allows shops to accept vault currencies as payment.
    /// </summary>
    public static class ShopPatches
    {
        /// <summary>
        /// Maps item IDs to vault currency requirements.
        /// This needs to be populated based on Sun Haven's item database.
        /// Format: itemId -> (currencyId, amount required)
        /// </summary>
        private static Dictionary<int, (string currencyId, int amount)> _vaultPurchaseRequirements
            = new Dictionary<int, (string, int)>();

        /// <summary>
        /// Register an item that requires vault currency to purchase.
        /// Call this during initialization to set up special shop items.
        /// </summary>
        public static void RegisterVaultPurchase(int itemId, string currencyId, int amount)
        {
            _vaultPurchaseRequirements[itemId] = (currencyId, amount);
            Plugin.Log?.LogInfo($"Registered vault purchase: Item {itemId} requires {amount} {currencyId}");
        }

        /// <summary>
        /// Clear all registered vault purchases
        /// </summary>
        public static void ClearVaultPurchases()
        {
            _vaultPurchaseRequirements.Clear();
        }

        /// <summary>
        /// Prefix patch for ShopMenu.BuyItem
        /// Checks if the item requires vault currency and handles the transaction.
        /// </summary>
        /// <returns>False to skip original method if handled by vault, true otherwise</returns>
        public static bool OnBeforeBuyItem(object __instance, ref bool __result)
        {
            try
            {
                // Get the item being purchased
                int itemId = GetCurrentItemId(__instance);
                if (itemId < 0) return true; // Let original method handle it

                // Check if this item requires vault currency
                if (!_vaultPurchaseRequirements.TryGetValue(itemId, out var requirement))
                {
                    return true; // Normal purchase, let original handle it
                }

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null)
                {
                    Plugin.Log?.LogWarning("VaultManager not available for purchase check");
                    return true;
                }

                // Check if player has enough vault currency
                if (!vaultManager.HasCurrency(requirement.currencyId, requirement.amount))
                {
                    Plugin.Log?.LogInfo($"Purchase blocked: insufficient {requirement.currencyId} (need {requirement.amount})");

                    // Show insufficient funds message
                    ShowInsufficientFundsMessage(requirement.currencyId, requirement.amount);

                    __result = false;
                    return false; // Skip original method
                }

                // Deduct vault currency
                // Note: The actual item giving is handled by the original method
                // We just need to intercept to check/deduct custom currency

                Plugin.Log?.LogInfo($"Vault purchase approved: {requirement.amount} {requirement.currencyId}");

                // Let original method continue - it will handle giving the item
                // We deduct currency in the postfix if purchase succeeds
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnBeforeBuyItem: {ex.Message}");
                return true; // Let original handle it on error
            }
        }

        /// <summary>
        /// Postfix patch for successful purchases - deducts vault currency after confirmation
        /// </summary>
        public static void OnAfterBuyItem(object __instance, bool __result)
        {
            if (!__result) return; // Purchase failed, don't deduct

            try
            {
                int itemId = GetCurrentItemId(__instance);
                if (itemId < 0) return;

                if (!_vaultPurchaseRequirements.TryGetValue(itemId, out var requirement))
                {
                    return; // Not a vault purchase
                }

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null) return;

                // Deduct the currency now that purchase is confirmed
                DeductCurrency(vaultManager, requirement.currencyId, requirement.amount);
                Plugin.Log?.LogInfo($"Deducted {requirement.amount} {requirement.currencyId} for purchase");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnAfterBuyItem: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current item ID from the shop menu instance
        /// </summary>
        private static int GetCurrentItemId(object shopMenu)
        {
            try
            {
                // Try to find the selected item ID
                // This depends on Sun Haven's ShopMenu implementation

                var selectedItemField = AccessTools.Field(shopMenu.GetType(), "selectedItemID");
                if (selectedItemField != null)
                {
                    return (int)selectedItemField.GetValue(shopMenu);
                }

                var selectedItemProperty = AccessTools.Property(shopMenu.GetType(), "SelectedItemID");
                if (selectedItemProperty != null)
                {
                    return (int)selectedItemProperty.GetValue(shopMenu);
                }

                // Try currentItem field
                var currentItemField = AccessTools.Field(shopMenu.GetType(), "currentItem");
                if (currentItemField != null)
                {
                    var item = currentItemField.GetValue(shopMenu);
                    if (item != null)
                    {
                        var idField = AccessTools.Field(item.GetType(), "id");
                        if (idField != null)
                        {
                            return (int)idField.GetValue(item);
                        }
                    }
                }

                return -1;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting item ID: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Deduct currency from the vault
        /// </summary>
        private static void DeductCurrency(VaultManager vaultManager, string currencyId, int amount)
        {
            if (currencyId.StartsWith("seasonal_"))
            {
                string typeName = currencyId.Substring("seasonal_".Length);
                if (Enum.TryParse<SeasonalTokenType>(typeName, out var tokenType))
                {
                    vaultManager.RemoveSeasonalTokens(tokenType, amount);
                }
            }
            else if (currencyId.StartsWith("community_"))
            {
                vaultManager.RemoveCommunityTokens(currencyId.Substring("community_".Length), amount);
            }
            else if (currencyId.StartsWith("key_"))
            {
                vaultManager.RemoveKeys(currencyId.Substring("key_".Length), amount);
            }
            else if (currencyId.StartsWith("ticket_"))
            {
                vaultManager.RemoveTickets(currencyId.Substring("ticket_".Length), amount);
            }
            else if (currencyId.StartsWith("orb_"))
            {
                vaultManager.RemoveOrbs(currencyId.Substring("orb_".Length), amount);
            }
            else if (currencyId.StartsWith("custom_"))
            {
                vaultManager.RemoveCustomCurrency(currencyId.Substring("custom_".Length), amount);
            }
        }

        /// <summary>
        /// Show a message when player doesn't have enough vault currency
        /// </summary>
        private static void ShowInsufficientFundsMessage(string currencyId, int required)
        {
            try
            {
                // Try to use Sun Haven's notification system
                var notificationType = AccessTools.TypeByName("Wish.NotificationStack");
                if (notificationType != null)
                {
                    var instanceProperty = AccessTools.Property(notificationType, "Instance");
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        var sendMethod = AccessTools.Method(notificationType, "SendNotification", new[] { typeof(string) });
                        if (sendMethod != null && instance != null)
                        {
                            string currencyName = GetCurrencyDisplayName(currencyId);
                            sendMethod.Invoke(instance, new object[] { $"Need {required} {currencyName}" });
                            return;
                        }
                    }
                }

                // Fallback to log
                Plugin.Log?.LogInfo($"Insufficient funds: need {required} {currencyId}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error showing notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Get display name for a currency ID
        /// </summary>
        private static string GetCurrencyDisplayName(string currencyId)
        {
            if (currencyId.StartsWith("seasonal_"))
                return currencyId.Substring("seasonal_".Length) + " Tokens";
            if (currencyId.StartsWith("community_"))
                return "Community Tokens";
            if (currencyId.StartsWith("key_"))
                return currencyId.Substring("key_".Length) + " Keys";
            if (currencyId.StartsWith("ticket_"))
                return currencyId.Substring("ticket_".Length) + " Tickets";

            return currencyId;
        }
    }
}
