using System;
using Wish;

namespace TheVault.Patches
{
    /// <summary>
    /// Minimal patches for GameSave class.
    /// Main vault loading is handled by PlayerPatches.OnPlayerInitialized.
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
        /// </summary>
        public static void OnLoadCharacter(int characterNumber)
        {
            try
            {
                Plugin.Log?.LogDebug($"GameSave.LoadCharacter: slot {characterNumber}");

                LastLoadedSlot = characterNumber;

                if (GameSave.Instance?.Saves == null ||
                    characterNumber < 0 ||
                    characterNumber >= GameSave.Instance.Saves.Count)
                {
                    Plugin.Log?.LogWarning($"GameSavePatches: invalid slot {characterNumber}");
                    return;
                }

                var saveData = GameSave.Instance.Saves[characterNumber];
                string charName = GetCharacterNameFromSaveData(saveData);
                if (!string.IsNullOrEmpty(charName))
                {
                    LastLoadedCharacterName = charName;
                    Plugin.Log?.LogDebug($"GameSavePatches: character name '{charName}' from slot {characterNumber}");
                }
                else
                {
                    Plugin.Log?.LogWarning($"GameSavePatches: could not resolve name for slot {characterNumber}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error in OnLoadCharacter: {ex.Message}");
            }
        }

        private static string GetCharacterNameFromSaveData(GameSaveData saveData)
        {
            if (saveData == null) return null;

            string name = saveData.characterData?.characterName;
            if (!string.IsNullOrEmpty(name))
                return name;

            if (!string.IsNullOrEmpty(saveData.fileName))
            {
                string fromFile = saveData.fileName;
                if (fromFile.EndsWith(".save", StringComparison.OrdinalIgnoreCase))
                    fromFile = fromFile.Substring(0, fromFile.Length - 5);
                if (!string.IsNullOrEmpty(fromFile))
                    return fromFile;
            }

            return null;
        }

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
