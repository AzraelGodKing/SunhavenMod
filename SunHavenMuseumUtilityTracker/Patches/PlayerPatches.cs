using System;
using System.Reflection;
using Wish;

namespace SunHavenMuseumUtilityTracker.Patches
{
    /// <summary>
    /// Patches for detecting player/character loading.
    /// </summary>
    public static class PlayerPatches
    {
        private static bool _isDataLoaded = false;
        private static string _loadedCharacterName = null;

        public static bool IsDataLoaded => _isDataLoaded;
        public static string LoadedCharacterName => _loadedCharacterName;

        /// <summary>
        /// Called when player is initialized in-game.
        /// </summary>
        public static void OnPlayerInitialized(Player __instance)
        {
            try
            {
                Plugin.EnsureUIComponentsExist();

                string characterName = GetCurrentCharacterName();

                if (string.IsNullOrEmpty(characterName) || characterName == "default")
                {
                    Plugin.Log?.LogWarning("Could not determine character name on player init");
                    return;
                }

                Plugin.Log?.LogInfo($"Player initialized: {characterName}");

                if (_isDataLoaded && _loadedCharacterName == characterName)
                {
                    Plugin.Log?.LogInfo($"Data already loaded for {characterName}");
                    return;
                }

                if (_isDataLoaded && _loadedCharacterName != characterName)
                {
                    Plugin.Log?.LogInfo($"Switching from {_loadedCharacterName} to {characterName}");
                    Plugin.SaveData();
                    ResetState();
                }

                LoadDataForCharacter(characterName);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        private static void LoadDataForCharacter(string characterName)
        {
            try
            {
                Plugin.Log?.LogInfo($"Loading donation data for: {characterName}");

                Plugin.LoadDataForPlayer(characterName);

                _isDataLoaded = true;
                _loadedCharacterName = characterName;

                Plugin.Log?.LogInfo($"Donation data loaded successfully for {characterName}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading donation data: {ex.Message}");
            }
        }

        private static string GetCurrentCharacterName()
        {
            try
            {
                // Use the character name from LoadCharacter
                string lastLoadedName = GameSavePatches.LastLoadedCharacterName;
                if (!string.IsNullOrEmpty(lastLoadedName))
                {
                    string sanitizedName = SanitizeFileName(lastLoadedName);
                    Plugin.Log?.LogInfo($"GetCurrentCharacterName: Using LastLoadedCharacterName = '{sanitizedName}'");
                    return sanitizedName;
                }

                // Fallback to CurrentCharacter
                var currentChar = GameSave.CurrentCharacter;
                if (currentChar != null && !string.IsNullOrEmpty(currentChar.characterName))
                {
                    string nameFromCurrent = SanitizeFileName(currentChar.characterName);
                    Plugin.Log?.LogWarning($"GetCurrentCharacterName: FALLBACK to CurrentCharacter = '{nameFromCurrent}'");
                    return nameFromCurrent;
                }

                Plugin.Log?.LogWarning("GetCurrentCharacterName: Could not determine character name");
                return "default";
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error getting character name: {ex.Message}");
                return "default";
            }
        }

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

        public static void ResetState()
        {
            Plugin.Log?.LogInfo("Resetting data state");
            _isDataLoaded = false;
            _loadedCharacterName = null;
            GameSavePatches.ResetLastLoadedSlot();
        }

        public static void SaveAndReset()
        {
            try
            {
                if (_isDataLoaded)
                {
                    Plugin.Log?.LogInfo($"Saving data for {_loadedCharacterName} before menu");
                    Plugin.SaveData();
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

        public static void OnLoadCharacter(int characterNumber)
        {
            try
            {
                Plugin.Log?.LogInfo($"GameSave.LoadCharacter: slot {characterNumber}");
                LastLoadedSlot = characterNumber;

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
