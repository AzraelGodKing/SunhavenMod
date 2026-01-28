using System;
using System.Reflection;
using Wish;

namespace TheList.Patches
{
    /// <summary>
    /// Handles player initialization and character switching.
    /// Loads/saves list data at appropriate times.
    /// </summary>
    public static class PlayerPatches
    {
        private static bool _isListLoaded = false;
        private static string _loadedCharacterName = null;

        /// <summary>
        /// Returns true if a list is currently loaded.
        /// </summary>
        public static bool IsListLoaded => _isListLoaded;

        /// <summary>
        /// Returns the name of the character whose list is loaded.
        /// </summary>
        public static string LoadedCharacterName => _loadedCharacterName;

        /// <summary>
        /// Called when player is initialized in-game.
        /// </summary>
        public static void OnPlayerInitialized(Player __instance)
        {
            try
            {
                // Ensure UI components exist
                Plugin.EnsureUIComponentsExist();

                // Get the current character name
                string characterName = GetCurrentCharacterName();

                if (string.IsNullOrEmpty(characterName) || characterName == "default")
                {
                    Plugin.Log?.LogWarning("Could not determine character name on player init");
                    return;
                }

                Plugin.Log?.LogInfo($"Player initialized: {characterName}");

                // If list already loaded for this character, skip
                if (_isListLoaded && _loadedCharacterName == characterName)
                {
                    Plugin.Log?.LogInfo($"List already loaded for {characterName}");
                    return;
                }

                // If list loaded for different character, save it first
                if (_isListLoaded && _loadedCharacterName != characterName)
                {
                    Plugin.Log?.LogInfo($"Switching from {_loadedCharacterName} to {characterName}");
                    Plugin.SaveList();
                    ResetState();
                }

                // Load list for this character
                LoadListForCharacter(characterName);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        /// <summary>
        /// Load list data for a character.
        /// </summary>
        private static void LoadListForCharacter(string characterName)
        {
            try
            {
                Plugin.Log?.LogInfo($"Loading list for: {characterName}");

                // Load the list file
                Plugin.LoadListForPlayer(characterName);

                // Update state
                _isListLoaded = true;
                _loadedCharacterName = characterName;

                // Set player name in list manager
                var listManager = Plugin.GetListManager();
                listManager?.SetPlayerName(characterName);

                Plugin.Log?.LogInfo($"List loaded successfully for {characterName}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading list: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current character name.
        /// </summary>
        private static string GetCurrentCharacterName()
        {
            try
            {
                // PRIMARY: Use the character name extracted during LoadCharacter
                string lastLoadedName = GameSavePatches.LastLoadedCharacterName;
                if (!string.IsNullOrEmpty(lastLoadedName))
                {
                    string sanitizedName = SanitizeFileName(lastLoadedName);
                    Plugin.Log?.LogInfo($"GetCurrentCharacterName: Using LastLoadedCharacterName = '{sanitizedName}'");
                    return sanitizedName;
                }

                // FALLBACK: Use CurrentCharacter
                var currentChar = GameSave.CurrentCharacter;
                if (currentChar != null && !string.IsNullOrEmpty(currentChar.characterName))
                {
                    string nameFromCurrent = SanitizeFileName(currentChar.characterName);
                    Plugin.Log?.LogWarning($"GetCurrentCharacterName: FALLBACK to CurrentCharacter = '{nameFromCurrent}'");
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
        /// Reset list state. Called when returning to menu.
        /// </summary>
        public static void ResetState()
        {
            Plugin.Log?.LogInfo("Resetting list state");
            _isListLoaded = false;
            _loadedCharacterName = null;
            GameSavePatches.ResetLastLoadedSlot();
        }

        /// <summary>
        /// Save and reset. Called when exiting to menu.
        /// </summary>
        public static void SaveAndReset()
        {
            try
            {
                if (_isListLoaded)
                {
                    Plugin.Log?.LogInfo($"Saving list for {_loadedCharacterName} before menu");
                    Plugin.SaveList();
                }
                ResetState();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in SaveAndReset: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tracks character loading for proper name extraction.
    /// </summary>
    public static class GameSavePatches
    {
        public static int LastLoadedSlot { get; private set; } = -1;
        public static string LastLoadedCharacterName { get; private set; } = null;

        public static void ResetLastLoadedSlot()
        {
            LastLoadedSlot = -1;
            LastLoadedCharacterName = null;
            Plugin.Log?.LogInfo("GameSavePatches: Reset LastLoadedSlot and LastLoadedCharacterName");
        }

        /// <summary>
        /// Called after GameSave.LoadCharacter is invoked.
        /// </summary>
        public static void OnLoadCharacter(int characterNumber)
        {
            try
            {
                Plugin.Log?.LogInfo($"GameSave.LoadCharacter: slot {characterNumber}");
                LastLoadedSlot = characterNumber;

                // Extract character name from save data
                if (GameSave.Instance?.Saves != null &&
                    characterNumber >= 0 &&
                    characterNumber < GameSave.Instance.Saves.Count)
                {
                    var charData = GameSave.Instance.Saves[characterNumber];
                    if (charData != null)
                    {
                        string charName = GetCharacterNameFromSaveData(charData);
                        if (!string.IsNullOrEmpty(charName))
                        {
                            LastLoadedCharacterName = charName;
                            Plugin.Log?.LogInfo($"GameSavePatches: Extracted character name '{charName}' from slot {characterNumber}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnLoadCharacter: {ex.Message}");
            }
        }

        private static string GetCharacterNameFromSaveData(object saveData)
        {
            if (saveData == null) return null;

            var type = saveData.GetType();
            string[] possibleNames = { "characterName", "CharacterName", "name", "Name", "playerName", "PlayerName" };

            // Try properties
            foreach (var propName in possibleNames)
            {
                var prop = type.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(saveData) as string;
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            // Try characterData nested property
            var charDataProp = type.GetProperty("characterData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (charDataProp != null)
            {
                var charDataObj = charDataProp.GetValue(saveData);
                if (charDataObj != null)
                {
                    var charDataType = charDataObj.GetType();
                    foreach (var propName in possibleNames)
                    {
                        var nameProp = charDataType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (nameProp != null && nameProp.PropertyType == typeof(string))
                        {
                            var value = nameProp.GetValue(charDataObj) as string;
                            if (!string.IsNullOrEmpty(value))
                                return value;
                        }
                    }
                }
            }

            return null;
        }
    }
}
