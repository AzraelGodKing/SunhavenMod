using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using SunhavenMods.Shared;

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
            CharacterSaveStore.EnsureDirectory(_savePath);
        }

        private string GetSaveFilePath(string characterName) =>
            CharacterSaveStore.GetFilePath(_savePath, characterName, "_smartchests.json");

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
            bool hasExistingRulesOnDisk = FileContainsRules(filePath) || FileContainsRules(filePath + CharacterSaveStore.BackupSuffix);
            if (!hasRulesInMemory
                && hasExistingRulesOnDisk
                && !HasSuccessfulLoadThisSession(data.CharacterName)
                )
            {
                Plugin.Log?.LogWarning($"[Save] Skipping write for '{data.CharacterName}': in-memory rules are empty before any successful load this session. Existing non-empty file preserved.");
                return;
            }

            if (!hasRulesInMemory
                && hasExistingRulesOnDisk
                && HasSuccessfulLoadThisSession(data.CharacterName)
                && !_manager.UserClearedAllRulesThisSession)
            {
                Plugin.Log?.LogWarning($"[Save] Skipping write for '{data.CharacterName}': in-memory rules are empty after load but disk still has rules (likely area transition). Existing file preserved.");
                return;
            }

            try
            {
                var json = SerializeToJson(data);

                Plugin.Log?.LogDebug($"[Save] JSON length: {json.Length} chars");

                if (!CharacterSaveStore.WriteAtomic(filePath, json))
                    throw new IOException("Atomic write failed");

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
            var data = CharacterSaveStore.LoadWithBackup(
                filePath,
                json => TryDeserializePayload(json, characterName),
                out var source,
                onReadFailure: (p, ex) => Plugin.Log?.LogError($"Error loading {p}: {ex.Message}"));

            if (data != null)
            {
                MarkSuccessfulLoad(characterName);
                if (source == CharacterSaveSource.Backup)
                    Plugin.Log?.LogInfo($"Loaded from backup for {characterName}");

                return data;
            }

            if (CharacterSaveStore.GetAbsenceReason(filePath) == CharacterSaveAbsenceReason.FilesPresentButUnusable)
                Plugin.Log?.LogWarning($"Main and backup save files unusable for {characterName}");
            else
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

        private SmartChestSaveData TryDeserializePayload(string json, string characterName)
        {
            if (!CharacterSaveStore.LooksLikeJsonObject(json))
            {
                Plugin.Log?.LogWarning("Smart chest save file does not contain valid JSON");
                return null;
            }

            var data = DeserializeFromJson(json);
            if (data == null)
            {
                Plugin.Log?.LogWarning("Failed to deserialize smart chest save file");
                return null;
            }

            int totalRules = 0;
            foreach (var chest in data.Chests)
                totalRules += chest.Rules.Count;

            Plugin.Log?.LogInfo($"Loaded smart chest config for {characterName}: {data.Chests.Count} chest(s), {totalRules} rule(s)");
            return data;
        }

        #region JSON Serialization (delegates to SmartChestJson.cs — no BepInEx dependency)

        private static string SerializeToJson(SmartChestSaveData data) => SmartChestJson.Serialize(data);
        private static SmartChestSaveData DeserializeFromJson(string json) => SmartChestJson.Deserialize(json);

        #endregion
    }
}
