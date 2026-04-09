using Wish;

namespace HavensBirthright.Session
{
    /// <summary>
    /// Snapshot written from <see cref="Wish.GameSave.LoadCharacter(int)"/> postfix (TheVault / SMUT pattern).
    /// <see cref="GameSave.CurrentCharacter"/> can lag briefly during transitions; pairing this with
    /// <see cref="TryGetAuthoritativeCharacterData"/> avoids fingerprint/race bugs during that window.
    /// </summary>
    internal static class BirthrightGameSaveContext
    {
        public static int LastLoadedSlot { get; private set; } = -1;

        public static string LastLoadedCharacterName { get; private set; }

        public static void Reset()
        {
            LastLoadedSlot = -1;
            LastLoadedCharacterName = null;
        }

        public static void RecordLoadCharacter(int characterNumber)
        {
            LastLoadedSlot = characterNumber;
            LastLoadedCharacterName = null;
            try
            {
                var gs = GameSave.Instance;
                if (gs?.Saves == null || characterNumber < 0 || characterNumber >= gs.Saves.Count)
                    return;
                LastLoadedCharacterName = gs.Saves[characterNumber]?.characterData?.characterName;
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// True when we have a save list entry for the slot the game last loaded via <c>LoadCharacter</c>.
        /// </summary>
        public static bool TryGetAuthoritativeCharacterData(out CharacterData data)
        {
            data = null;
            try
            {
                var gs = GameSave.Instance;
                if (gs == null || LastLoadedSlot < 0 || gs.Saves == null || LastLoadedSlot >= gs.Saves.Count)
                    return false;
                if (gs.CharacterIndex != LastLoadedSlot)
                    return false;
                data = gs.Saves[LastLoadedSlot]?.characterData;
                return data != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
