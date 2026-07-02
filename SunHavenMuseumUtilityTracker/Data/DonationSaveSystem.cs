using System;
using System.IO;
using UnityEngine;

namespace SunHavenMuseumUtilityTracker.Data
{
    /// <summary>
    /// Handles saving and loading donation data per character.
    /// </summary>
    public class DonationSaveSystem
    {
        private readonly string _saveFolder;
        private readonly DonationManager _manager;
        private float _lastSaveCheck;
        private const float SAVE_INTERVAL = 30f;

        public DonationSaveSystem(DonationManager manager)
        {
            _manager = manager;
            _saveFolder = Path.Combine(BepInEx.Paths.ConfigPath, "SunHavenMuseumUtilityTracker", "Saves");

            if (!Directory.Exists(_saveFolder))
            {
                Directory.CreateDirectory(_saveFolder);
                Plugin.Log?.LogInfo($"Created save folder: {_saveFolder}");
            }
        }

        private string GetSaveFilePath(string characterName)
        {
            string safeName = SanitizeFileName(characterName);
            return Path.Combine(_saveFolder, $"{safeName}_donations.json");
        }

        private string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        /// <summary>
        /// Loads donation data for a character.
        /// </summary>
        public DonationData Load(string characterName)
        {
            string filePath = GetSaveFilePath(characterName);
            string backupPath = filePath + ".bak";

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

            Plugin.Log?.LogInfo($"No usable save data for {characterName}, creating new data");
            return new DonationData(characterName);
        }

        private DonationData TryLoadFromFile(string filePath, string characterName)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var wrapper = JsonUtility.FromJson<DonationDataWrapper>(json);

                if (wrapper != null)
                {
                    var data = wrapper.ToData();
                    Plugin.Log?.LogInfo($"Loaded {data.DonatedItemIds.Count} donated items for {characterName}");
                    return data;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to load donation data for {characterName} from {Path.GetFileName(filePath)}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Saves donation data for a character.
        /// </summary>
        public bool Save(string characterName, DonationData data)
        {
            if (data == null || string.IsNullOrEmpty(characterName))
                return false;

            string filePath = GetSaveFilePath(characterName);
            string tempPath = filePath + ".tmp";
            string backupPath = filePath + ".bak";

            try
            {
                var wrapper = new DonationDataWrapper(data);
                string json = JsonUtility.ToJson(wrapper, true);

                // Write to temp file first
                File.WriteAllText(tempPath, json);

                // Create backup of existing file
                if (File.Exists(filePath))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(filePath, backupPath);
                }

                // Move temp to final
                File.Move(tempPath, filePath);

                Plugin.Log?.LogInfo($"Saved {data.DonatedItemIds.Count} donated items for {characterName}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to save donation data for {characterName}: {ex.Message}");

                // Cleanup temp file
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                catch (Exception delEx) { Plugin.Log?.LogWarning($"[DonationSave] Cleanup temp file failed: {delEx.Message}"); }
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if it's time to auto-save and saves if dirty.
        /// </summary>
        public void CheckAutoSave()
        {
            if (!_manager.IsLoaded || !_manager.IsDirty)
                return;

            if (Time.time - _lastSaveCheck < SAVE_INTERVAL)
                return;

            _lastSaveCheck = Time.time;

            if (Save(_manager.CurrentCharacter, _manager.GetData()))
            {
                _manager.ClearDirty();
            }
        }

        /// <summary>
        /// Forces an immediate save.
        /// </summary>
        public bool ForceSave()
        {
            if (!_manager.IsLoaded)
                return false;

            bool result = Save(_manager.CurrentCharacter, _manager.GetData());
            if (result)
            {
                _manager.ClearDirty();
            }
            return result;
        }
    }
}
