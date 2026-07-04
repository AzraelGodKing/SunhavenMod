using System;
using System.IO;
using BepInEx;
using SunhavenMods.Shared;

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
            CharacterSaveStore.EnsureDirectory(_savePath);
        }

        private string GetSaveFilePath(string characterName) =>
            CharacterSaveStore.GetFilePath(_savePath, characterName, "_giftroster.json");

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

                if (!CharacterSaveStore.WriteAtomic(filePath, json))
                    throw new IOException("Atomic write failed");

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
            var data = CharacterSaveStore.LoadWithBackup(filePath, json => TryDeserializePayload(json, characterName), out var source);

            if (data != null)
            {
                if (source == CharacterSaveSource.Backup)
                    Plugin.Log?.LogInfo($"[Load] Loaded gift roster from backup for {characterName}");

                return data;
            }

            if (File.Exists(filePath) || File.Exists(filePath + CharacterSaveStore.BackupSuffix))
                Plugin.Log?.LogWarning($"[Load] Main and backup files unusable for {characterName}");
            else
                Plugin.Log?.LogInfo($"[Load] No valid gift roster for {characterName}, creating new");

            return new GiftRosterData(characterName);
        }

        private GiftRosterData TryDeserializePayload(string json, string characterName)
        {
            if (!CharacterSaveStore.LooksLikeJsonObject(json))
            {
                Plugin.Log?.LogWarning($"[Load] Gift roster file is not valid JSON");
                return null;
            }

            var data = GiftRosterJson.Deserialize(json);
            if (data == null)
            {
                Plugin.Log?.LogWarning("[Load] Failed to deserialize gift roster file");
                return null;
            }

            if (string.IsNullOrEmpty(data.CharacterName))
                data.CharacterName = characterName;

            return data;
        }
    }
}
