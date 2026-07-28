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
        private static readonly Dictionary<string, string> _currencyDisplayNameCache = new Dictionary<string, string>(StringComparer.Ordinal);
        private static int _approvedVaultPurchaseItemId = -1;
        private static string _approvedVaultPurchaseCurrencyId;
        private static int _approvedVaultPurchaseAmount;

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
                    ClearApprovedVaultPurchase();
                    return false; // Skip original (Shop.BuyItem is void)
                }
                _approvedVaultPurchaseItemId = itemId;
                _approvedVaultPurchaseCurrencyId = requirement.currencyId;
                _approvedVaultPurchaseAmount = requirement.amount;
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
                if (itemId != _approvedVaultPurchaseItemId)
                    return;

                var vaultManager = Plugin.GetVaultManager();
                if (vaultManager == null)
                {
                    ClearApprovedVaultPurchase();
                    return;
                }

                // Deduct the currency now that purchase is confirmed
                if (!DeductCurrency(vaultManager, _approvedVaultPurchaseCurrencyId, _approvedVaultPurchaseAmount))
                {
                    Plugin.Log?.LogError($"Failed to deduct {_approvedVaultPurchaseAmount} {_approvedVaultPurchaseCurrencyId} after purchase; potential desync.");
                    ClearApprovedVaultPurchase();
                    return;
                }
                Plugin.Log?.LogInfo($"Deducted {_approvedVaultPurchaseAmount} {_approvedVaultPurchaseCurrencyId} for purchase");
                ClearApprovedVaultPurchase();
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
            return OnBeforeBuyItem(__instance, __0, 1);
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
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[ShopPatches] GetItemIdFromItemInfo: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Deduct currency from the vault
        /// </summary>
        private static bool DeductCurrency(VaultManager vaultManager, string currencyId, int amount)
        {
            if (TryExtractSuffix(currencyId, "seasonal_", out string seasonalSuffix))
            {
                if (Enum.TryParse<SeasonalTokenType>(seasonalSuffix, out var tokenType))
                {
                    return vaultManager.RemoveSeasonalTokens(tokenType, amount);
                }
                Plugin.Log?.LogWarning($"Invalid seasonal token type '{seasonalSuffix}' in currency id '{currencyId}'");
                return false;
            }
            else if (TryExtractSuffix(currencyId, "community_", out string communitySuffix))
            {
                return vaultManager.RemoveCommunityTokens(communitySuffix, amount);
            }
            else if (TryExtractSuffix(currencyId, "key_", out string keySuffix))
            {
                return vaultManager.RemoveKeys(keySuffix, amount);
            }
            else if (TryExtractSuffix(currencyId, "special_", out string specialSuffix))
            {
                return vaultManager.RemoveSpecial(specialSuffix, amount);
            }
            else if (TryExtractSuffix(currencyId, "orb_", out string orbSuffix))
            {
                return vaultManager.RemoveOrbs(orbSuffix, amount);
            }
            else if (TryExtractSuffix(currencyId, "custom_", out string customSuffix))
            {
                return vaultManager.RemoveCustomCurrency(customSuffix, amount);
            }
            else
            {
                Plugin.Log?.LogWarning($"Unknown vault currency id format '{currencyId}', skipping deduction");
                return false;
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
            if (string.IsNullOrEmpty(currencyId))
                return string.Empty;

            if (_currencyDisplayNameCache.TryGetValue(currencyId, out string cached))
                return cached;

            string displayName = currencyId;
            if (TryExtractSuffix(currencyId, "seasonal_", out string seasonalSuffix))
                displayName = seasonalSuffix + " Tokens";
            else if (TryExtractSuffix(currencyId, "community_", out _))
                displayName = "Community Tokens";
            else if (TryExtractSuffix(currencyId, "key_", out string keySuffix))
                displayName = keySuffix + " Keys";
            else if (TryExtractSuffix(currencyId, "special_", out string specialSuffix))
                displayName = FormatSpecialName(specialSuffix);
            else if (TryExtractSuffix(currencyId, "orb_", out string orbSuffix))
                displayName = orbSuffix + " Orbs";
            else if (TryExtractSuffix(currencyId, "custom_", out string customSuffix))
                displayName = customSuffix;

            _currencyDisplayNameCache[currencyId] = displayName;
            return displayName;
        }

        private static bool TryExtractSuffix(string currencyId, string prefix, out string suffix)
        {
            suffix = string.Empty;
            if (string.IsNullOrEmpty(currencyId))
            {
                Plugin.Log?.LogWarning("Encountered empty currency id");
                return false;
            }

            if (!currencyId.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            suffix = currencyId.Remove(0, prefix.Length);
            return true;
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

        private static void ClearApprovedVaultPurchase()
        {
            _approvedVaultPurchaseItemId = -1;
            _approvedVaultPurchaseCurrencyId = null;
            _approvedVaultPurchaseAmount = 0;
        }
    }
}
