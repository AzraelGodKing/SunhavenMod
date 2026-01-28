using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TheList.Data
{
    /// <summary>
    /// Handles saving and loading list data per-character.
    /// </summary>
    public class ListSaveSystem
    {
        private readonly ListManager _listManager;
        private readonly string _saveDirectory;
        private string _currentSaveFile;
        private float _lastAutoSave;

        public ListSaveSystem(ListManager listManager)
        {
            _listManager = listManager;
            _saveDirectory = Path.Combine(BepInEx.Paths.ConfigPath, "TheList", "Saves");

            // Ensure save directory exists
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
                Plugin.Log?.LogInfo($"Created save directory: {_saveDirectory}");
            }

            _lastAutoSave = Time.time;
        }

        #region Save/Load

        /// <summary>
        /// Save list data for the current player.
        /// </summary>
        public bool Save()
        {
            try
            {
                var data = _listManager.GetListData();
                if (string.IsNullOrEmpty(data.PlayerName))
                {
                    Plugin.Log?.LogWarning("Cannot save: no player name set");
                    return false;
                }

                string savePath = GetSaveFilePath(data.PlayerName);
                string json = SerializeData(data);

                // Write to temp file first
                string tempPath = savePath + ".tmp";
                File.WriteAllText(tempPath, json, Encoding.UTF8);

                // Backup existing file
                if (File.Exists(savePath))
                {
                    string backupPath = savePath + ".backup";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(savePath, backupPath);
                }

                // Move temp to final
                File.Move(tempPath, savePath);

                _listManager.MarkClean();
                _currentSaveFile = savePath;
                _lastAutoSave = Time.time;

                Plugin.Log?.LogInfo($"Saved list data to {savePath}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error saving list data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load list data for a player.
        /// </summary>
        public bool Load(string playerName)
        {
            try
            {
                if (string.IsNullOrEmpty(playerName))
                {
                    Plugin.Log?.LogWarning("Cannot load: no player name provided");
                    return false;
                }

                string savePath = GetSaveFilePath(playerName);
                _currentSaveFile = savePath;

                if (!File.Exists(savePath))
                {
                    // No save file - create new list for this player
                    Plugin.Log?.LogInfo($"No save file found for {playerName}, creating new list");
                    var newData = new ListData { PlayerName = playerName };
                    _listManager.LoadListData(newData);
                    return true;
                }

                string json = File.ReadAllText(savePath, Encoding.UTF8);
                var data = DeserializeData(json);

                if (data == null)
                {
                    // Try loading from backup
                    string backupPath = savePath + ".backup";
                    if (File.Exists(backupPath))
                    {
                        Plugin.Log?.LogWarning("Main save corrupted, trying backup...");
                        json = File.ReadAllText(backupPath, Encoding.UTF8);
                        data = DeserializeData(json);
                    }
                }

                if (data == null)
                {
                    Plugin.Log?.LogError($"Could not load save data for {playerName}");
                    var newData = new ListData { PlayerName = playerName };
                    _listManager.LoadListData(newData);
                    return false;
                }

                data.PlayerName = playerName; // Ensure name matches
                _listManager.LoadListData(data);
                _lastAutoSave = Time.time;

                Plugin.Log?.LogInfo($"Loaded list data for {playerName}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading list data: {ex.Message}");
                var newData = new ListData { PlayerName = playerName };
                _listManager.LoadListData(newData);
                return false;
            }
        }

        #endregion

        #region Auto-Save

        /// <summary>
        /// Check if auto-save should occur.
        /// </summary>
        public void CheckAutoSave(float interval)
        {
            if (!_listManager.IsDirty)
                return;

            if (Time.time - _lastAutoSave >= interval)
            {
                Save();
            }
        }

        /// <summary>
        /// Force save regardless of dirty state.
        /// </summary>
        public void ForceSave()
        {
            if (_listManager.IsDirty)
            {
                Save();
            }
        }

        #endregion

        #region Serialization

        private string SerializeData(ListData data)
        {
            var wrapper = ListDataWrapper.FromListData(data);
            return JsonUtility.ToJson(wrapper, true);
        }

        private ListData DeserializeData(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<ListDataWrapper>(json);
                return wrapper?.ToListData();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Deserialization error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Utilities

        private string GetSaveFilePath(string playerName)
        {
            string safeName = SanitizeFileName(playerName);
            return Path.Combine(_saveDirectory, $"{safeName}.list");
        }

        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "default";

            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid)
            {
                name = name.Replace(c, '_');
            }

            name = name.Trim();
            return string.IsNullOrEmpty(name) ? "default" : name;
        }

        /// <summary>
        /// Check if a save file exists for a player.
        /// </summary>
        public bool SaveExists(string playerName)
        {
            return File.Exists(GetSaveFilePath(playerName));
        }

        /// <summary>
        /// Delete save file for a player.
        /// </summary>
        public bool DeleteSave(string playerName)
        {
            try
            {
                string savePath = GetSaveFilePath(playerName);
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Plugin.Log?.LogInfo($"Deleted save file for {playerName}");
                }

                string backupPath = savePath + ".backup";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error deleting save: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all player names with save files.
        /// </summary>
        public string[] GetAllSavedPlayers()
        {
            try
            {
                var files = Directory.GetFiles(_saveDirectory, "*.list");
                var names = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                {
                    names[i] = Path.GetFileNameWithoutExtension(files[i]);
                }
                return names;
            }
            catch
            {
                return new string[0];
            }
        }

        #endregion
    }
}
