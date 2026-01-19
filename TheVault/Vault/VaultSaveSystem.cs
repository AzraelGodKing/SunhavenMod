using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace TheVault.Vault
{
    /// <summary>
    /// Handles saving and loading vault data to/from encrypted files.
    /// Saves are stored per-player in the BepInEx config folder.
    /// Uses AES encryption to prevent manual editing.
    /// </summary>
    public class VaultSaveSystem
    {
        private readonly string _saveDirectory;
        private readonly VaultManager _vaultManager;
        private string _currentSaveFile;

        // Encryption settings
        private const string ENCRYPTION_SALT = "TheV4ultS@lt2026Secure";
        private const int KEY_SIZE = 256;
        private const int ITERATIONS = 10000;
        private static readonly byte[] _iv = new byte[16] { 0x43, 0x75, 0x72, 0x72, 0x65, 0x6E, 0x63, 0x79, 0x53, 0x70, 0x65, 0x6C, 0x6C, 0x49, 0x56, 0x31 };

        // Auto-save interval in seconds
        private const float AUTO_SAVE_INTERVAL = 300f; // 5 minutes
        private float _lastAutoSave;

        public VaultSaveSystem(VaultManager vaultManager)
        {
            _vaultManager = vaultManager;
            _saveDirectory = Path.Combine(BepInEx.Paths.ConfigPath, "CurrencySpell", "Saves");
            _lastAutoSave = Time.time;

            // Ensure save directory exists
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
                Plugin.Log?.LogInfo($"Created save directory: {_saveDirectory}");
            }
        }

        /// <summary>
        /// Get the save file path for a specific player
        /// </summary>
        private string GetSaveFilePath(string playerName)
        {
            // Sanitize player name for file system
            string safeName = SanitizeFileName(playerName);
            if (string.IsNullOrEmpty(safeName))
                safeName = "default";

            return Path.Combine(_saveDirectory, $"{safeName}.vault");
        }

        /// <summary>
        /// Sanitize a string for use as a filename
        /// </summary>
        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }

        /// <summary>
        /// Load vault data for a player
        /// </summary>
        public bool Load(string playerName)
        {
            try
            {
                _currentSaveFile = GetSaveFilePath(playerName);

                if (!File.Exists(_currentSaveFile))
                {
                    Plugin.Log?.LogInfo($"No existing save file for player '{playerName}', creating new vault");
                    var newData = new VaultData { PlayerName = playerName };
                    _vaultManager.LoadVaultData(newData);
                    return true;
                }

                // Read and decrypt the file
                byte[] encryptedData = File.ReadAllBytes(_currentSaveFile);
                string json = Decrypt(encryptedData, playerName);

                if (string.IsNullOrEmpty(json))
                {
                    Plugin.Log?.LogWarning($"Failed to decrypt vault data for '{playerName}', creating new vault");
                    var newData = new VaultData { PlayerName = playerName };
                    _vaultManager.LoadVaultData(newData);
                    return true;
                }

                var wrapper = JsonUtility.FromJson<VaultDataWrapper>(json);

                VaultData data;
                if (wrapper != null)
                {
                    data = wrapper.ToVaultData();
                }
                else
                {
                    Plugin.Log?.LogWarning($"Failed to deserialize vault data, creating new vault");
                    data = new VaultData { PlayerName = playerName };
                }

                // Handle version migrations if needed
                data = MigrateData(data);

                _vaultManager.LoadVaultData(data);
                Plugin.Log?.LogInfo($"Loaded vault data for player '{playerName}'");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to load vault data: {ex.Message}");

                // Load empty vault on error
                _vaultManager.LoadVaultData(new VaultData { PlayerName = playerName });
                return false;
            }
        }

        /// <summary>
        /// Save current vault data (encrypted)
        /// </summary>
        public bool Save()
        {
            if (string.IsNullOrEmpty(_currentSaveFile))
            {
                Plugin.Log?.LogWarning("No save file set, cannot save");
                return false;
            }

            try
            {
                var data = _vaultManager.GetVaultData();
                var wrapper = VaultDataWrapper.FromVaultData(data);
                string json = JsonUtility.ToJson(wrapper, true);

                // Encrypt the JSON data
                byte[] encryptedData = Encrypt(json, data.PlayerName);

                // Write to temp file first, then move (atomic operation)
                string tempFile = _currentSaveFile + ".tmp";
                File.WriteAllBytes(tempFile, encryptedData);

                // Backup existing file
                if (File.Exists(_currentSaveFile))
                {
                    string backupFile = _currentSaveFile + ".backup";
                    if (File.Exists(backupFile))
                        File.Delete(backupFile);
                    File.Move(_currentSaveFile, backupFile);
                }

                File.Move(tempFile, _currentSaveFile);

                _vaultManager.MarkClean();
                _lastAutoSave = Time.time;
                Plugin.Log?.LogInfo($"Saved vault data to {_currentSaveFile}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to save vault data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Force an immediate save
        /// </summary>
        public void ForceSave()
        {
            if (_vaultManager.IsDirty)
            {
                Save();
            }
        }

        /// <summary>
        /// Check if auto-save should run and perform it if needed
        /// </summary>
        public void CheckAutoSave()
        {
            if (!_vaultManager.IsDirty)
                return;

            if (Time.time - _lastAutoSave >= AUTO_SAVE_INTERVAL)
            {
                Plugin.Log?.LogInfo("Auto-saving vault data...");
                Save();
            }
        }

        /// <summary>
        /// Handle data migration between versions
        /// </summary>
        private VaultData MigrateData(VaultData data)
        {
            if (data.Version < 1)
            {
                // Version 0 -> 1 migration
                data.Version = 1;
                Plugin.Log?.LogInfo("Migrated vault data to version 1");
            }

            // Add future migrations here
            // if (data.Version < 2) { ... }

            return data;
        }

        /// <summary>
        /// Delete vault data for a player (use with caution)
        /// </summary>
        public bool DeleteSave(string playerName)
        {
            try
            {
                string saveFile = GetSaveFilePath(playerName);
                if (File.Exists(saveFile))
                {
                    File.Delete(saveFile);
                    Plugin.Log?.LogInfo($"Deleted vault save for player '{playerName}'");
                }

                string backupFile = saveFile + ".backup";
                if (File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to delete vault save: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get list of all saved player vaults
        /// </summary>
        public string[] GetAllSavedPlayers()
        {
            try
            {
                var files = Directory.GetFiles(_saveDirectory, "*.vault");
                var players = new string[files.Length];

                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileNameWithoutExtension(files[i]);
                    players[i] = fileName;
                }

                return players;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to get saved players: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Export vault data to a specific location (for backups)
        /// </summary>
        public bool ExportVault(string exportPath)
        {
            try
            {
                var data = _vaultManager.GetVaultData();
                var wrapper = VaultDataWrapper.FromVaultData(data);
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(exportPath, json);
                Plugin.Log?.LogInfo($"Exported vault to {exportPath}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to export vault: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Import vault data from a specific location
        /// </summary>
        public bool ImportVault(string importPath)
        {
            try
            {
                if (!File.Exists(importPath))
                {
                    Plugin.Log?.LogError($"Import file not found: {importPath}");
                    return false;
                }

                string json = File.ReadAllText(importPath);
                var wrapper = JsonUtility.FromJson<VaultDataWrapper>(json);

                if (wrapper == null)
                {
                    Plugin.Log?.LogError("Failed to parse import file");
                    return false;
                }

                var data = wrapper.ToVaultData();
                data = MigrateData(data);
                _vaultManager.LoadVaultData(data);
                Plugin.Log?.LogInfo($"Imported vault from {importPath}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to import vault: {ex.Message}");
                return false;
            }
        }

        #region Encryption

        /// <summary>
        /// Generate encryption key from player name and machine-specific data
        /// </summary>
        private byte[] GenerateKey(string playerName)
        {
            // Combine player name with salt and machine ID for unique key per player/machine
            string machineId = SystemInfo.deviceUniqueIdentifier;
            string combined = $"{ENCRYPTION_SALT}_{playerName}_{machineId}";

            using (var deriveBytes = new Rfc2898DeriveBytes(combined, Encoding.UTF8.GetBytes(ENCRYPTION_SALT), ITERATIONS))
            {
                return deriveBytes.GetBytes(KEY_SIZE / 8);
            }
        }

        /// <summary>
        /// Encrypt JSON string to bytes
        /// </summary>
        private byte[] Encrypt(string plainText, string playerName)
        {
            byte[] key = GenerateKey(playerName);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // Write a magic header to identify encrypted files
                    byte[] header = Encoding.UTF8.GetBytes("CSVAULT2");
                    ms.Write(header, 0, header.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs, Encoding.UTF8))
                    {
                        writer.Write(plainText);
                    }

                    return ms.ToArray();
                }
            }
        }

        /// <summary>
        /// Decrypt bytes to JSON string
        /// </summary>
        private string Decrypt(byte[] cipherData, string playerName)
        {
            try
            {
                // Check for magic header
                if (cipherData.Length < 8)
                {
                    Plugin.Log?.LogWarning("Vault file too small, may be corrupted");
                    return null;
                }

                string header = Encoding.UTF8.GetString(cipherData, 0, 8);
                if (header != "CSVAULT2")
                {
                    // Try to read as plain JSON (legacy unencrypted file)
                    Plugin.Log?.LogInfo("Detected legacy unencrypted vault file, will re-encrypt on save");
                    return Encoding.UTF8.GetString(cipherData);
                }

                byte[] key = GenerateKey(playerName);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = _iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(cipherData, 8, cipherData.Length - 8))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                Plugin.Log?.LogError($"Decryption failed (file may have been tampered with): {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error decrypting vault: {ex.Message}");
                return null;
            }
        }

        #endregion
    }

    /// <summary>
    /// Wrapper class for Unity's JsonUtility serialization.
    /// JsonUtility doesn't support Dictionary, so we use arrays of key-value pairs.
    /// </summary>
    [Serializable]
    public class VaultDataWrapper
    {
        public int Version;
        public string PlayerName;
        public string LastSaved;

        // Seasonal tokens as parallel arrays
        public int[] SeasonalTokenTypes;
        public int[] SeasonalTokenValues;

        // Community tokens
        public string[] CommunityTokenKeys;
        public int[] CommunityTokenValues;

        // Keys
        public string[] KeyIds;
        public int[] KeyValues;

        // Tickets
        public string[] TicketIds;
        public int[] TicketValues;

        // Orbs
        public string[] OrbIds;
        public int[] OrbValues;

        // Custom currencies
        public string[] CustomCurrencyIds;
        public int[] CustomCurrencyValues;

        public static VaultDataWrapper FromVaultData(VaultData data)
        {
            var wrapper = new VaultDataWrapper
            {
                Version = data.Version,
                PlayerName = data.PlayerName ?? "",
                LastSaved = data.LastSaved.ToString("o")
            };

            // Seasonal tokens
            var seasonalTypes = new System.Collections.Generic.List<int>();
            var seasonalValues = new System.Collections.Generic.List<int>();
            foreach (var kvp in data.SeasonalTokens)
            {
                seasonalTypes.Add((int)kvp.Key);
                seasonalValues.Add(kvp.Value);
            }
            wrapper.SeasonalTokenTypes = seasonalTypes.ToArray();
            wrapper.SeasonalTokenValues = seasonalValues.ToArray();

            // Community tokens
            var commKeys = new System.Collections.Generic.List<string>();
            var commValues = new System.Collections.Generic.List<int>();
            foreach (var kvp in data.CommunityTokens)
            {
                commKeys.Add(kvp.Key);
                commValues.Add(kvp.Value);
            }
            wrapper.CommunityTokenKeys = commKeys.ToArray();
            wrapper.CommunityTokenValues = commValues.ToArray();

            // Keys
            var keyIds = new System.Collections.Generic.List<string>();
            var keyVals = new System.Collections.Generic.List<int>();
            foreach (var kvp in data.Keys)
            {
                keyIds.Add(kvp.Key);
                keyVals.Add(kvp.Value);
            }
            wrapper.KeyIds = keyIds.ToArray();
            wrapper.KeyValues = keyVals.ToArray();

            // Tickets
            var ticketIds = new System.Collections.Generic.List<string>();
            var ticketVals = new System.Collections.Generic.List<int>();
            foreach (var kvp in data.Tickets)
            {
                ticketIds.Add(kvp.Key);
                ticketVals.Add(kvp.Value);
            }
            wrapper.TicketIds = ticketIds.ToArray();
            wrapper.TicketValues = ticketVals.ToArray();

            // Orbs
            var orbIds = new System.Collections.Generic.List<string>();
            var orbVals = new System.Collections.Generic.List<int>();
            foreach (var kvp in data.Orbs)
            {
                orbIds.Add(kvp.Key);
                orbVals.Add(kvp.Value);
            }
            wrapper.OrbIds = orbIds.ToArray();
            wrapper.OrbValues = orbVals.ToArray();

            // Custom currencies
            var customIds = new System.Collections.Generic.List<string>();
            var customVals = new System.Collections.Generic.List<int>();
            foreach (var kvp in data.CustomCurrencies)
            {
                customIds.Add(kvp.Key);
                customVals.Add(kvp.Value);
            }
            wrapper.CustomCurrencyIds = customIds.ToArray();
            wrapper.CustomCurrencyValues = customVals.ToArray();

            return wrapper;
        }

        public VaultData ToVaultData()
        {
            var data = new VaultData
            {
                Version = Version,
                PlayerName = PlayerName ?? ""
            };

            // Parse last saved
            if (DateTime.TryParse(LastSaved, out var lastSaved))
                data.LastSaved = lastSaved;

            // Seasonal tokens
            data.SeasonalTokens.Clear();
            if (SeasonalTokenTypes != null && SeasonalTokenValues != null)
            {
                int count = Math.Min(SeasonalTokenTypes.Length, SeasonalTokenValues.Length);
                for (int i = 0; i < count; i++)
                {
                    if (Enum.IsDefined(typeof(SeasonalTokenType), SeasonalTokenTypes[i]))
                    {
                        data.SeasonalTokens[(SeasonalTokenType)SeasonalTokenTypes[i]] = SeasonalTokenValues[i];
                    }
                }
            }

            // Community tokens
            data.CommunityTokens.Clear();
            if (CommunityTokenKeys != null && CommunityTokenValues != null)
            {
                int count = Math.Min(CommunityTokenKeys.Length, CommunityTokenValues.Length);
                for (int i = 0; i < count; i++)
                {
                    data.CommunityTokens[CommunityTokenKeys[i]] = CommunityTokenValues[i];
                }
            }

            // Keys
            data.Keys.Clear();
            if (KeyIds != null && KeyValues != null)
            {
                int count = Math.Min(KeyIds.Length, KeyValues.Length);
                for (int i = 0; i < count; i++)
                {
                    data.Keys[KeyIds[i]] = KeyValues[i];
                }
            }

            // Tickets
            data.Tickets.Clear();
            if (TicketIds != null && TicketValues != null)
            {
                int count = Math.Min(TicketIds.Length, TicketValues.Length);
                for (int i = 0; i < count; i++)
                {
                    data.Tickets[TicketIds[i]] = TicketValues[i];
                }
            }

            // Orbs
            data.Orbs.Clear();
            if (OrbIds != null && OrbValues != null)
            {
                int count = Math.Min(OrbIds.Length, OrbValues.Length);
                for (int i = 0; i < count; i++)
                {
                    data.Orbs[OrbIds[i]] = OrbValues[i];
                }
            }

            // Custom currencies
            data.CustomCurrencies.Clear();
            if (CustomCurrencyIds != null && CustomCurrencyValues != null)
            {
                int count = Math.Min(CustomCurrencyIds.Length, CustomCurrencyValues.Length);
                for (int i = 0; i < count; i++)
                {
                    data.CustomCurrencies[CustomCurrencyIds[i]] = CustomCurrencyValues[i];
                }
            }

            return data;
        }
    }
}
