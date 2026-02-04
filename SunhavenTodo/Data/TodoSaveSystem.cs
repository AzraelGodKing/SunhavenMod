using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace SunhavenTodo.Data
{
    public class TodoSaveSystem
    {
        private readonly TodoManager _manager;
        private string _savePath;

        public TodoSaveSystem(TodoManager manager)
        {
            _manager = manager;
            _savePath = Path.Combine(Paths.ConfigPath, PluginInfo.PLUGIN_GUID);

            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
        }

        private string GetSaveFilePath(string characterName)
        {
            var safeName = SanitizeFileName(characterName);
            return Path.Combine(_savePath, $"{safeName}_todos.json");
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
            var data = _manager.GetData();
            if (data == null || string.IsNullOrEmpty(data.CharacterName))
            {
                Plugin.Log?.LogWarning("Cannot save: No data or character name");
                return;
            }

            try
            {
                var wrapper = TodoListDataWrapper.FromData(data);
                var json = JsonUtility.ToJson(wrapper, true);
                var filePath = GetSaveFilePath(data.CharacterName);

                // Write to temp file first (atomic operation)
                var tempFilePath = filePath + ".tmp";
                File.WriteAllText(tempFilePath, json);

                // Backup existing file before overwriting
                if (File.Exists(filePath))
                {
                    var backupPath = filePath + ".bak";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(filePath, backupPath);
                }

                // Move temp file to final location
                File.Move(tempFilePath, filePath);

                _manager.MarkClean();
                Plugin.Log?.LogInfo($"Saved todo list for {data.CharacterName} to {filePath}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to save todo list: {ex.Message}");
            }
        }

        public TodoListData Load(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                Plugin.Log?.LogWarning("Cannot load: No character name");
                return null;
            }

            var filePath = GetSaveFilePath(characterName);
            var backupPath = filePath + ".bak";

            // Try main file first
            if (File.Exists(filePath))
            {
                var result = TryLoadFromFile(filePath, characterName);
                if (result != null)
                    return result;

                Plugin.Log?.LogWarning($"Main save file corrupted for {characterName}, trying backup...");
            }

            // Try backup file if main failed or doesn't exist
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

            Plugin.Log?.LogInfo($"No valid save file found for {characterName}, creating new todo list");
            return new TodoListData(characterName);
        }

        private TodoListData TryLoadFromFile(string filePath, string characterName)
        {
            try
            {
                var json = File.ReadAllText(filePath);

                // Basic validation - check if it looks like valid JSON
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
                {
                    Plugin.Log?.LogWarning($"File {filePath} does not contain valid JSON");
                    return null;
                }

                var wrapper = JsonUtility.FromJson<TodoListDataWrapper>(json);
                if (wrapper == null)
                {
                    Plugin.Log?.LogWarning($"Failed to deserialize {filePath}");
                    return null;
                }

                var data = wrapper.ToData();
                Plugin.Log?.LogInfo($"Loaded todo list for {characterName} with {data.Items.Count} items from {filePath}");
                return data;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Error loading {filePath}: {ex.Message}");
                return null;
            }
        }

        public void Delete(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return;

            var filePath = GetSaveFilePath(characterName);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Plugin.Log?.LogInfo($"Deleted todo list for {characterName}");
                }
                catch (Exception ex)
                {
                    Plugin.Log?.LogError($"Failed to delete todo list: {ex.Message}");
                }
            }
        }
    }
}
