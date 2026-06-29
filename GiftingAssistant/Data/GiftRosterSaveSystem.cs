using System;
using System.IO;
using BepInEx;

namespace GiftingAssistant.Data
{
    public class GiftRosterSaveSystem
    {
        private readonly GiftRosterManager _manager;
        private readonly string _savePath;

        public GiftRosterSaveSystem(GiftRosterManager manager)
        {
            _manager = manager;
            _savePath = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_GUID);

            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);
        }

        private string GetSaveFilePath(string characterName)
        {
            var safeName = SanitizeFileName(characterName);
            return Path.Combine(_savePath, $"{safeName}_giftroster.json");
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        public void Save()
        {
            var data = _manager.GetData();
            if (data == null || string.IsNullOrEmpty(data.CharacterName))
            {
                Plugin.Log?.LogWarning("[Save] Cannot save: no data or character name");
                return;
            }

            try
            {
                var json = GiftRosterJson.Serialize(data);
                var filePath = GetSaveFilePath(data.CharacterName);
                var tempFilePath = filePath + ".tmp";

                try
                {
                    File.WriteAllText(tempFilePath, json);

                    if (File.Exists(filePath))
                    {
                        var backupPath = filePath + ".bak";
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                        File.Move(filePath, backupPath);
                    }

                    File.Move(tempFilePath, filePath);
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }

                _manager.MarkClean();
                Plugin.Log?.LogInfo($"[Save] Saved gift roster for '{data.CharacterName}' ({data.Entries.Count} entries)");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Save] Failed to save gift roster: {ex}");
            }
        }

        public GiftRosterData Load(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Plugin.Log?.LogWarning("[Load] Cannot load: no character name");
                return null;
            }

            var filePath = GetSaveFilePath(characterName);
            var backupPath = filePath + ".bak";

            if (File.Exists(filePath))
            {
                var result = TryLoadFromFile(filePath, characterName);
                if (result != null)
                    return result;
                Plugin.Log?.LogWarning($"[Load] Main file corrupted for {characterName}, trying backup...");
            }

            if (File.Exists(backupPath))
            {
                var result = TryLoadFromFile(backupPath, characterName);
                if (result != null)
                {
                    Plugin.Log?.LogInfo($"[Load] Loaded gift roster from backup for {characterName}");
                    return result;
                }
            }

            Plugin.Log?.LogInfo($"[Load] No valid gift roster for {characterName}, creating new");
            return new GiftRosterData(characterName);
        }

        private GiftRosterData TryLoadFromFile(string filePath, string characterName)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                {
                    Plugin.Log?.LogWarning($"[Load] {filePath} is not valid JSON");
                    return null;
                }

                var data = GiftRosterJson.Deserialize(json);
                if (data == null)
                {
                    Plugin.Log?.LogWarning($"[Load] Failed to deserialize {filePath}");
                    return null;
                }

                if (string.IsNullOrEmpty(data.CharacterName))
                    data.CharacterName = characterName;

                return data;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Load] Error loading {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}
