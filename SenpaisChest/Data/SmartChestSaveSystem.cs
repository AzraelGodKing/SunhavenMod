using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;

namespace SenpaisChest.Data
{
    public class SmartChestSaveSystem
    {
        private readonly SmartChestManager _manager;
        private readonly string _savePath;
        private readonly HashSet<string> _successfulLoadsThisSession = new HashSet<string>(StringComparer.Ordinal);

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
            // Sync character name from game so we always write to the current character's file
            var currentName = Plugin.GetCurrentCharacterName();
            if (!string.IsNullOrEmpty(currentName))
                _manager.SetCharacterName(currentName);

            var data = _manager.GetSaveData();
            if (data == null)
            {
                Plugin.Log?.LogWarning("[Save] Cannot save: GetSaveData returned null");
                return;
            }

            if (string.IsNullOrEmpty(data.CharacterName))
            {
                Plugin.Log?.LogDebug("[Save] Cannot save: CharacterName is empty (expected on main menu)");
                return;
            }

            int totalRules = 0;
            foreach (var chest in data.Chests)
                totalRules += chest.Rules.Count;

            var filePath = GetSaveFilePath(data.CharacterName);
            Plugin.Log?.LogInfo($"[Save] Saving {data.Chests.Count} chest(s) with {totalRules} total rule(s) for '{data.CharacterName}' -> {filePath}");

            // Protect existing non-empty save files from being overwritten by an empty runtime state
            // when this session has not yet successfully loaded that character from disk.
            bool hasRulesInMemory = totalRules > 0;
            bool hasExistingRulesOnDisk = FileContainsRules(filePath) || FileContainsRules(filePath + ".bak");
            if (!hasRulesInMemory
                && hasExistingRulesOnDisk
                && !HasSuccessfulLoadThisSession(data.CharacterName)
                )
            {
                Plugin.Log?.LogWarning($"[Save] Skipping write for '{data.CharacterName}': in-memory rules are empty before any successful load this session. Existing non-empty file preserved.");
                return;
            }

            try
            {
                var json = SerializeToJson(data);

                Plugin.Log?.LogDebug($"[Save] JSON length: {json.Length} chars");

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
                Plugin.Log?.LogInfo($"[Save] Saved successfully: {filePath}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Save] Failed to save: {ex}");
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
                {
                    MarkSuccessfulLoad(characterName);
                    return result;
                }

                Plugin.Log?.LogWarning($"Main save file corrupted for {characterName}, trying backup...");
            }

            if (File.Exists(backupPath))
            {
                var result = TryLoadFromFile(backupPath, characterName);
                if (result != null)
                {
                    MarkSuccessfulLoad(characterName);
                    Plugin.Log?.LogInfo($"Loaded from backup for {characterName}");
                    return result;
                }
                Plugin.Log?.LogWarning($"Backup file also corrupted for {characterName}");
            }

            Plugin.Log?.LogInfo($"No save file found for {characterName}, creating new config");
            return new SmartChestSaveData(characterName);
        }

        private bool HasSuccessfulLoadThisSession(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return false;
            return _successfulLoadsThisSession.Contains(characterName);
        }

        private void MarkSuccessfulLoad(string characterName)
        {
            if (!string.IsNullOrEmpty(characterName))
                _successfulLoadsThisSession.Add(characterName);
        }

        private static bool FileContainsRules(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                var json = File.ReadAllText(filePath);
                var parsed = DeserializeFromJson(json);
                if (parsed == null || parsed.Chests == null)
                    return false;

                for (int i = 0; i < parsed.Chests.Count; i++)
                {
                    var chest = parsed.Chests[i];
                    if (chest?.Rules != null && chest.Rules.Count > 0)
                        return true;
                }
            }
            catch (Exception ex)
            {
                // If we cannot inspect the existing file, do not block save behavior here.
                Plugin.Log?.LogDebug($"[Save] FileContainsRules inspection failed for '{filePath}': {ex.Message}");
            }

            return false;
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

                var data = DeserializeFromJson(json);
                if (data == null)
                {
                    Plugin.Log?.LogWarning($"Failed to deserialize {filePath}");
                    return null;
                }

                int totalRules = 0;
                foreach (var chest in data.Chests)
                    totalRules += chest.Rules.Count;

                Plugin.Log?.LogInfo($"Loaded smart chest config for {characterName}: {data.Chests.Count} chest(s), {totalRules} rule(s)");
                return data;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading {filePath}: {ex.Message}");
                return null;
            }
        }


        #region JSON Serialization (delegates to SmartChestJson.cs — no BepInEx dependency)

        private static string SerializeToJson(SmartChestSaveData data) => SmartChestJson.Serialize(data);
        private static SmartChestSaveData DeserializeFromJson(string json) => SmartChestJson.Deserialize(json);

        #endregion
    }
}
