using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Wish;

namespace HavenDevTools.Services
{
    /// <summary>
    /// Service for tracking and displaying currency balances (integrates with The Vault)
    /// </summary>
    public class CurrencyTracker
    {
        // Known currency item IDs in Sun Haven
        public static readonly Dictionary<string, int> CurrencyItemIds = new Dictionary<string, int>
        {
            // Seasonal Tokens
            { "Spring Token", 18020 },
            { "Summer Token", 18021 },
            { "Fall Token", 18023 },
            { "Winter Token", 18022 },

            // Keys
            { "Copper Key", 1251 },
            { "Iron Key", 1252 },
            { "Adamant Key", 1253 },
            { "Mithril Key", 1254 },
            { "Sunite Key", 1255 },
            { "Glorite Key", 1256 },
            { "King's Lost Mine Key", 1257 },

            // Special
            { "Community Token", 18013 },
            { "Doubloon", 60014 },
            { "Black Bottle Cap", 60013 },
            { "Red Carnival Ticket", 18012 },
            { "Candy Corn Pieces", 18016 },
            { "Mana Shard", 18015 }
        };

        public CurrencyTracker()
        {
            Plugin.Log?.LogInfo("[CurrencyTracker] Initialized");
        }

        /// <summary>
        /// Get player's inventory count for a currency item
        /// </summary>
        public int GetInventoryAmount(int itemId)
        {
            try
            {
                if (Player.Instance?.Inventory == null) return 0;

                var inventory = Player.Instance.Inventory;
                var getAmountMethod = AccessTools.Method(inventory.GetType(), "GetAmount", new[] { typeof(int) });

                if (getAmountMethod != null)
                {
                    return (int)getAmountMethod.Invoke(inventory, new object[] { itemId });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CurrencyTracker] Error getting inventory amount: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Get vault amount for a currency (requires The Vault mod)
        /// </summary>
        public int GetVaultAmount(string currencyId)
        {
            if (!Plugin.HasTheVault) return 0;

            try
            {
                var vaultAssembly = GetVaultAssembly();
                if (vaultAssembly == null) return 0;

                var pluginType = vaultAssembly.GetType("TheVault.Plugin");
                if (pluginType == null) return 0;

                var getVaultManagerMethod = pluginType.GetMethod("GetVaultManager", BindingFlags.Public | BindingFlags.Static);
                if (getVaultManagerMethod == null) return 0;

                var vaultManager = getVaultManagerMethod.Invoke(null, null);
                if (vaultManager == null) return 0;

                var getCurrencyMethod = vaultManager.GetType().GetMethod("GetCurrency", new[] { typeof(string) });
                if (getCurrencyMethod != null)
                {
                    return (int)getCurrencyMethod.Invoke(vaultManager, new object[] { currencyId });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CurrencyTracker] Error getting vault amount: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Get all vault currencies (requires The Vault mod)
        /// </summary>
        public Dictionary<string, int> GetAllVaultCurrencies()
        {
            var result = new Dictionary<string, int>();

            if (!Plugin.HasTheVault) return result;

            try
            {
                var vaultAssembly = GetVaultAssembly();
                if (vaultAssembly == null) return result;

                var pluginType = vaultAssembly.GetType("TheVault.Plugin");
                if (pluginType == null) return result;

                var getVaultManagerMethod = pluginType.GetMethod("GetVaultManager", BindingFlags.Public | BindingFlags.Static);
                if (getVaultManagerMethod == null) return result;

                var vaultManager = getVaultManagerMethod.Invoke(null, null);
                if (vaultManager == null) return result;

                var getAllMethod = vaultManager.GetType().GetMethod("GetAllNonZeroCurrencies");
                if (getAllMethod != null)
                {
                    var currencies = getAllMethod.Invoke(vaultManager, null) as Dictionary<string, int>;
                    if (currencies != null)
                    {
                        return currencies;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CurrencyTracker] Error getting all vault currencies: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Get player's gold amount (stored in GameSave.Coins)
        /// </summary>
        public int GetGold()
        {
            try
            {
                // Gold is stored in GameSave.Coins (static property)
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                if (gameSaveType != null)
                {
                    var coinsProp = gameSaveType.GetProperty("Coins", BindingFlags.Public | BindingFlags.Static);
                    if (coinsProp != null)
                    {
                        return (int)coinsProp.GetValue(null);
                    }
                }

                // Fallback: try through SingletonBehaviour<GameSave>.Instance
                var singletonType = AccessTools.TypeByName("SingletonBehaviour`1");
                if (singletonType != null && gameSaveType != null)
                {
                    var genericType = singletonType.MakeGenericType(gameSaveType);
                    var instanceProp = genericType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProp != null)
                    {
                        var instance = instanceProp.GetValue(null);
                        if (instance != null)
                        {
                            var currentSaveProp = instance.GetType().GetProperty("CurrentSave");
                            if (currentSaveProp != null)
                            {
                                var currentSave = currentSaveProp.GetValue(instance);
                                if (currentSave != null)
                                {
                                    var coinsField = currentSave.GetType().GetField("coins", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (coinsField != null)
                                    {
                                        return Convert.ToInt32(coinsField.GetValue(currentSave));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CurrencyTracker] Error getting gold: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Get player's orb/ticket amounts
        /// </summary>
        public int GetOrbs()
        {
            try
            {
                if (Player.Instance == null) return 0;

                var orbsProp = Player.Instance.GetType().GetProperty("Orbs", BindingFlags.Public | BindingFlags.Instance);
                if (orbsProp != null)
                {
                    return (int)orbsProp.GetValue(Player.Instance);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CurrencyTracker] Error getting orbs: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Get player's tickets
        /// </summary>
        public int GetTickets()
        {
            try
            {
                if (Player.Instance == null) return 0;

                var ticketsProp = Player.Instance.GetType().GetProperty("Tickets", BindingFlags.Public | BindingFlags.Instance);
                if (ticketsProp != null)
                {
                    return (int)ticketsProp.GetValue(Player.Instance);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CurrencyTracker] Error getting tickets: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Get a summary of all currencies
        /// </summary>
        public CurrencySummary GetSummary()
        {
            var summary = new CurrencySummary
            {
                Gold = GetGold(),
                Orbs = GetOrbs(),
                Tickets = GetTickets(),
                InventoryCurrencies = new Dictionary<string, int>(),
                VaultCurrencies = new Dictionary<string, int>()
            };

            foreach (var kvp in CurrencyItemIds)
            {
                int amount = GetInventoryAmount(kvp.Value);
                if (amount > 0)
                {
                    summary.InventoryCurrencies[kvp.Key] = amount;
                }
            }

            if (Plugin.HasTheVault)
            {
                summary.VaultCurrencies = GetAllVaultCurrencies();
            }

            return summary;
        }

        private Assembly GetVaultAssembly()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "TheVault")
                {
                    return assembly;
                }
            }
            return null;
        }
    }

    public class CurrencySummary
    {
        public int Gold { get; set; }
        public int Orbs { get; set; }
        public int Tickets { get; set; }
        public Dictionary<string, int> InventoryCurrencies { get; set; }
        public Dictionary<string, int> VaultCurrencies { get; set; }
    }
}
