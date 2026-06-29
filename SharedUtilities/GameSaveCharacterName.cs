using System;
using HarmonyLib;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Resolves the active character name from <c>Wish.GameSave</c> (CurrentCharacter or save characterData).
    /// </summary>
    public static class GameSaveCharacterName
    {
        /// <param name="fallback">Returned when lookup fails (e.g. last loaded character name).</param>
        /// <param name="logWarning">Optional logger for lookup failures.</param>
        public static string TryGetCurrent(string fallback = null, Action<string> logWarning = null)
        {
            try
            {
                var gameSaveType = AccessTools.TypeByName("Wish.GameSave");
                if (gameSaveType == null)
                    return fallback;

                var currentCharProp = AccessTools.Property(gameSaveType, "CurrentCharacter");
                var currentChar = currentCharProp?.GetValue(null);
                if (currentChar != null)
                {
                    var name = ReadCharacterName(currentChar);
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
                            var name = ReadCharacterName(charData);
                            if (!string.IsNullOrEmpty(name))
                                return name;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logWarning?.Invoke(ex.Message);
            }

            return fallback;
        }

        private static string ReadCharacterName(object characterOrData)
        {
            var nameProp = AccessTools.Property(characterOrData.GetType(), "characterName");
            return nameProp?.GetValue(characterOrData) as string;
        }
    }
}
