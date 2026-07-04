using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using SunhavenMods.Shared;

namespace BirthdayReminder.Data
{
    internal sealed class FavoriteGiftStore
    {
        private const string SaveDirectoryName = "BirthdayReminder";
        private const string SaveSubDirectoryName = "Data";
        private const string FileSuffix = "_favorites.txt";

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

            try
            {
                string path = GetPath(characterName);
                string text = CharacterSaveStore.LoadTextWithBackup(
                    path,
                    out var source,
                    onReadFailure: (p, ex) => Plugin.Log?.LogWarning($"[Favorites] Failed to read '{p}': {ex.Message}"));
                if (text == null)
                    return store;

                if (source == CharacterSaveSource.Backup)
                    Plugin.Log?.LogInfo($"[Favorites] Loaded from backup for '{characterName}'");

                store.ParseLines(text);
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
            try
            {
                var sb = new StringBuilder();
                foreach (var kvp in _favoritesByNpc)
                {
                    sb.Append(kvp.Key);
                    sb.Append('\t');
                    sb.Append(kvp.Value);
                    sb.AppendLine();
                }

                if (!CharacterSaveStore.WriteAtomic(path, sb.ToString()))
                    Plugin.Log?.LogWarning($"[Favorites] Save failed for '{_characterName}'");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Favorites] Save failed for '{_characterName}': {ex.Message}");
            }
        }

        private void ParseLines(string text)
        {
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
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

                    _favoritesByNpc[npcName] = giftName;
                }
            }
        }

        private static string GetPath(string characterName)
        {
            string directory = Path.Combine(Paths.ConfigPath, SaveDirectoryName, SaveSubDirectoryName);
            return CharacterSaveStore.GetFilePath(directory, characterName, FileSuffix, "Unknown");
        }
    }
}
