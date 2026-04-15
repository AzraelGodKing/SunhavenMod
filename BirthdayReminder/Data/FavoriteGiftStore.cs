using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;

namespace BirthdayReminder.Data
{
    internal sealed class FavoriteGiftStore
    {
        private readonly string _characterName;
        private readonly Dictionary<string, string> _favoritesByNpc;

        private FavoriteGiftStore(string characterName)
        {
            _characterName = string.IsNullOrWhiteSpace(characterName) ? "Unknown" : characterName;
            _favoritesByNpc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static FavoriteGiftStore Load(string characterName)
        {
            var store = new FavoriteGiftStore(characterName);
            string path = GetPath(characterName);

            try
            {
                if (!File.Exists(path))
                    return store;

                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    int sep = line.IndexOf('\t');
                    if (sep <= 0 || sep >= line.Length - 1)
                        continue;

                    string npcName = line.Substring(0, sep);
                    string giftName = line.Substring(sep + 1);
                    if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(giftName))
                        continue;
                    store._favoritesByNpc[npcName] = giftName;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Favorites] Load failed for '{characterName}': {ex.Message}");
            }

            return store;
        }

        public bool TryGetFavorite(string npcName, out string giftName)
        {
            giftName = null;
            if (string.IsNullOrWhiteSpace(npcName))
                return false;
            return _favoritesByNpc.TryGetValue(npcName, out giftName) && !string.IsNullOrWhiteSpace(giftName);
        }

        public void SetFavorite(string npcName, string giftName)
        {
            if (string.IsNullOrWhiteSpace(npcName) || string.IsNullOrWhiteSpace(giftName))
                return;
            _favoritesByNpc[npcName] = giftName.Trim();
        }

        public void Save()
        {
            string path = GetPath(_characterName);
            string dir = Path.GetDirectoryName(path);
            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string tempPath = path + ".tmp";
                var sb = new StringBuilder();
                foreach (var kvp in _favoritesByNpc)
                {
                    sb.Append(kvp.Key);
                    sb.Append('\t');
                    sb.Append(kvp.Value);
                    sb.AppendLine();
                }
                try
                {
                    File.WriteAllText(tempPath, sb.ToString());
                    if (File.Exists(path))
                        File.Delete(path);
                    File.Move(tempPath, path);
                }
                finally
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Favorites] Save failed for '{_characterName}': {ex.Message}");
            }
        }

        private static string GetPath(string characterName)
        {
            string safeCharacter = SanitizeFileName(characterName);
            return Path.Combine(Paths.ConfigPath, "BirthdayReminder", "Data", $"{safeCharacter}_favorites.txt");
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Trim();
        }
    }
}
