using HarmonyLib;
using System;
using System.Collections.Generic;
using TheVault.Vault;
using Wish;

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
        /// Prefix for Wish.Shop.BuyItem(ShopItemInfo2, int) and BuyItem(ShopLoot2, int). Item info is first arg.
        /// </summary>
        public static bool OnBeforeBuyItem(object __instance, object __0, int __1)
        {
            try
            {
                int itemId = GetItemIdFromItemInfo(__0);
                if (itemId < 0) return true;
                if (!_vaultPurchaseRequirements.TryGetValue(itemId, out var requirement))
                    return true;
                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null)
                {
                    Plugin.Log?.LogWarning("VaultManager not available for purchase check");
                    return true;
                }
                if (!vaultManager.HasCurrency(requirement.currencyId, requirement.amount))
                {
                    Plugin.Log?.LogInfo($"Purchase blocked: insufficient {requirement.currencyId} (need {requirement.amount})");
                    ShowInsufficientFundsMessage(requirement.currencyId, requirement.amount);
                    return false; // Skip original (Shop.BuyItem is void)
                }
                Plugin.Log?.LogInfo($"Vault purchase approved: {requirement.amount} {requirement.currencyId}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnBeforeBuyItem: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Postfix for Shop.BuyItem - deducts vault currency after successful purchase. Item info is first arg.
        /// </summary>
        public static void OnAfterBuyItem(object __instance, object __0, int __1)
        {
            try
            {
                int itemId = GetItemIdFromItemInfo(__0);
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
        /// Postfix for Shop.BuyItem(ShopLoot2) single-quantity overload — same vault deduction as <see cref="OnAfterBuyItem"/>.
        /// </summary>
        public static void OnAfterBuyItemSingle(object __instance, object __0)
        {
            OnAfterBuyItem(__instance, __0, 1);
        }

        /// <summary>
        /// Prefix for Wish.Shop.BuyItem(ShopLoot2) - single arg is item info.
        /// </summary>
        public static bool OnBeforeBuyItemSingle(object __instance, object __0)
        {
            try
            {
                int itemId = GetItemIdFromItemInfo(__0);
                if (itemId < 0) return true;
                if (!_vaultPurchaseRequirements.TryGetValue(itemId, out var requirement))
                    return true;
                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null) { Plugin.Log?.LogWarning("VaultManager not available"); return true; }
                if (!vaultManager.HasCurrency(requirement.currencyId, requirement.amount))
                {
                    ShowInsufficientFundsMessage(requirement.currencyId, requirement.amount);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnBeforeBuyItemSingle: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Get item ID from ShopItemInfo2 or ShopLoot2 (first argument of Shop.BuyItem).
        /// </summary>
        private static int GetItemIdFromItemInfo(object itemInfo)
        {
            if (itemInfo == null) return -1;
            try
            {
                if (itemInfo is ShopItemInfo2 si)
                    return si.id;
                if (itemInfo is ShopLoot2 sl)
                    return sl.id;
                var idField = AccessTools.Field(itemInfo.GetType(), "id");
                if (idField != null)
                    return (int)idField.GetValue(itemInfo);
                var idProp = AccessTools.Property(itemInfo.GetType(), "id");
                if (idProp != null)
                    return (int)idProp.GetValue(itemInfo);
                return -1;
            }
            catch { return -1; }
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
            else if (currencyId.StartsWith("special_"))
            {
                vaultManager.RemoveSpecial(currencyId.Substring("special_".Length), amount);
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
                string currencyName = GetCurrencyDisplayName(currencyId);
                string msg = $"Need {required} {currencyName}";
                if (NotificationStack.Instance != null)
                {
                    NotificationStack.Instance.SendNotification(msg);
                    return;
                }
                Plugin.Log?.LogInfo($"Insufficient funds: {msg} ({currencyId})");
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
            if (currencyId.StartsWith("special_"))
                return FormatSpecialName(currencyId.Substring("special_".Length));

            return currencyId;
        }

        private static string FormatSpecialName(string specialName)
        {
            return specialName switch
            {
                "doubloon" => "Doubloons",
                "blackbottlecap" => "Black Bottle Caps",
                "redcarnivalticket" => "Red Carnival Tickets",
                "candycornpieces" => "Candy Corn Pieces",
                "manashard" => "Mana Shards",
                _ => specialName
            };
        }
    }
}
