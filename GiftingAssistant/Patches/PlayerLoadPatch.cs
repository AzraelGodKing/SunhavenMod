using System;
using GiftingAssistant.Game;
using HarmonyLib;
using SunhavenMods.Shared;

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

                string characterName = GameSaveCharacterName.TryGetCurrent(
                    _loadedCharacterName,
                    msg => Plugin.Log?.LogWarning($"[GiftingAssistant] Failed to get character name: {msg}"));
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
    }
}
