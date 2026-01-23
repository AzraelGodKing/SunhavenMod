using HarmonyLib;
using System;
using TheVault.UI;
using Wish;

namespace TheVault.Patches
{
    /// <summary>
    /// Patches for player initialization and events.
    /// Used to load vault data when player enters the game.
    /// </summary>
    public static class PlayerPatches
    {
        private static bool _hasLoadedVault = false;
        private static string _currentCharacterName = null;

        /// <summary>
        /// Returns true if the vault has been loaded for the current character.
        /// UI components should check this before displaying.
        /// </summary>
        public static bool IsVaultLoaded => _hasLoadedVault;

        /// <summary>
        /// Called after player is initialized as owner.
        /// This is the main entry point for loading vault data.
        /// </summary>
        public static void OnPlayerInitialized(Wish.Player __instance)
        {
            try
            {
                // Get character name from GameSave
                string characterName = GetCharacterName();

                // Check if we're switching characters or loading for first time
                if (_hasLoadedVault && _currentCharacterName == characterName)
                {
                    Plugin.Log?.LogInfo($"Vault already loaded for character: {characterName}");
                    return;
                }

                // If switching characters, save the old vault first
                if (_hasLoadedVault && _currentCharacterName != null && _currentCharacterName != characterName)
                {
                    Plugin.Log?.LogInfo($"Switching from character '{_currentCharacterName}' to '{characterName}' - saving old vault");
                    Plugin.SaveVault();
                }

                Plugin.Log?.LogInfo($"Player initialized: {characterName}");

                // Load vault data for this character
                Plugin.LoadVaultForPlayer(characterName);
                _hasLoadedVault = true;
                _currentCharacterName = characterName;

                // Set player name in vault manager
                var vaultManager = Plugin.GetVaultManager();
                vaultManager?.SetPlayerName(characterName);

                // Now that game is fully loaded, load the UI icons
                Plugin.Log?.LogInfo("Game fully loaded - loading UI icons...");
                IconCache.LoadAllIcons();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the character's name from GameSave.CurrentCharacter
        /// </summary>
        private static string GetCharacterName()
        {
            try
            {
                // Use GameSave.CurrentCharacter which contains the character data
                var currentChar = GameSave.CurrentCharacter;
                if (currentChar != null)
                {
                    // Try characterName field first (this is what Sun Haven uses)
                    string name = currentChar.characterName;
                    if (!string.IsNullOrEmpty(name))
                    {
                        Plugin.Log?.LogInfo($"Found character name from GameSave: {name}");
                        return SanitizeCharacterName(name);
                    }
                }

                // Fallback: try getting from Player instance
                if (Player.Instance != null)
                {
                    // Try reflection as backup
                    var playerType = Player.Instance.GetType();

                    // Try playerName field
                    var nameField = AccessTools.Field(playerType, "playerName");
                    if (nameField != null)
                    {
                        var name = nameField.GetValue(Player.Instance) as string;
                        if (!string.IsNullOrEmpty(name))
                        {
                            Plugin.Log?.LogInfo($"Found character name from Player.playerName: {name}");
                            return SanitizeCharacterName(name);
                        }
                    }
                }

                Plugin.Log?.LogWarning("Could not determine character name, using 'default'");
                return "default";
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character name: {ex.Message}");
                return "default";
            }
        }

        /// <summary>
        /// Sanitize character name for use as filename
        /// </summary>
        private static string SanitizeCharacterName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "default";

            // Remove or replace invalid filename characters
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }

            // Trim and ensure not empty
            name = name.Trim();
            if (string.IsNullOrEmpty(name))
                return "default";

            return name;
        }

        /// <summary>
        /// Reset the vault loaded flag (used when player exits to menu)
        /// </summary>
        public static void ResetVaultLoaded()
        {
            _hasLoadedVault = false;
        }
    }
}
