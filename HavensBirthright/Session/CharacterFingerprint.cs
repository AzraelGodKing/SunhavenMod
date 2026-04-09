using System;
using Wish;

namespace HavensBirthright.Session
{
    /// <summary>
    /// Character appearance + stable switch identity (save slot, indices, body layer 14).
    /// </summary>
    internal static class CharacterFingerprint
    {
        internal const byte LegacySwitchFingerprintCharIndex = 255;
        internal const int LegacySwitchFingerprintListSlot = -2;

        /// <summary>
        /// Prefer save-slot <see cref="CharacterData"/> when <see cref="GameSave.CurrentCharacter"/> is stale right after <c>LoadCharacter</c>.
        /// </summary>
        internal static CharacterData GetAuthoritativeCharacterData()
        {
            CharacterData ch = GameSave.CurrentCharacter;
            if (BirthrightGameSaveContext.TryGetAuthoritativeCharacterData(out CharacterData auth))
            {
                if (ch == null || !string.Equals(ch.characterName, auth.characterName, StringComparison.Ordinal))
                    ch = auth;
            }
            return ch;
        }

        internal static string GetCurrentBodyStyleName()
        {
            try
            {
                CharacterData ch = GetAuthoritativeCharacterData();
                if (ch?.StyleData == null || ch.StyleData.Count == 0)
                    return null;
                ch.StyleData.TryGetValue(14, out string bodyStyleName);
                return bodyStyleName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Format: <c>name|gameRaceByte|characterData.characterIndex|GameSave.CharacterIndex|bodyStyle14</c>.
        /// </summary>
        internal static string GetCurrentCharacterSwitchId()
        {
            try
            {
                CharacterData ch = GetAuthoritativeCharacterData();
                if (ch == null)
                    return null;
                string name = ch.characterName ?? "";
                string body = "";
                ch.StyleData?.TryGetValue(14, out body);
                int listSlot = -1;
                try
                {
                    var inst = GameSave.Instance;
                    if (inst != null)
                        listSlot = inst.CharacterIndex;
                }
                catch
                {
                    // ignore
                }

                return $"{name}|{ch.race}|{ch.characterIndex}|{listSlot}|{body ?? ""}";
            }
            catch
            {
                return null;
            }
        }

        internal static bool TryParseCharacterSwitchFingerprint(string id, out string name, out string raceByte, out byte charDataIndex, out int listSlot, out string bodyStyle)
        {
            name = "";
            raceByte = "";
            bodyStyle = "";
            charDataIndex = 0;
            listSlot = LegacySwitchFingerprintListSlot;
            if (string.IsNullOrEmpty(id))
                return false;

            string[] p = id.Split('|');
            if (p.Length >= 5 && byte.TryParse(p[2], out charDataIndex) && int.TryParse(p[3], out listSlot))
            {
                name = p[0];
                raceByte = p[1];
                bodyStyle = p.Length == 5 ? (p[4] ?? "") : string.Join("|", p, 4, p.Length - 4);
                return true;
            }

            if (p.Length == 3)
            {
                name = p[0];
                raceByte = p[1];
                bodyStyle = p[2] ?? "";
                charDataIndex = LegacySwitchFingerprintCharIndex;
                listSlot = LegacySwitchFingerprintListSlot;
                return true;
            }

            return false;
        }
    }
}
