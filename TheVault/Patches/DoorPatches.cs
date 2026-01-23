using HarmonyLib;
using System;
using System.Collections.Generic;
using TheVault.Vault;

namespace TheVault.Patches
{
    /// <summary>
    /// Patches for door/gate interactions.
    /// Allows certain doors to require vault currencies (keys, tickets, etc.) to open.
    /// </summary>
    public static class DoorPatches
    {
        /// <summary>
        /// Maps door identifiers to their vault requirements.
        /// Format: doorId -> (currencyId, amount required, consumeOnUse)
        /// </summary>
        private static Dictionary<string, (string currencyId, int amount, bool consume)> _doorRequirements
            = new Dictionary<string, (string, int, bool)>();

        /// <summary>
        /// Register a door that requires vault currency to open.
        /// </summary>
        /// <param name="doorId">Unique identifier for the door (scene name + door name)</param>
        /// <param name="currencyId">The vault currency required (e.g., "key_dungeon")</param>
        /// <param name="amount">Amount required</param>
        /// <param name="consumeOnUse">Whether to consume the currency when door is opened</param>
        public static void RegisterDoorRequirement(string doorId, string currencyId, int amount, bool consumeOnUse = true)
        {
            _doorRequirements[doorId] = (currencyId, amount, consumeOnUse);
            Plugin.Log?.LogInfo($"Registered door requirement: {doorId} requires {amount} {currencyId}");
        }

        /// <summary>
        /// Remove a door requirement
        /// </summary>
        public static void RemoveDoorRequirement(string doorId)
        {
            _doorRequirements.Remove(doorId);
        }

        /// <summary>
        /// Clear all door requirements
        /// </summary>
        public static void ClearDoorRequirements()
        {
            _doorRequirements.Clear();
        }

        /// <summary>
        /// Check if a door can be opened based on vault requirements.
        /// Call this from door interaction patches.
        /// </summary>
        /// <returns>True if door can be opened, false otherwise</returns>
        public static bool CanOpenDoor(string doorId, out string failureReason)
        {
            failureReason = null;

            if (!_doorRequirements.TryGetValue(doorId, out var requirement))
            {
                return true; // No requirement registered, allow access
            }

            var vaultManager = Plugin.GetVaultManager();
            if (vaultManager == null)
            {
                Plugin.Log?.LogWarning("VaultManager not available for door check");
                return true; // Allow access if vault system unavailable
            }

            if (!vaultManager.HasCurrency(requirement.currencyId, requirement.amount))
            {
                string currencyName = GetCurrencyDisplayName(requirement.currencyId);
                failureReason = $"Requires {requirement.amount} {currencyName}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when a door is successfully opened.
        /// Consumes vault currency if configured to do so.
        /// </summary>
        public static void OnDoorOpened(string doorId)
        {
            if (!_doorRequirements.TryGetValue(doorId, out var requirement))
            {
                return;
            }

            if (!requirement.consume)
            {
                return; // Don't consume, just check
            }

            var vaultManager = Plugin.GetVaultManager();
            if (vaultManager == null) return;

            // Consume the currency
            DeductCurrency(vaultManager, requirement.currencyId, requirement.amount);
            Plugin.Log?.LogInfo($"Consumed {requirement.amount} {requirement.currencyId} to open {doorId}");
        }

        /// <summary>
        /// Prefix patch for door/gate interaction.
        /// The exact method to patch depends on Sun Haven's implementation.
        /// Common candidates: Door.Interact, Gate.Open, etc.
        /// </summary>
        public static bool OnBeforeDoorInteract(object __instance, ref bool __result)
        {
            try
            {
                string doorId = GetDoorId(__instance);
                if (string.IsNullOrEmpty(doorId)) return true;

                if (!CanOpenDoor(doorId, out string failureReason))
                {
                    ShowDoorLockedMessage(failureReason);
                    __result = false;
                    return false; // Block the interaction
                }

                return true; // Allow interaction
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnBeforeDoorInteract: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Postfix patch for successful door opening.
        /// </summary>
        public static void OnAfterDoorOpened(object __instance)
        {
            try
            {
                string doorId = GetDoorId(__instance);
                if (string.IsNullOrEmpty(doorId)) return;

                OnDoorOpened(doorId);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnAfterDoorOpened: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a unique identifier for a door instance
        /// </summary>
        private static string GetDoorId(object doorInstance)
        {
            try
            {
                // Try to get a unique identifier
                // This depends on Sun Haven's door implementation

                // Try name property
                var nameProperty = AccessTools.Property(doorInstance.GetType(), "name");
                if (nameProperty != null)
                {
                    var name = nameProperty.GetValue(doorInstance) as string;
                    if (!string.IsNullOrEmpty(name))
                    {
                        // Combine with current scene for uniqueness
                        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                        return $"{scene}_{name}";
                    }
                }

                // Try ID field
                var idField = AccessTools.Field(doorInstance.GetType(), "id");
                if (idField != null)
                {
                    return idField.GetValue(doorInstance)?.ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting door ID: {ex.Message}");
                return null;
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
        /// Show a message when door is locked
        /// </summary>
        private static void ShowDoorLockedMessage(string reason)
        {
            try
            {
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
                            sendMethod.Invoke(instance, new object[] { $"Locked: {reason}" });
                            return;
                        }
                    }
                }

                Plugin.Log?.LogInfo($"Door locked: {reason}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error showing door locked message: {ex.Message}");
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
