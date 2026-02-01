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
                File.WriteAllText(filePath, json);
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
            if (!File.Exists(filePath))
            {
                Plugin.Log?.LogInfo($"No save file found for {characterName}, creating new todo list");
                return new TodoListData(characterName);
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var wrapper = JsonUtility.FromJson<TodoListDataWrapper>(json);
                var data = wrapper.ToData();
                Plugin.Log?.LogInfo($"Loaded todo list for {characterName} with {data.Items.Count} items");
                return data;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Failed to load todo list: {ex.Message}");
                return new TodoListData(characterName);
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
