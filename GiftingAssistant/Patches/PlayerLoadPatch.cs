using System;
using GiftingAssistant.Game;
using HarmonyLib;

namespace GiftingAssistant.Patches
{
    /// <summary>
    /// Postfix on Wish.Player.InitializeAsOwner: load the per-character roster, ensure the UI
    /// exists, refresh game NPC data, and (re)hook the overnight reset.
    /// </summary>
    public static class PlayerLoadPatch
    {
        private static string _loadedCharacterName;

        public static void OnPlayerInitialized(object __instance)
        {
            try
            {
                Plugin.EnsureUIComponentsExist();
                GiftGameData.InvalidateCache();

                string characterName = GetCurrentCharacterName();
                if (string.IsNullOrEmpty(characterName))
                {
                    Plugin.Log?.LogWarning("[GiftingAssistant] Character name unavailable; skipping roster load.");
                    return;
                }

                if (_loadedCharacterName != characterName)
                {
                    Plugin.SaveData();
                    _loadedCharacterName = characterName;
                }

                Plugin.Instance?.LoadDataForCharacter(characterName);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[GiftingAssistant] Error in OnPlayerInitialized: {ex.Message}");
            }
        }

        internal static void ResetForMenu()
        {
            _loadedCharacterName = null;
        }

        private static string GetCurrentCharacterName()
        {
            try
            {
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                if (gameSaveType == null)
                    return null;

                var currentCharProp = AccessTools.Property(gameSaveType, "CurrentCharacter");
                var currentChar = currentCharProp?.GetValue(null);
                if (currentChar != null)
                {
                    var nameProp = AccessTools.Property(currentChar.GetType(), "characterName");
                    var name = nameProp?.GetValue(currentChar) as string;
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }

                var instance = AccessTools.Property(gameSaveType, "Instance")?.GetValue(null);
                if (instance != null)
                {
                    var currentSave = AccessTools.Property(gameSaveType, "CurrentSave")?.GetValue(instance);
                    if (currentSave != null)
                    {
                        var charData = AccessTools.Property(currentSave.GetType(), "characterData")?.GetValue(currentSave);
                        if (charData != null)
                        {
                            var name = AccessTools.Property(charData.GetType(), "characterName")?.GetValue(charData) as string;
                            if (!string.IsNullOrEmpty(name))
                                return name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[GiftingAssistant] Failed to get character name: {ex.Message}");
            }

            return _loadedCharacterName;
        }
    }
}
