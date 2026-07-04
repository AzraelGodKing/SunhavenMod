using System;
using System.IO;
using UnityEngine;
using SunhavenMods.Shared;

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
                CharacterSaveStore.EnsureDirectory(_saveFolder);
                Plugin.Log?.LogInfo($"Created save folder: {_saveFolder}");
            }
        }

        private string GetSaveFilePath(string characterName) =>
            CharacterSaveStore.GetFilePath(_saveFolder, characterName, "_donations.json");

        /// <summary>
        /// Loads donation data for a character.
        /// </summary>
        public DonationData Load(string characterName)
        {
            string filePath = GetSaveFilePath(characterName);
            var data = CharacterSaveStore.LoadWithBackup(
                filePath,
                TryDeserializePayload,
                out var source,
                onReadFailure: (p, ex) => Plugin.Log?.LogError($"Failed to read donation save '{p}': {ex.Message}"));

            if (data != null)
            {
                if (source == CharacterSaveSource.Backup)
                    Plugin.Log?.LogInfo($"Loaded from backup for {characterName}");

                Plugin.Log?.LogInfo($"Loaded {data.DonatedItemIds.Count} donated items for {characterName}");
                return data;
            }

            if (CharacterSaveStore.GetAbsenceReason(filePath) == CharacterSaveAbsenceReason.FilesPresentButUnusable)
                Plugin.Log?.LogWarning($"Main and backup save files unusable for {characterName}");
            else
                Plugin.Log?.LogInfo($"No usable save data for {characterName}, creating new data");

            return new DonationData(characterName);
        }

        private DonationData TryDeserializePayload(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<DonationDataWrapper>(json);
                return wrapper?.ToData();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to deserialize donation save: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves donation data for a character.
        /// </summary>
        public bool Save(string characterName, DonationData data)
        {
            if (data == null || string.IsNullOrEmpty(characterName))
                return false;

            string filePath = GetSaveFilePath(characterName);

            try
            {
                var wrapper = new DonationDataWrapper(data);
                string json = JsonUtility.ToJson(wrapper, true);

                if (!CharacterSaveStore.WriteAtomic(filePath, json))
                    throw new IOException("Atomic write failed");

                Plugin.Log?.LogInfo($"Saved {data.DonatedItemIds.Count} donated items for {characterName}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to save donation data for {characterName}: {ex.Message}");
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
