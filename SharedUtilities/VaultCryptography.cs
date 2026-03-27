using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// CSVAULT2 AES crypto aligned with the **legacy** TheVault <c>VaultSaveSystem</c> (same salt, IV, PBKDF2, Steam/player key rule).
    /// V3 adds: player-name key when Steam key fails, legacy password variants, and accepting V3 JSON in legacy decrypt (not only <c>PlayerName</c>).
    /// </summary>
    public static class VaultCryptography
    {
        private const string EncryptionSalt = "TheV4ultS@lt2026Secure";
        private const int KeySize = 256;
        private const int Iterations = 10000;

        private static readonly byte[] Iv =
        {
            0x43, 0x75, 0x72, 0x72, 0x65, 0x6E, 0x63, 0x79, 0x53, 0x70, 0x65, 0x6C, 0x6C, 0x49, 0x56, 0x31
        };

        private static string _cachedSteamId;
        private static bool _steamLookupFinished;

        public static ManualLogSource LogSource { get; set; }

        public static byte[] Encrypt(string plainText, string playerName)
        {
            byte[] key = GenerateKey(playerName);
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    byte[] header = Encoding.UTF8.GetBytes("CSVAULT2");
                    ms.Write(header, 0, header.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs, Encoding.UTF8))
                        writer.Write(plainText);
                    return ms.ToArray();
                }
            }
        }

        /// <param name="alternateKeyName">Sanitized file stem when it differs from <paramref name="playerName"/>.</param>
        public static string Decrypt(byte[] cipherData, string playerName, string alternateKeyName = null)
        {
            try
            {
                if (cipherData == null || cipherData.Length < 8)
                {
                    LogSource?.LogWarning("[VaultCryptography] Vault file too small.");
                    return null;
                }

                string header = Encoding.UTF8.GetString(cipherData, 0, 8);
                if (header != "CSVAULT2")
                {
                    LogSource?.LogInfo("[VaultCryptography] Unencrypted vault payload; will re-encrypt on save.");
                    return Encoding.UTF8.GetString(cipherData);
                }

                // 1–2) Same as legacy primary key, then player-portable if Steam is active (cross-session fix)
                string steamId = GetSteamIdLegacyCached();
                string json = DecryptWithPrimaryKey(cipherData, GenerateKey(playerName));
                if (IsVaultJson(json)) return json;
                if (!string.IsNullOrEmpty(steamId))
                {
                    json = DecryptWithPrimaryKey(cipherData, DeriveKeyPlayerPortable(playerName));
                    if (IsVaultJson(json)) return json;
                }

                // 3) Legacy TryLegacyDecryption password strings (header skipped; matches migration intent)
                json = TryLegacyKeyStrings(cipherData, playerName);
                if (IsVaultJson(json)) return json;

                if (!string.IsNullOrEmpty(alternateKeyName) &&
                    !string.Equals(alternateKeyName, playerName, StringComparison.OrdinalIgnoreCase))
                {
                    json = TryLegacyKeyStrings(cipherData, alternateKeyName);
                    if (IsVaultJson(json)) return json;
                }

                json = TryLegacyKeyStrings(cipherData, "Player_" + playerName);
                if (IsVaultJson(json)) return json;

                LogSource?.LogError("[VaultCryptography] Decryption failed (legacy-compatible paths exhausted).");
                return null;
            }
            catch (Exception ex)
            {
                LogSource?.LogError($"[VaultCryptography] Decrypt error: {ex.Message}");
                return null;
            }
        }

        /// <summary>Public for persistence that still mirrors legacy load order.</summary>
        public static string TryLegacyDecryption(byte[] encryptedData, string playerName)
        {
            string json = TryLegacyKeyStrings(encryptedData, playerName);
            return IsVaultJson(json) ? json : null;
        }

        private static string TryLegacyKeyStrings(byte[] encryptedData, string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return null;
            string[] legacyMethods =
            {
                $"{EncryptionSalt}_{playerName}_TheVaultPortable",
                $"{EncryptionSalt}_Player_{playerName}_TheVaultPortable",
                $"{EncryptionSalt}_{playerName}_{GetMachineId()}"
            };
            foreach (var keySource in legacyMethods)
            {
                try
                {
                    byte[] key = GenerateLegacyKey(keySource);
                    string json = DecryptAesBodyAfterHeader(encryptedData, key);
                    if (IsVaultJson(json)) return json;
                }
                catch { }
            }
            return null;
        }

        private static string DecryptWithPrimaryKey(byte[] cipherData, byte[] key)
        {
            try
            {
                return DecryptAesBodyAfterHeader(cipherData, key);
            }
            catch
            {
                return null;
            }
        }

        private static string DecryptAesBodyAfterHeader(byte[] cipherData, byte[] key)
        {
            int offset = 0;
            if (cipherData.Length >= 8 && Encoding.UTF8.GetString(cipherData, 0, 8) == "CSVAULT2")
                offset = 8;
            if (offset >= cipherData.Length) return null;

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherData, offset, cipherData.Length - offset))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        private static byte[] GenerateLegacyKey(string combined)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(combined, Encoding.UTF8.GetBytes(EncryptionSalt), Iterations))
                return deriveBytes.GetBytes(KeySize / 8);
        }

        private static byte[] DeriveKeyPlayerPortable(string playerName)
        {
            string combined = $"{EncryptionSalt}_Player_{playerName}_TheVaultPortable";
            using (var deriveBytes = new Rfc2898DeriveBytes(combined, Encoding.UTF8.GetBytes(EncryptionSalt), Iterations))
                return deriveBytes.GetBytes(KeySize / 8);
        }

        private static byte[] GenerateKey(string playerName)
        {
            string steamId = GetSteamIdLegacyCached();
            string identifier = !string.IsNullOrEmpty(steamId) ? $"Steam_{steamId}" : $"Player_{playerName}";
            string combined = $"{EncryptionSalt}_{identifier}_TheVaultPortable";
            using (var deriveBytes = new Rfc2898DeriveBytes(combined, Encoding.UTF8.GetBytes(EncryptionSalt), Iterations))
                return deriveBytes.GetBytes(KeySize / 8);
        }

        /// <summary>Same one-shot behavior as legacy VaultSaveSystem (first call locks result).</summary>
        private static string GetSteamIdLegacyCached()
        {
            if (_steamLookupFinished) return _cachedSteamId;
            _steamLookupFinished = true;
            _cachedSteamId = null;
            try
            {
                Assembly steamAssembly = null;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string n = a.GetName().Name;
                    if (n != null && (n.Contains("rlabrecque") || n == "Steamworks.NET")) { steamAssembly = a; break; }
                }
                if (steamAssembly == null)
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try { if (a.GetType("Steamworks.SteamUser") != null) { steamAssembly = a; break; } }
                        catch { }
                    }
                }
                if (steamAssembly == null) return null;
                var steamUserType = steamAssembly.GetType("Steamworks.SteamUser");
                var getSteamIdMethod = steamUserType?.GetMethod("GetSteamID", BindingFlags.Public | BindingFlags.Static);
                if (getSteamIdMethod == null) return null;
                var steamId = getSteamIdMethod.Invoke(null, null);
                if (steamId == null) return null;
                string steamIdStr = steamId.ToString();
                if (string.IsNullOrEmpty(steamIdStr) || steamIdStr == "0" || steamIdStr.Length < 10) return null;
                _cachedSteamId = steamIdStr;
                return _cachedSteamId;
            }
            catch (Exception ex)
            {
                LogSource?.LogWarning($"[VaultCryptography] Steam ID lookup failed: {ex.Message}");
                return null;
            }
        }

        private static string GetMachineId()
        {
            try { return SystemInfo.deviceUniqueIdentifier; }
            catch { return "unknown"; }
        }

        private static bool IsVaultJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            if (json.IndexOf("\"PlayerName\"", StringComparison.Ordinal) >= 0) return true;
            // V3 persisted payload
            if (json.IndexOf("\"Entries\"", StringComparison.Ordinal) >= 0 &&
                json.IndexOf("\"CurrencyId\"", StringComparison.Ordinal) >= 0)
                return true;
            return false;
        }
    }
}
