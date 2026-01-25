using System;
using TheVault.UI;
using Wish;

namespace TheVault.Patches
{
    /// <summary>
    /// Simple vault loading system.
    /// Loads vault when player enters game, resets when returning to menu.
    /// </summary>
    public static class PlayerPatches
    {
        private static bool _isVaultLoaded = false;
        private static string _loadedCharacterName = null;

        /// <summary>
        /// Returns true if a vault is currently loaded.
        /// </summary>
        public static bool IsVaultLoaded => _isVaultLoaded;

        /// <summary>
        /// Returns the name of the character whose vault is loaded.
        /// </summary>
        public static string LoadedCharacterName => _loadedCharacterName;

        /// <summary>
        /// Called when player is initialized in-game.
        /// This is our single trigger point for vault loading.
        /// </summary>
        public static void OnPlayerInitialized(Player __instance)
        {
            try
            {
                // Ensure UI components exist (recreate if destroyed by game's cleanup)
                Plugin.EnsureUIComponentsExist();

                // Get the current character name
                string characterName = GetCurrentCharacterName();

                if (string.IsNullOrEmpty(characterName) || characterName == "default")
                {
                    Plugin.Log?.LogWarning("Could not determine character name on player init");
                    return;
                }

                Plugin.Log?.LogInfo($"Player initialized: {characterName}");

                // If vault already loaded for this character, skip
                if (_isVaultLoaded && _loadedCharacterName == characterName)
                {
                    Plugin.Log?.LogInfo($"Vault already loaded for {characterName}");
                    return;
                }

                // If vault loaded for different character, save it first
                if (_isVaultLoaded && _loadedCharacterName != characterName)
                {
                    Plugin.Log?.LogInfo($"Switching from {_loadedCharacterName} to {characterName}");
                    Plugin.SaveVault();
                    ResetState();
                }

                // Load vault for this character
                LoadVaultForCharacter(characterName);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        /// <summary>
        /// Load vault data for a character.
        /// </summary>
        private static void LoadVaultForCharacter(string characterName)
        {
            try
            {
                Plugin.Log?.LogInfo($"Loading vault for: {characterName}");

                // Load the vault file
                Plugin.LoadVaultForPlayer(characterName);

                // Update state
                _isVaultLoaded = true;
                _loadedCharacterName = characterName;

                // Set player name in vault manager
                var vaultManager = Plugin.GetVaultManager();
                vaultManager?.SetPlayerName(characterName);

                // Load UI icons
                IconCache.LoadAllIcons();

                Plugin.Log?.LogInfo($"Vault loaded successfully for {characterName}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading vault: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current character name.
        /// Primary source: LastLoadedCharacterName (extracted from slot during LoadCharacter)
        /// Fallback: GameSave.CurrentCharacter (can be stale on character switch)
        /// </summary>
        private static string GetCurrentCharacterName()
        {
            try
            {
                // PRIMARY: Use the character name extracted during LoadCharacter
                // This bypasses the stale CurrentCharacter issue
                string lastLoadedName = GameSavePatches.LastLoadedCharacterName;
                if (!string.IsNullOrEmpty(lastLoadedName))
                {
                    string sanitizedName = SanitizeFileName(lastLoadedName);
                    Plugin.Log?.LogInfo($"GetCurrentCharacterName: Using LastLoadedCharacterName = '{sanitizedName}'");
                    return sanitizedName;
                }

                // FALLBACK: Use CurrentCharacter (may be stale on character switch)
                // OLD CODE (kept for reference):
                // var currentChar = GameSave.CurrentCharacter;
                // if (currentChar != null && !string.IsNullOrEmpty(currentChar.characterName))
                // {
                //     return SanitizeFileName(currentChar.characterName);
                // }

                var currentChar = GameSave.CurrentCharacter;
                if (currentChar != null && !string.IsNullOrEmpty(currentChar.characterName))
                {
                    string nameFromCurrent = SanitizeFileName(currentChar.characterName);
                    Plugin.Log?.LogWarning($"GetCurrentCharacterName: FALLBACK to CurrentCharacter = '{nameFromCurrent}' (LastLoadedCharacterName was null)");
                    return nameFromCurrent;
                }

                Plugin.Log?.LogWarning("GetCurrentCharacterName: Could not determine character name from any source");
                return "default";
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character name: {ex.Message}");
                return "default";
            }
        }

        /// <summary>
        /// Sanitize a string for use as a filename.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "default";

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }

            name = name.Trim();
            return string.IsNullOrEmpty(name) ? "default" : name;
        }

        /// <summary>
        /// Reset vault state. Called when returning to menu.
        /// </summary>
        public static void ResetState()
        {
            Plugin.Log?.LogInfo("Resetting vault state");
            _isVaultLoaded = false;
            _loadedCharacterName = null;
            GameSavePatches.ResetLastLoadedSlot(); // Reset slot tracker so next character gets fresh data
            ItemPatches.ResetState();
            IconCache.Clear();
        }

        /// <summary>
        /// Save and reset. Called when exiting to menu.
        /// </summary>
        public static void SaveAndReset()
        {
            try
            {
                if (_isVaultLoaded)
                {
                    Plugin.Log?.LogInfo($"Saving vault for {_loadedCharacterName} before menu");
                    Plugin.SaveVault();
                }
                ResetState();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in SaveAndReset: {ex.Message}");
            }
        }

        // Legacy compatibility methods
        public static void ResetVaultLoaded() => ResetState();
        public static void ForceVaultReload() => ResetState();
        public static void TriggerVaultLoad(string characterName) => LoadVaultForCharacter(characterName);
    }
}
