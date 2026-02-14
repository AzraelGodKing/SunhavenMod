using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace SenpaisChest.Data
{
    public class SmartChestSaveSystem
    {
        private readonly SmartChestManager _manager;
        private readonly string _savePath;

        public SmartChestSaveSystem(SmartChestManager manager)
        {
            _manager = manager;
            _savePath = Path.Combine(Paths.ConfigPath, "SenpaisChest", "Saves");

            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
        }

        private string GetSaveFilePath(string characterName)
        {
            var safeName = SanitizeFileName(characterName);
            return Path.Combine(_savePath, $"{safeName}_smartchests.json");
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public void Save()
        {
            var data = _manager.GetSaveData();
            if (data == null || string.IsNullOrEmpty(data.CharacterName))
            {
                Plugin.Log?.LogWarning("Cannot save: No data or character name");
                return;
            }

            try
            {
                var wrapper = SmartChestSaveDataWrapper.FromData(data);
                var json = JsonUtility.ToJson(wrapper, true);
                var filePath = GetSaveFilePath(data.CharacterName);

                var tempFilePath = filePath + ".tmp";
                File.WriteAllText(tempFilePath, json);

                if (File.Exists(filePath))
                {
                    var backupPath = filePath + ".bak";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(filePath, backupPath);
                }

                File.Move(tempFilePath, filePath);

                _manager.MarkClean();
                Plugin.Log?.LogInfo($"Saved smart chest config for {data.CharacterName}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to save smart chest config: {ex.Message}");
            }
        }

        public SmartChestSaveData Load(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Plugin.Log?.LogWarning("Cannot load: No character name");
                return null;
            }

            var filePath = GetSaveFilePath(characterName);
            var backupPath = filePath + ".bak";

            if (File.Exists(filePath))
            {
                var result = TryLoadFromFile(filePath, characterName);
                if (result != null)
                    return result;

                Plugin.Log?.LogWarning($"Main save file corrupted for {characterName}, trying backup...");
            }

            if (File.Exists(backupPath))
            {
                var result = TryLoadFromFile(backupPath, characterName);
                if (result != null)
                {
                    Plugin.Log?.LogInfo($"Loaded from backup for {characterName}");
                    return result;
                }
                Plugin.Log?.LogWarning($"Backup file also corrupted for {characterName}");
            }

            Plugin.Log?.LogInfo($"No save file found for {characterName}, creating new config");
            return new SmartChestSaveData(characterName);
        }

        private SmartChestSaveData TryLoadFromFile(string filePath, string characterName)
        {
            try
            {
                var json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                {
                    Plugin.Log?.LogWarning($"File {filePath} does not contain valid JSON");
                    return null;
                }

                var wrapper = JsonUtility.FromJson<SmartChestSaveDataWrapper>(json);
                if (wrapper == null)
                {
                    Plugin.Log?.LogWarning($"Failed to deserialize {filePath}");
                    return null;
                }

                var data = wrapper.ToData();
                Plugin.Log?.LogInfo($"Loaded smart chest config for {characterName} with {data.Chests.Count} chests");
                return data;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading {filePath}: {ex.Message}");
                return null;
            }
        }
    }
}
