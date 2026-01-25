using System;
using System.Reflection;
using Wish;

namespace TheVault.Patches
{
    /// <summary>
    /// Minimal patches for GameSave class.
    /// Main vault loading is handled by PlayerPatches.OnPlayerInitialized.
    /// </summary>
    public static class GameSavePatches
    {
        /// <summary>
        /// The last character slot that was loaded via LoadCharacter.
        /// This is used to get the correct character name since CurrentCharacter can be stale.
        /// </summary>
        public static int LastLoadedSlot { get; private set; } = -1;

        /// <summary>
        /// The character name extracted from the last loaded slot.
        /// This is the authoritative source for character name (bypasses stale CurrentCharacter).
        /// </summary>
        public static string LastLoadedCharacterName { get; private set; } = null;

        /// <summary>
        /// Reset the last loaded slot and character name. Called when returning to menu.
        /// </summary>
        public static void ResetLastLoadedSlot()
        {
            LastLoadedSlot = -1;
            LastLoadedCharacterName = null;
            Plugin.Log?.LogInfo("GameSavePatches: Reset LastLoadedSlot and LastLoadedCharacterName");
        }

        /// <summary>
        /// Called after GameSave.Load is invoked.
        /// This is just for logging - actual vault loading happens in OnPlayerInitialized.
        /// </summary>
        public static void OnGameSaveLoad()
        {
            try
            {
                var currentChar = GameSave.CurrentCharacter;
                if (currentChar != null)
                {
                    Plugin.Log?.LogInfo($"GameSave.Load: {currentChar.characterName}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnGameSaveLoad: {ex.Message}");
            }
        }

        /// <summary>
        /// Called after GameSave.LoadCharacter is invoked.
        /// Stores the slot number and extracts the character name for use by PlayerPatches.
        /// </summary>
        public static void OnLoadCharacter(int characterNumber)
        {
            try
            {
                Plugin.Log?.LogInfo($"GameSave.LoadCharacter: slot {characterNumber}");

                // Store the slot number
                LastLoadedSlot = characterNumber;
                Plugin.Log?.LogInfo($"GameSavePatches: Stored LastLoadedSlot = {characterNumber}");

                // Try to extract character name from the Saves list using reflection
                // (since GameSaveData structure isn't known at compile time)
                try
                {
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
                            else
                            {
                                Plugin.Log?.LogWarning($"GameSavePatches: Could not extract character name from slot {characterNumber}");
                            }
                        }
                    }
                }
                catch (Exception extractEx)
                {
                    Plugin.Log?.LogWarning($"Could not extract character name from slot: {extractEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnLoadCharacter: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract character name from GameSaveData using reflection.
        /// Tries multiple possible property/field names.
        /// </summary>
        private static string GetCharacterNameFromSaveData(object saveData)
        {
            if (saveData == null) return null;

            var type = saveData.GetType();
            Plugin.Log?.LogInfo($"GameSavePatches: SaveData type = {type.FullName}");

            // List of possible property/field names for character name
            string[] possibleNames = new[]
            {
                "characterName", "CharacterName", "name", "Name",
                "playerName", "PlayerName", "displayName", "DisplayName"
            };

            // Try properties first on the main object
            foreach (var propName in possibleNames)
            {
                var prop = type.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(saveData) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        Plugin.Log?.LogInfo($"GameSavePatches: Found name via property '{propName}' = '{value}'");
                        return value;
                    }
                }
            }

            // Try fields on the main object
            foreach (var fieldName in possibleNames)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(string))
                {
                    var value = field.GetValue(saveData) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        Plugin.Log?.LogInfo($"GameSavePatches: Found name via field '{fieldName}' = '{value}'");
                        return value;
                    }
                }
            }

            // Try characterData property (we saw this in logs: Property: characterData (CharacterData))
            var charDataProp = type.GetProperty("characterData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (charDataProp != null)
            {
                Plugin.Log?.LogInfo($"GameSavePatches: Found characterData property, inspecting...");
                var charDataObj = charDataProp.GetValue(saveData);
                if (charDataObj != null)
                {
                    var charDataType = charDataObj.GetType();
                    Plugin.Log?.LogInfo($"GameSavePatches: characterData type = {charDataType.FullName}");

                    // Try to find name in characterData
                    foreach (var propName in possibleNames)
                    {
                        var nameProp = charDataType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (nameProp != null && nameProp.PropertyType == typeof(string))
                        {
                            var value = nameProp.GetValue(charDataObj) as string;
                            if (!string.IsNullOrEmpty(value))
                            {
                                Plugin.Log?.LogInfo($"GameSavePatches: Found name via characterData.{propName} = '{value}'");
                                return value;
                            }
                        }
                    }

                    // Also try fields in characterData
                    foreach (var fieldName in possibleNames)
                    {
                        var nameField = charDataType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (nameField != null && nameField.FieldType == typeof(string))
                        {
                            var value = nameField.GetValue(charDataObj) as string;
                            if (!string.IsNullOrEmpty(value))
                            {
                                Plugin.Log?.LogInfo($"GameSavePatches: Found name via characterData.{fieldName} = '{value}'");
                                return value;
                            }
                        }
                    }

                    // Log all members of characterData for debugging
                    Plugin.Log?.LogInfo($"GameSavePatches: Listing all members of {charDataType.Name}:");
                    foreach (var prop in charDataType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        try
                        {
                            var val = prop.GetValue(charDataObj);
                            string valStr = val?.ToString() ?? "null";
                            if (valStr.Length > 100) valStr = valStr.Substring(0, 100) + "...";
                            Plugin.Log?.LogInfo($"  characterData.{prop.Name} ({prop.PropertyType.Name}) = {valStr}");
                        }
                        catch { }
                    }
                }
            }

            // FALLBACK: Try to extract name from fileName (e.g., "test9.save" -> "test9")
            var fileNameProp = type.GetProperty("fileName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fileNameProp != null)
            {
                var fileName = fileNameProp.GetValue(saveData) as string;
                if (!string.IsNullOrEmpty(fileName))
                {
                    // Remove .save extension
                    string nameFromFile = fileName;
                    if (nameFromFile.EndsWith(".save", StringComparison.OrdinalIgnoreCase))
                    {
                        nameFromFile = nameFromFile.Substring(0, nameFromFile.Length - 5);
                    }
                    if (!string.IsNullOrEmpty(nameFromFile))
                    {
                        Plugin.Log?.LogInfo($"GameSavePatches: Extracted name from fileName '{fileName}' -> '{nameFromFile}'");
                        return nameFromFile;
                    }
                }
            }

            // Log all available properties and fields for debugging (only if we haven't found anything)
            Plugin.Log?.LogInfo($"GameSavePatches: Listing all members of {type.Name}:");
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    var val = prop.GetValue(saveData);
                    Plugin.Log?.LogInfo($"  Property: {prop.Name} ({prop.PropertyType.Name}) = {val}");
                }
                catch { }
            }
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                try
                {
                    var val = field.GetValue(saveData);
                    Plugin.Log?.LogInfo($"  Field: {field.Name} ({field.FieldType.Name}) = {val}");
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Called after GameSave.SetCurrentCharacter is invoked.
        /// This is just for logging.
        /// </summary>
        public static void OnSetCurrentCharacter()
        {
            try
            {
                var currentChar = GameSave.CurrentCharacter;
                if (currentChar != null)
                {
                    Plugin.Log?.LogInfo($"GameSave.SetCurrentCharacter: {currentChar.characterName}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnSetCurrentCharacter: {ex.Message}");
            }
        }
    }
}
