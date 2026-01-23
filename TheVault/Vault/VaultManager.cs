using System;
using System.Collections.Generic;

namespace TheVault.Vault
{
    /// <summary>
    /// Core manager for vault operations. Handles all currency transactions
    /// and provides the API for other systems to check/modify vault values.
    /// </summary>
    public class VaultManager
    {
        private VaultData _vaultData;
        private Dictionary<string, CurrencyDefinition> _currencyDefinitions;
        private bool _isDirty;

        /// <summary>
        /// Event fired when any vault value changes
        /// </summary>
        public event Action<string, int, int> OnCurrencyChanged;

        /// <summary>
        /// Event fired when vault is loaded
        /// </summary>
        public event Action OnVaultLoaded;

        public VaultManager()
        {
            _vaultData = new VaultData();
            _currencyDefinitions = new Dictionary<string, CurrencyDefinition>();
            _isDirty = false;
            InitializeCurrencyDefinitions();
        }

        /// <summary>
        /// Initialize known currency definitions with their game item IDs.
        /// </summary>
        private void InitializeCurrencyDefinitions()
        {
            // Seasonal Tokens - Actual Sun Haven item IDs
            RegisterCurrency(new CurrencyDefinition("token_spring", "Spring Token", CurrencyCategory.SeasonalToken, 18020));
            RegisterCurrency(new CurrencyDefinition("token_summer", "Summer Token", CurrencyCategory.SeasonalToken, 18021));
            RegisterCurrency(new CurrencyDefinition("token_fall", "Fall Token", CurrencyCategory.SeasonalToken, 18023));
            RegisterCurrency(new CurrencyDefinition("token_winter", "Winter Token", CurrencyCategory.SeasonalToken, 18022));

            // Keys - Actual Sun Haven item IDs
            RegisterCurrency(new CurrencyDefinition("key_copper", "Copper Key", CurrencyCategory.Key, 1251));
            RegisterCurrency(new CurrencyDefinition("key_iron", "Iron Key", CurrencyCategory.Key, 1252));
            RegisterCurrency(new CurrencyDefinition("key_adamant", "Adamant Key", CurrencyCategory.Key, 1253));
            RegisterCurrency(new CurrencyDefinition("key_mithril", "Mithril Key", CurrencyCategory.Key, 1254));
            RegisterCurrency(new CurrencyDefinition("key_sunite", "Sunite Key", CurrencyCategory.Key, 1255));
            RegisterCurrency(new CurrencyDefinition("key_glorite", "Glorite Key", CurrencyCategory.Key, 1256));
            RegisterCurrency(new CurrencyDefinition("key_kingslostmine", "King's Lost Mine Key", CurrencyCategory.Key, 1257));

            // Special currencies (includes Community Token)
            RegisterCurrency(new CurrencyDefinition("special_communitytoken", "Community Token", CurrencyCategory.Special, 18013));
            RegisterCurrency(new CurrencyDefinition("special_doubloon", "Doubloon", CurrencyCategory.Special, 60014));
            RegisterCurrency(new CurrencyDefinition("special_blackbottlecap", "Black Bottle Cap", CurrencyCategory.Special, 60013));
            RegisterCurrency(new CurrencyDefinition("special_redcarnivalticket", "Red Carnival Ticket", CurrencyCategory.Special, 18012));
            RegisterCurrency(new CurrencyDefinition("special_candycornpieces", "Candy Corn Pieces", CurrencyCategory.Special, 18016));
            RegisterCurrency(new CurrencyDefinition("special_manashard", "Mana Shard", CurrencyCategory.Special, 18015));

            Plugin.Log?.LogInfo($"Initialized {_currencyDefinitions.Count} currency definitions");
        }

        /// <summary>
        /// Register a new currency definition
        /// </summary>
        public void RegisterCurrency(CurrencyDefinition definition)
        {
            _currencyDefinitions[definition.Id] = definition;
        }

        /// <summary>
        /// Get a currency definition by its ID
        /// </summary>
        public CurrencyDefinition GetCurrencyDefinition(string currencyId)
        {
            return _currencyDefinitions.TryGetValue(currencyId, out var def) ? def : null;
        }

        /// <summary>
        /// Get a currency definition by its game item ID
        /// </summary>
        public CurrencyDefinition GetCurrencyByGameItemId(int gameItemId)
        {
            foreach (var def in _currencyDefinitions.Values)
            {
                if (def.GameItemId == gameItemId)
                    return def;
            }
            return null;
        }

        /// <summary>
        /// Get all registered currency definitions
        /// </summary>
        public IEnumerable<CurrencyDefinition> GetAllCurrencies()
        {
            return _currencyDefinitions.Values;
        }

        /// <summary>
        /// Get currencies by category
        /// </summary>
        public IEnumerable<CurrencyDefinition> GetCurrenciesByCategory(CurrencyCategory category)
        {
            foreach (var kvp in _currencyDefinitions)
            {
                if (kvp.Value.Category == category)
                    yield return kvp.Value;
            }
        }

        /// <summary>
        /// Load vault data (called by save system)
        /// </summary>
        public void LoadVaultData(VaultData data)
        {
            _vaultData = data ?? new VaultData();
            _isDirty = false;
            Plugin.Log?.LogInfo($"Vault data loaded for player: {_vaultData.PlayerName}");
            OnVaultLoaded?.Invoke();
        }

        /// <summary>
        /// Get current vault data for saving
        /// </summary>
        public VaultData GetVaultData()
        {
            _vaultData.LastSaved = DateTime.Now;
            return _vaultData;
        }

        /// <summary>
        /// Check if vault has unsaved changes
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Mark vault as saved
        /// </summary>
        public void MarkClean()
        {
            _isDirty = false;
        }

        /// <summary>
        /// Set the player name for this vault
        /// </summary>
        public void SetPlayerName(string playerName)
        {
            _vaultData.PlayerName = playerName;
            _isDirty = true;
        }

        #region Seasonal Tokens

        public int GetSeasonalTokens(SeasonalTokenType type)
        {
            return _vaultData.SeasonalTokens.TryGetValue(type, out int count) ? count : 0;
        }

        public bool AddSeasonalTokens(SeasonalTokenType type, int amount)
        {
            if (amount < 0) return false;

            int oldValue = GetSeasonalTokens(type);
            _vaultData.SeasonalTokens[type] = oldValue + amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"seasonal_{type}", oldValue, oldValue + amount);
            Plugin.Log?.LogInfo($"Added {amount} {type} tokens. New total: {_vaultData.SeasonalTokens[type]}");
            return true;
        }

        public bool RemoveSeasonalTokens(SeasonalTokenType type, int amount)
        {
            if (amount < 0) return false;

            int current = GetSeasonalTokens(type);
            if (current < amount) return false;

            int oldValue = current;
            _vaultData.SeasonalTokens[type] = current - amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"seasonal_{type}", oldValue, current - amount);
            Plugin.Log?.LogInfo($"Removed {amount} {type} tokens. New total: {_vaultData.SeasonalTokens[type]}");
            return true;
        }

        public bool HasSeasonalTokens(SeasonalTokenType type, int amount)
        {
            return GetSeasonalTokens(type) >= amount;
        }

        #endregion

        #region Community Tokens

        public int GetCommunityTokens(string tokenId)
        {
            return _vaultData.CommunityTokens.TryGetValue(tokenId, out int count) ? count : 0;
        }

        public bool AddCommunityTokens(string tokenId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(tokenId)) return false;

            int oldValue = GetCommunityTokens(tokenId);
            _vaultData.CommunityTokens[tokenId] = oldValue + amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"community_{tokenId}", oldValue, oldValue + amount);
            return true;
        }

        public bool RemoveCommunityTokens(string tokenId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(tokenId)) return false;

            int current = GetCommunityTokens(tokenId);
            if (current < amount) return false;

            int oldValue = current;
            _vaultData.CommunityTokens[tokenId] = current - amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"community_{tokenId}", oldValue, current - amount);
            return true;
        }

        public bool HasCommunityTokens(string tokenId, int amount)
        {
            return GetCommunityTokens(tokenId) >= amount;
        }

        #endregion

        #region Keys

        public int GetKeys(string keyId)
        {
            return _vaultData.Keys.TryGetValue(keyId, out int count) ? count : 0;
        }

        public bool AddKeys(string keyId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(keyId)) return false;

            int oldValue = GetKeys(keyId);
            _vaultData.Keys[keyId] = oldValue + amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"key_{keyId}", oldValue, oldValue + amount);
            Plugin.Log?.LogInfo($"Added {amount} {keyId} keys. New total: {_vaultData.Keys[keyId]}");
            return true;
        }

        public bool RemoveKeys(string keyId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(keyId)) return false;

            int current = GetKeys(keyId);
            if (current < amount) return false;

            int oldValue = current;
            _vaultData.Keys[keyId] = current - amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"key_{keyId}", oldValue, current - amount);
            return true;
        }

        public bool HasKeys(string keyId, int amount)
        {
            return GetKeys(keyId) >= amount;
        }

        #endregion

        #region Special (uses Tickets storage for backwards compatibility)

        public int GetSpecial(string specialId)
        {
            return _vaultData.Tickets.TryGetValue(specialId, out int count) ? count : 0;
        }

        public bool AddSpecial(string specialId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(specialId)) return false;

            int oldValue = GetSpecial(specialId);
            _vaultData.Tickets[specialId] = oldValue + amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"special_{specialId}", oldValue, oldValue + amount);
            return true;
        }

        public bool RemoveSpecial(string specialId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(specialId)) return false;

            int current = GetSpecial(specialId);
            if (current < amount) return false;

            int oldValue = current;
            _vaultData.Tickets[specialId] = current - amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"special_{specialId}", oldValue, current - amount);
            return true;
        }

        public bool HasSpecial(string specialId, int amount)
        {
            return GetSpecial(specialId) >= amount;
        }

        // Legacy methods for backwards compatibility
        public int GetTickets(string ticketId) => GetSpecial(ticketId);
        public bool AddTickets(string ticketId, int amount) => AddSpecial(ticketId, amount);
        public bool RemoveTickets(string ticketId, int amount) => RemoveSpecial(ticketId, amount);
        public bool HasTickets(string ticketId, int amount) => HasSpecial(ticketId, amount);

        #endregion

        #region Orbs

        public int GetOrbs(string orbId)
        {
            return _vaultData.Orbs.TryGetValue(orbId, out int count) ? count : 0;
        }

        public bool AddOrbs(string orbId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(orbId)) return false;

            int oldValue = GetOrbs(orbId);
            _vaultData.Orbs[orbId] = oldValue + amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"orb_{orbId}", oldValue, oldValue + amount);
            return true;
        }

        public bool RemoveOrbs(string orbId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(orbId)) return false;

            int current = GetOrbs(orbId);
            if (current < amount) return false;

            int oldValue = current;
            _vaultData.Orbs[orbId] = current - amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"orb_{orbId}", oldValue, current - amount);
            return true;
        }

        public bool HasOrbs(string orbId, int amount)
        {
            return GetOrbs(orbId) >= amount;
        }

        #endregion

        #region Custom Currencies

        public int GetCustomCurrency(string currencyId)
        {
            return _vaultData.CustomCurrencies.TryGetValue(currencyId, out int count) ? count : 0;
        }

        public bool AddCustomCurrency(string currencyId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(currencyId)) return false;

            int oldValue = GetCustomCurrency(currencyId);
            _vaultData.CustomCurrencies[currencyId] = oldValue + amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"custom_{currencyId}", oldValue, oldValue + amount);
            return true;
        }

        public bool RemoveCustomCurrency(string currencyId, int amount)
        {
            if (amount < 0 || string.IsNullOrEmpty(currencyId)) return false;

            int current = GetCustomCurrency(currencyId);
            if (current < amount) return false;

            int oldValue = current;
            _vaultData.CustomCurrencies[currencyId] = current - amount;
            _isDirty = true;

            OnCurrencyChanged?.Invoke($"custom_{currencyId}", oldValue, current - amount);
            return true;
        }

        public bool HasCustomCurrency(string currencyId, int amount)
        {
            return GetCustomCurrency(currencyId) >= amount;
        }

        #endregion

        #region Generic Currency Operations

        /// <summary>
        /// Generic method to get any currency by its full ID
        /// </summary>
        public int GetCurrency(string fullCurrencyId)
        {
            if (string.IsNullOrEmpty(fullCurrencyId)) return 0;

            // Parse the currency ID to determine type
            if (fullCurrencyId.StartsWith("seasonal_"))
            {
                string typeName = fullCurrencyId.Substring("seasonal_".Length);
                if (Enum.TryParse<SeasonalTokenType>(typeName, out var tokenType))
                    return GetSeasonalTokens(tokenType);
            }
            else if (fullCurrencyId.StartsWith("community_"))
            {
                return GetCommunityTokens(fullCurrencyId.Substring("community_".Length));
            }
            else if (fullCurrencyId.StartsWith("key_"))
            {
                return GetKeys(fullCurrencyId.Substring("key_".Length));
            }
            else if (fullCurrencyId.StartsWith("special_"))
            {
                return GetSpecial(fullCurrencyId.Substring("special_".Length));
            }
            else if (fullCurrencyId.StartsWith("orb_"))
            {
                return GetOrbs(fullCurrencyId.Substring("orb_".Length));
            }
            else if (fullCurrencyId.StartsWith("custom_"))
            {
                return GetCustomCurrency(fullCurrencyId.Substring("custom_".Length));
            }

            return 0;
        }

        /// <summary>
        /// Generic method to check if player has enough of any currency
        /// </summary>
        public bool HasCurrency(string fullCurrencyId, int amount)
        {
            return GetCurrency(fullCurrencyId) >= amount;
        }

        /// <summary>
        /// Get a summary of all non-zero currencies for display
        /// </summary>
        public Dictionary<string, int> GetAllNonZeroCurrencies()
        {
            var result = new Dictionary<string, int>();

            foreach (var kvp in _vaultData.SeasonalTokens)
            {
                if (kvp.Value > 0)
                    result[$"seasonal_{kvp.Key}"] = kvp.Value;
            }

            foreach (var kvp in _vaultData.CommunityTokens)
            {
                if (kvp.Value > 0)
                    result[$"community_{kvp.Key}"] = kvp.Value;
            }

            foreach (var kvp in _vaultData.Keys)
            {
                if (kvp.Value > 0)
                    result[$"key_{kvp.Key}"] = kvp.Value;
            }

            foreach (var kvp in _vaultData.Tickets)
            {
                if (kvp.Value > 0)
                    result[$"special_{kvp.Key}"] = kvp.Value;
            }

            foreach (var kvp in _vaultData.Orbs)
            {
                if (kvp.Value > 0)
                    result[$"orb_{kvp.Key}"] = kvp.Value;
            }

            foreach (var kvp in _vaultData.CustomCurrencies)
            {
                if (kvp.Value > 0)
                    result[$"custom_{kvp.Key}"] = kvp.Value;
            }

            return result;
        }

        #endregion
    }
}
